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
    }
}
