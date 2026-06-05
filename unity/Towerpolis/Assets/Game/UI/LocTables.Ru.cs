using System.Collections.Generic;

namespace Towerpolis.Game.UI
{
    /// <summary>Russian string table (ADR-0008). Keep keys in sync with <see cref="LocTables.En"/> —
    /// the LocCompletenessTests assert parity. Use {0},{1}… for runtime values, never concatenation.</summary>
    public static partial class LocTables
    {
        public static readonly Dictionary<string, string> Ru = new()
        {
            // ----- Gameplay HUD -----
            { LocKeys.HudPerfect,    "ИДЕАЛЬНО!" },
            { LocKeys.HudRetry,      "ЕЩЁ РАЗ" },
            { LocKeys.HudSummit,     "ВЕРШИНА!\n{0} ЭТАЖЕЙ" },
            { LocKeys.HudRecord,     "РЕКОРД!" },
            { LocKeys.HudBest,       "ЛУЧШЕЕ  {0}" },
            { LocKeys.HudChain,      "×{0}" },
            { LocKeys.HudStreak,     "СЕРИЯ ×{0}" },
            { LocKeys.HudRunCoins,   "+{0} МОНЕТ\nэтажи {1}  ·  идеально {2}  ·  всего {3}" },
            { LocKeys.HudTrophyLine, "\nТРОФЕЙ ЗА СЕРИЮ  +{0} жильцов" },
        };
    }
}
