using System;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Meta
{
    /// <summary>
    /// Pure, deterministic upgrade economy (progression-spec §2 / §9 Step 1): purchase validation plus the
    /// effective gameplay effect of an upgrade level. NOTHING here mutates state — <see cref="TryPurchase"/>
    /// returns the proposed result and the caller applies it. The gameplay track (Magnet) returns a
    /// NO-EFFECT value in Daily Seed, so a daily run is byte-for-byte identical to an unupgraded run and
    /// the shared seed stays fair (no branch in the grading path — the caller just gets the neutral value).
    /// </summary>
    public static class UpgradeService
    {
        /// <summary>
        /// Try to buy the next level of an upgrade. <paramref name="costs"/>[i] is the cost to go from level
        /// i to i+1. Returns (ok, newCoins, newLevel); on failure newCoins/newLevel are returned unchanged.
        /// Never mutates and never lets coins go negative. <paramref name="upgradeId"/> is the caller's
        /// stable key for a single unified call site (telemetry/UI) — the math is driven by costs+level.
        /// </summary>
        public static (bool ok, int newCoins, int newLevel) TryPurchase(
            string upgradeId, int currentLevel, int[] costs, int maxLevel, int currentCoins)
        {
            if (costs == null) throw new ArgumentNullException(nameof(costs));

            if (currentLevel < 0) currentLevel = 0;
            if (currentLevel >= maxLevel || currentLevel >= costs.Length)
                return (false, currentCoins, currentLevel); // already at cap

            int cost = costs[currentLevel];
            if (currentCoins < cost)
                return (false, currentCoins, currentLevel); // can't afford → no change

            return (true, currentCoins - cost, currentLevel + 1);
        }

        /// <summary>Effective auto-centre fraction for the magnet at this level — always 0 (no help) in Daily.</summary>
        public static float GetMagnetFraction(int level, CoreConfig cfg, bool isDaily)
        {
            if (isDaily) return 0f; // suppressed regardless of level — daily fairness
            if (cfg == null) throw new ArgumentNullException(nameof(cfg));
            return Sample(cfg.MagnetFractions, level, 0f);
        }

        // Clamp the level into the table; an empty/absent table falls back to the neutral default.
        static float Sample(float[] table, int level, float fallback)
        {
            if (table == null || table.Length == 0) return fallback;
            if (level < 0) level = 0;
            if (level >= table.Length) level = table.Length - 1;
            return table[level];
        }
    }
}
