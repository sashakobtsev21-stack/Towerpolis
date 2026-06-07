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
        public readonly int ComboLevel;        // post-drop combo level (0..cap) — drives the HUD combo bar
        public readonly int ComboBonusScore;   // >0 only on the drop that FILLED the combo bar (then it reset)
        public readonly bool Toppled;          // true on the drop that ends the run

        public DropOutcome(Grade grade, bool floorPlaced, int scoreGained, int residentsAdded,
            float topWidth, float leanOffset, int missStrikes, int perfectChain, int comboLevel,
            int comboBonusScore, bool toppled)
        {
            Grade = grade;
            FloorPlaced = floorPlaced;
            ScoreGained = scoreGained;
            ResidentsAdded = residentsAdded;
            TopWidth = topWidth;
            LeanOffset = leanOffset;
            MissStrikes = missStrikes;
            PerfectChain = perfectChain;
            ComboLevel = comboLevel;
            ComboBonusScore = comboBonusScore;
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
        readonly float _residentMult; // prestige bonus applied to every resident earned (1.0 = none)

        public float CurrentTopWidth { get; private set; }
        public float LeanOffset { get; private set; }
        public int MissStrikes { get; private set; }
        public int Score { get; private set; }           // sum of floor scores only (spec §6.1)
        public int PerfectChain { get; private set; }
        public int MaxPerfectChain { get; private set; } // longest chain reached this run (mission metric)
        public int ComboLevel { get; private set; }      // 0..cap: +1/Perfect, −1/Good, 0 on a strike
        public int MaxComboLevel { get; private set; }   // highest combo level reached this run
        public UpgradeTier PendingUpgrade { get; private set; } // earned block upgrade for the NEXT spawn (Phase C)

        /// <summary>Run-end "trophy roof" bonus residents for this run's longest Perfect chain (Phase C),
        /// scaled by the prestige residents multiplier.</summary>
        public int TrophyRoofResidents => (int)(Scoring.TrophyRoofBonus(_cfg, MaxPerfectChain) * _residentMult);
        public int TotalPerfects { get; private set; }   // cumulative Perfect drops (coins/stats — meta §5)
        public int FloorCount { get; private set; }     // placed floors, excluding the base
        public int TotalResidents { get; private set; }
        public bool IsOver { get; private set; }

        /// <summary>Total run score = floor scores + resident bonus (spec §6.3).</summary>
        public int RunScore => Score + TotalResidents * _cfg.ResidentScoreValue;

        public TowerRun(CoreConfig cfg, float residentMult = 1.0f)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            if (cfg.ComboResidentBonus is null || cfg.ComboResidentBonus.Length <= cfg.ComboLevelCap)
                throw new ArgumentException("ComboResidentBonus must have an entry for every level 0..ComboLevelCap.", nameof(cfg));
            _cfg = cfg;
            _residentMult = residentMult > 0f ? residentMult : 1.0f;
            CurrentTopWidth = cfg.InitialBlockWidth;
        }

        /// <summary>Place the next block at horizontal <paramref name="offsetX"/> from the tower top center.</summary>
        public DropOutcome PlaceBlock(FloorType type, float offsetX)
        {
            if (IsOver) throw new InvalidOperationException("The run is already over.");

            Grade grade = Grading.Evaluate(_cfg, offsetX, CurrentTopWidth);
            int scoreGained = 0;
            int residentsAdded = 0;
            int comboBonusScore = 0;
            // Tower-Bloxx: only a clean-enough catch (Perfect/Good) lands; Sloppy & Miss tip off and fall.
            bool floorPlaced = grade == Grade.Perfect || grade == Grade.Good;

            switch (grade)
            {
                case Grade.Perfect:
                    PerfectChain += 1;
                    if (PerfectChain > MaxPerfectChain) MaxPerfectChain = PerfectChain;
                    TotalPerfects += 1;
                    ComboLevel += 1; // each Perfect raises the combo bar
                    if (ComboLevel > MaxComboLevel) MaxComboLevel = ComboLevel;
                    // Phase C: a streak milestone arms an upgrade for the next spawned block (upgrade-only).
                    UpgradeTier earned = EvaluateStreakUpgrade(_cfg, PerfectChain);
                    if (earned > PendingUpgrade) PendingUpgrade = earned;
                    LeanOffset *= 1f - _cfg.PerfectLeanCorrectionFraction;
                    // Tower-Bloxx: residents = base + the Perfect bonus + the live combo bonus (level reached).
                    residentsAdded = Scoring.BaseResidents(_cfg, type) + Scoring.PerfectResidentBonus(_cfg, type)
                                   + Scoring.ComboResidentBonus(_cfg, ComboLevel);
                    scoreGained = Scoring.FloorScore(_cfg, type, grade, PerfectChain);
                    if (ComboLevel >= _cfg.ComboLevelCap) // the bar filled → award the completion bonus, then start over
                    {
                        comboBonusScore = _cfg.ComboCompleteScoreBonus;
                        scoreGained += comboBonusScore;
                        ComboLevel = 0;
                    }
                    FloorCount += 1;
                    break;

                case Grade.Good:
                    // Tower-Bloxx pivot: the block stays WHOLE (no slice). Top width is constant;
                    // the overhang accumulates as lean that drives the building's sway.
                    LeanOffset += offsetX * _cfg.GoodLeanFactor;
                    PerfectChain = 0;
                    // A Good HOLDS the combo level (only a strike breaks it) — so the on-screen series bar fills
                    // on Perfects and resets only on a miss. This floor still earns the live combo bonus.
                    residentsAdded = Scoring.BaseResidents(_cfg, type) + Scoring.ComboResidentBonus(_cfg, ComboLevel);
                    scoreGained = Scoring.FloorScore(_cfg, type, grade, PerfectChain);
                    FloorCount += 1;
                    break;

                case Grade.Sloppy:
                    // Too much overhang — the block tips off (not placed). Costs a strike + breaks the combo.
                    PerfectChain = 0;
                    ComboLevel = 0;
                    PendingUpgrade = UpgradeTier.None; // a strike cancels any earned-but-unspent upgrade
                    if (_cfg.SloppyCostsStrike) MissStrikes += 1;
                    break;

                default: // Miss — block misses entirely and falls. Costs a strike + breaks the combo.
                    PerfectChain = 0;
                    ComboLevel = 0;
                    PendingUpgrade = UpgradeTier.None; // a strike cancels any earned-but-unspent upgrade
                    MissStrikes += 1;
                    break;
            }

            residentsAdded = (int)(residentsAdded * _residentMult); // prestige bonus
            Score += scoreGained;
            TotalResidents += residentsAdded;

            bool toppled = MissStrikes >= _cfg.StrikeLimit;
            if (toppled) IsOver = true;

            return new DropOutcome(grade, floorPlaced, scoreGained, residentsAdded,
                CurrentTopWidth, LeanOffset, MissStrikes, PerfectChain, ComboLevel, comboBonusScore, toppled);
        }

        /// <summary>Resolve the floor type for the NEXT block to spawn: raise the seeded type to any earned
        /// pending upgrade (upgrade-only — never lowers it), then consume the upgrade. Pure + deterministic
        /// (no RNG); call it once, immediately before instantiating the next block.</summary>
        public FloorType NextSpawnType(FloorType seededType)
        {
            var resolved = (FloorType)Math.Max((int)seededType, (int)PendingUpgrade);
            PendingUpgrade = UpgradeTier.None;
            return resolved;
        }

        // A streak milestone (a multiple of Tier1Threshold consecutive Perfects) earns an upgrade:
        // Premium at/after Tier2Threshold, else Balcony. Returns None between milestones.
        static UpgradeTier EvaluateStreakUpgrade(CoreConfig cfg, int chain)
        {
            int t1 = cfg.StreakUpgradeTier1Threshold;
            if (t1 <= 0 || chain < t1 || (chain - t1) % t1 != 0) return UpgradeTier.None;
            return chain >= cfg.StreakUpgradeTier2Threshold ? UpgradeTier.Premium : UpgradeTier.Balcony;
        }
    }
}
