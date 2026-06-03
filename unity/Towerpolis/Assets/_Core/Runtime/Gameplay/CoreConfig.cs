namespace Towerpolis.Core.Gameplay
{
    /// <summary>
    /// All Core-side gameplay tunables (grading, lean, strikes, scoring, residents). Defaults are the
    /// MVP feel-spec v1.1 values (ratified by game-director). The Unity layer backs this with a
    /// ScriptableObject so designers tune in-editor; Core only defines the plain, engine-free data.
    /// </summary>
    public sealed class CoreConfig
    {
        // --- Grading: thresholds are fractions of the CURRENT top width, not a constant (spec §3, R2) ---
        public float PerfectThreshold = 0.10f;
        public float GoodThreshold = 0.30f;
        public float SloppyThreshold = 0.50f;
        public float InitialBlockWidth = 2.0f;
        public float MinBlockWidth = 0.4f;

        // --- Lean (spec §3.1, §3.2) ---
        public float GoodLeanFactor = 0.15f;
        public float SloppyLeanFactor = 0.35f;
        public float PerfectLeanCorrectionFraction = 0.25f;

        // --- Rules (spec §4) ---
        public int StrikeLimit = 2;
        public bool SloppyCostsStrike = true; // OQ-01 ruling: default true

        // --- Score (spec §6) ---
        public int ScoreStandard = 100;
        public int ScoreBalcony = 150;
        public int ScorePremium = 200;
        // NOTE: FloorScore truncates (int)(base × multiplier). Defaults give exact integers; when the
        // ScriptableObject backing lands, validate that custom multipliers don't truncate surprisingly.
        public float MultiplierPerfect = 2.0f;
        public float MultiplierGood = 1.0f;
        public float MultiplierSloppy = 0.5f;
        public float MultiplierMiss = 0.0f;
        public int ChainBonus1To2 = 50;
        public int ChainBonus3To5 = 150;
        public int ChainBonus6To10 = 350;
        public int ChainBonus11Plus = 600;

        // --- Residents (spec §6.3, §9) ---
        public int ResidentsStandard = 2;
        public int ResidentsBalcony = 3;
        public int ResidentsPremium = 4;
        public int PerfectResidentBonus = 1;
        public int ResidentScoreValue = 10;
    }
}
