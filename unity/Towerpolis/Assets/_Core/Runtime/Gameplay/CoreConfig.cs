namespace Towerpolis.Core.Gameplay
{
    /// <summary>
    /// All Core-side gameplay tunables (grading, lean, strikes, scoring, residents). Defaults are the
    /// MVP feel-spec v1.1 values (ratified by game-director). The Unity layer backs this with a
    /// ScriptableObject so designers tune in-editor; Core only defines the plain, engine-free data.
    /// </summary>
    public sealed class CoreConfig
    {
        // --- Grading (Tower-Bloxx): Perfect = snap-to-centre zone, Good = caught (overhangs), Miss =
        //     too little overlap so it bounces off. Thresholds are fractions of block width. ---
        public float PerfectThreshold = 0.15f; // within 15% of centre → snaps to centre, counts Perfect
        public float GoodThreshold = 0.80f;    // caught up to 80% offset (≥20% overlap); beyond → Miss
        public float SloppyThreshold = 0.80f;  // Sloppy band collapsed into Good (kept for enum compat)
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
        public int ResidentsPremium = 5; // owner ruling 2026-06-04 (was 4)
        // Perfect-drop resident bonus per type (owner 2026-06-04): Standard +1, Balcony +2, Premium +3.
        public int PerfectBonusStandard = 1;
        public int PerfectBonusBalcony = 2;
        public int PerfectBonusPremium = 3;
        public int ResidentScoreValue = 10;

        // --- Economy / meta (Phase 3 — meta-spec §5). Earn-only; spending is Phase 4. ---
        public int CoinPerFloor = 1;            // coins per placed floor (any grade)
        public int CoinBonusPerfect = 2;        // extra coins per Perfect drop
        public int DailySeedFirstWinCoins = 50; // once per UTC day, any completed daily run
        public int[] StreakMilestoneDays = { 3, 7, 14, 30 };
        public int[] StreakMilestoneCoins = { 75, 200, 400, 1000 };
    }
}
