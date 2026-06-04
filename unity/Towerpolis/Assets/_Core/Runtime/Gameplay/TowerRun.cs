using System;

namespace Towerpolis.Core.Gameplay
{
    /// <summary>Result of one block placement (all deterministic; the Unity layer plays juice from it).</summary>
    public readonly struct DropOutcome
    {
        public readonly Grade Grade;
        public readonly bool FloorPlaced;     // false only on Miss
        public readonly int ScoreGained;
        public readonly int ResidentsAdded;
        public readonly float TopWidth;        // tower top width after this drop
        public readonly float LeanOffset;      // accumulated lean after this drop
        public readonly int MissStrikes;
        public readonly int PerfectChain;
        public readonly bool Toppled;          // true on the drop that ends the run

        public DropOutcome(Grade grade, bool floorPlaced, int scoreGained, int residentsAdded,
            float topWidth, float leanOffset, int missStrikes, int perfectChain, bool toppled)
        {
            Grade = grade;
            FloorPlaced = floorPlaced;
            ScoreGained = scoreGained;
            ResidentsAdded = residentsAdded;
            TopWidth = topWidth;
            LeanOffset = leanOffset;
            MissStrikes = missStrikes;
            PerfectChain = perfectChain;
            Toppled = toppled;
        }
    }

    /// <summary>
    /// The deterministic state of a single run (spec §3, §4, §6). Drives grading, the overhang slice,
    /// lean accumulation, the cumulative 2-strike rule, scoring and perfect chains. Holds NO engine
    /// state and reads NO clock — the Unity layer feeds it the drop offset and renders the outcome.
    /// Score is fully decided here BEFORE any wobble/topple animation plays (ADR-0002).
    /// </summary>
    public sealed class TowerRun
    {
        readonly CoreConfig _cfg;

        public float CurrentTopWidth { get; private set; }
        public float LeanOffset { get; private set; }
        public int MissStrikes { get; private set; }
        public int Score { get; private set; }           // sum of floor scores only (spec §6.1)
        public int PerfectChain { get; private set; }
        public int TotalPerfects { get; private set; }   // cumulative Perfect drops (coins/stats — meta §5)
        public int FloorCount { get; private set; }     // placed floors, excluding the base
        public int TotalResidents { get; private set; }
        public bool IsOver { get; private set; }

        /// <summary>Total run score = floor scores + resident bonus (spec §6.3).</summary>
        public int RunScore => Score + TotalResidents * _cfg.ResidentScoreValue;

        public TowerRun(CoreConfig cfg)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            _cfg = cfg;
            CurrentTopWidth = cfg.InitialBlockWidth;
        }

        /// <summary>Place the next block at horizontal <paramref name="offsetX"/> from the tower top center.</summary>
        public DropOutcome PlaceBlock(FloorType type, float offsetX)
        {
            if (IsOver) throw new InvalidOperationException("The run is already over.");

            Grade grade = Grading.Evaluate(_cfg, offsetX, CurrentTopWidth);
            int scoreGained = 0;
            int residentsAdded = 0;
            // Tower-Bloxx: only a clean-enough catch (Perfect/Good) lands; Sloppy & Miss tip off and fall.
            bool floorPlaced = grade == Grade.Perfect || grade == Grade.Good;

            switch (grade)
            {
                case Grade.Perfect:
                    PerfectChain += 1;
                    TotalPerfects += 1;
                    LeanOffset *= 1f - _cfg.PerfectLeanCorrectionFraction;
                    residentsAdded = Scoring.BaseResidents(_cfg, type) + Scoring.PerfectResidentBonus(_cfg, type);
                    scoreGained = Scoring.FloorScore(_cfg, type, grade, PerfectChain);
                    FloorCount += 1;
                    break;

                case Grade.Good:
                    // Tower-Bloxx pivot: the block stays WHOLE (no slice). Top width is constant;
                    // the overhang accumulates as lean that drives the building's sway.
                    LeanOffset += offsetX * _cfg.GoodLeanFactor;
                    PerfectChain = 0;
                    residentsAdded = Scoring.BaseResidents(_cfg, type);
                    scoreGained = Scoring.FloorScore(_cfg, type, grade, PerfectChain);
                    FloorCount += 1;
                    break;

                case Grade.Sloppy:
                    // Too much overhang — the block tips off (not placed). Costs a strike.
                    PerfectChain = 0;
                    if (_cfg.SloppyCostsStrike) MissStrikes += 1;
                    break;

                default: // Miss — block misses entirely and falls. Costs a strike.
                    PerfectChain = 0;
                    MissStrikes += 1;
                    break;
            }

            Score += scoreGained;
            TotalResidents += residentsAdded;

            bool toppled = MissStrikes >= _cfg.StrikeLimit;
            if (toppled) IsOver = true;

            return new DropOutcome(grade, floorPlaced, scoreGained, residentsAdded,
                CurrentTopWidth, LeanOffset, MissStrikes, PerfectChain, toppled);
        }
    }
}
