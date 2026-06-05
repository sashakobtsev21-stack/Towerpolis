using System.Collections.Generic;

namespace Towerpolis.Game.UI
{
    /// <summary>English string table (ADR-0008). Keep keys + {n} placeholders in sync with
    /// <see cref="LocTables.Ru"/> — the LocCompletenessTests assert parity.</summary>
    public static partial class LocTables
    {
        public static readonly Dictionary<string, string> En = new()
        {
            // ----- Gameplay HUD -----
            { LocKeys.HudPerfect,    "PERFECT!" },
            { LocKeys.HudRetry,      "AGAIN" },
            { LocKeys.HudSummit,     "SUMMIT!\n{0} FLOORS" },
            { LocKeys.HudRecord,     "RECORD!" },
            { LocKeys.HudBest,       "BEST  {0}" },
            { LocKeys.HudChain,      "×{0}" },
            { LocKeys.HudStreak,     "STREAK ×{0}" },
            { LocKeys.HudRunCoins,   "+{0} COINS\nfloors {1}  ·  perfect {2}  ·  total {3}" },
            { LocKeys.HudTrophyLine, "\nSTREAK TROPHY  +{0} residents" },
        };
    }
}
