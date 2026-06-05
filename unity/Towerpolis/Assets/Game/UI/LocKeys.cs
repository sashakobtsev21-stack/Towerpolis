namespace Towerpolis.Game.UI
{
    /// <summary>Localization key constants (ADR-0008), grouped by screen. Compile-checked, greppable —
    /// never type a raw key string at a call site. RU/EN values live in <see cref="LocTables"/>.</summary>
    public static class LocKeys
    {
        // ----- Gameplay HUD (HUDController) -----
        public const string HudPerfect    = "hud.perfect";     // "ИДЕАЛЬНО!"
        public const string HudRetry      = "hud.retry";       // "ЕЩЁ РАЗ"
        public const string HudSummit     = "hud.summit";      // "ВЕРШИНА!\n{0} ЭТАЖЕЙ"
        public const string HudRecord     = "hud.record";      // "РЕКОРД!"
        public const string HudBest       = "hud.best";        // "ЛУЧШЕЕ  {0}"
        public const string HudChain      = "hud.chain";       // "×{0}"
        public const string HudStreak     = "hud.streak";      // "СЕРИЯ ×{0}"
        public const string HudRunCoins   = "hud.runcoins";    // "+{0} МОНЕТ\nэтажи {1} · идеально {2} · всего {3}"
        public const string HudTrophyLine = "hud.trophyline";  // "\nТРОФЕЙ ЗА СЕРИЮ  +{0} жильцов"
    }
}
