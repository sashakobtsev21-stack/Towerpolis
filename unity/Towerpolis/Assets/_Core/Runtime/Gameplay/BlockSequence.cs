using Towerpolis.Core.Determinism;

namespace Towerpolis.Core.Gameplay
{
    /// <summary>
    /// Deterministic floor-type generator (spec §1.5, game-director ruling OQ-02). A weighted draw
    /// (Standard 70 / Balcony 22 / Premium 8) from a seed-derived stream, with two guards:
    /// floors 1–3 are forced Standard, and no rolling window of 5 floors may contain more than 2
    /// Premium (violating Premium draws are rejected and re-rolled, advancing the same stream so the
    /// result stays fully reproducible from the seed). Reproducibility is the enabler of the daily
    /// seed and cross-device fairness.
    /// </summary>
    public sealed class BlockSequence
    {
        // Cumulative weight cuts out of 100: [0,70) Standard, [70,92) Balcony, [92,100) Premium.
        const int StandardCut = 70;
        const int BalconyCut = 92;

        const int PremiumWindow = 5;
        const int MaxPremiumInWindow = 2;
        const int ForcedStandardFloors = 3;
        const int RedrawGuard = 64;

        readonly XorShiftRng _rng;

        // Rolling record of whether each of the last (PremiumWindow-1) placed floors was Premium.
        readonly bool[] _recentPremium = new bool[PremiumWindow];
        int _recentCount;
        int _recentHead;
        int _premiumInWindow;

        int _floor; // last produced floor index (0 = none yet); floors are 1-based

        public BlockSequence(ulong runSeed) => _rng = RunSeeds.BlockRng(runSeed);

        /// <summary>The next floor's type (floors are 1-based).</summary>
        public FloorType Next()
        {
            _floor += 1;
            FloorType type = _floor <= ForcedStandardFloors
                ? FloorType.Standard
                : DrawWithPremiumCap();
            Record(type == FloorType.Premium);
            return type;
        }

        /// <summary>The first <paramref name="count"/> floor types (floors 1..count). For tests/preview.</summary>
        public static FloorType[] Generate(ulong runSeed, int count)
        {
            var seq = new BlockSequence(runSeed);
            var result = new FloorType[count];
            for (int i = 0; i < count; i++) result[i] = seq.Next();
            return result;
        }

        FloorType DrawWithPremiumCap()
        {
            // Re-roll (advancing the stream) while a Premium would exceed the window cap. Deterministic.
            for (int guard = 0; guard < RedrawGuard; guard++)
            {
                FloorType type = DrawWeighted();
                if (type != FloorType.Premium || _premiumInWindow < MaxPremiumInWindow) return type;
            }
            return FloorType.Standard; // unreachable in practice; preserves sequence length
        }

        FloorType DrawWeighted()
        {
            int r = _rng.NextInt(0, 100);
            if (r < StandardCut) return FloorType.Standard;
            if (r < BalconyCut) return FloorType.Balcony;
            return FloorType.Premium;
        }

        // Maintain premiums among the last (PremiumWindow-1) placed floors, so that when the NEXT
        // floor is decided, allowing a Premium keeps any 5-floor window at <= 2 Premium.
        void Record(bool isPremium)
        {
            const int capacity = PremiumWindow - 1;
            if (_recentCount == capacity)
            {
                if (_recentPremium[_recentHead]) _premiumInWindow -= 1;
                _recentPremium[_recentHead] = isPremium;
                _recentHead = (_recentHead + 1) % capacity;
            }
            else
            {
                int tail = (_recentHead + _recentCount) % capacity;
                _recentPremium[tail] = isPremium;
                _recentCount += 1;
            }
            if (isPremium) _premiumInWindow += 1;
        }
    }
}
