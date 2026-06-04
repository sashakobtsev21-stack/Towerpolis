using System;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Meta
{
    /// <summary>
    /// Deterministic coin earn for a run (meta-spec §5): coins = floors × CoinPerFloor + perfects ×
    /// CoinBonusPerfect. Residents deliberately earn nothing (they are the meta-score). Daily-win and
    /// streak-milestone bonuses are separate, banked by the meta flow — see <see cref="DailyStreak"/>.
    /// Unity-free and NUnit-tested so the same run always yields the same coins (ADR-0002).
    /// </summary>
    public static class CoinEarnCalculator
    {
        public static int RunCoins(in RunResult result, CoreConfig cfg)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            return result.FloorCount * cfg.CoinPerFloor + result.PerfectDrops * cfg.CoinBonusPerfect;
        }

        /// <summary>
        /// Run coins PLUS the district-completion lump (progression-spec §2.5). The City Bonus upgrade
        /// multiplies ONLY the district reward — never the per-floor/per-perfect coins — so it can't inflate
        /// the daily economy and is safe in Daily Seed. The reward is added only when a district was
        /// completed THIS run.
        /// </summary>
        public static int RunCoins(in RunResult result, CoreConfig cfg,
            bool districtCompletedNow, int baseDistrictRewardCoins, int cityBonusLevel)
        {
            int runCoins = RunCoins(in result, cfg); // null-checks cfg
            if (!districtCompletedNow) return runCoins;
            return runCoins + CityBonusedReward(baseDistrictRewardCoins, cityBonusLevel, cfg);
        }

        /// <summary>Apply the City Bonus multiplier to a district-completion reward: floor(base × mult[level]).</summary>
        public static int CityBonusedReward(int baseRewardCoins, int cityBonusLevel, CoreConfig cfg)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            float[] mult = cfg.CityBonusMultipliers;
            if (mult == null || mult.Length == 0) return baseRewardCoins;
            int lvl = cityBonusLevel < 0 ? 0 : (cityBonusLevel >= mult.Length ? mult.Length - 1 : cityBonusLevel);
            return (int)System.Math.Floor(baseRewardCoins * mult[lvl]);
        }
    }
}
