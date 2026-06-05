namespace Towerpolis.Game.UI
{
    /// <summary>Localization key constants (ADR-0008), grouped by area. Compile-checked, greppable —
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

        // ----- Meta HUD (MetaHud) chrome -----
        public const string MetaCity            = "meta.btn.city";       // top-bar "ГОРОД"
        public const string MetaBonuses         = "meta.btn.bonuses";    // top-bar "БОНУСЫ"
        public const string MetaSkins           = "meta.btn.skins";      // top-bar + panel title "СКИНЫ"
        public const string MetaGoals           = "meta.btn.goals";      // top-bar + panel title "ЦЕЛИ"
        public const string MetaClose           = "meta.close";          // "ЗАКРЫТЬ"
        public const string MetaUpgradesTitle   = "meta.title.upgrades"; // panel title "УЛУЧШЕНИЯ"
        public const string MetaSettings        = "meta.settings";       // button + panel title "НАСТРОЙКИ"
        public const string MetaLanguage        = "meta.language";        // section "ЯЗЫК"
        public const string MetaSectionBlocks   = "meta.section.blocks"; // "БЛОКИ"
        public const string MetaSectionCrane    = "meta.section.crane";  // "КРАН"
        public const string MetaSectionWeekly   = "meta.section.weekly"; // "МИССИИ НЕДЕЛИ"
        public const string MetaSectionAchieve  = "meta.section.achieve";// "ДОСТИЖЕНИЯ"
        public const string MetaUpgHint         = "meta.upg.hint";       // coin-earning hint line

        // ----- Meta HUD dynamic labels -----
        public const string MetaResidents   = "meta.residents";    // "ЖИЛЬЦЫ  {0}"
        public const string MetaPopulation  = "meta.population";   // "НАСЕЛЕНИЕ  {0}  /  {1}"
        public const string MetaCoins       = "meta.coins";        // "МОНЕТЫ  {0}"
        public const string MetaUpgRow      = "meta.upg.row";      // "{0}   Ур {1} / {2}"
        public const string MetaBuy         = "meta.buy";          // "КУПИТЬ {0}"
        public const string MetaMax         = "meta.max";          // "МАКС"
        public const string MetaLoginClaim  = "meta.login.claim";  // "ЗАБРАТЬ ПОДАРОК"
        public const string MetaLoginTaken  = "meta.login.taken";  // "ПОДАРОК ВЗЯТ"
        public const string MetaSkinEquipped = "meta.skin.equipped"; // "НАДЕТО"
        public const string MetaSkinEquip    = "meta.skin.equip";    // "НАДЕТЬ"
        public const string MetaSkinLocked   = "meta.skin.locked";   // "ЗАКРЫТО"
        public const string MetaDone        = "meta.done";         // "ГОТОВО"
        public const string MetaMissionLine = "meta.mission.line"; // "{0}    {1}/{2}    +{3}"
        public const string MetaAchLine     = "meta.ach.line";     // "{0} — {1}"

        // ----- Toasts -----
        public const string ToastDistrict    = "toast.district";    // "РАЙОН ЗАВЕРШЁН!"
        public const string ToastMission     = "toast.mission";     // "МИССИЯ ВЫПОЛНЕНА\n{0}   +{1}"
        public const string ToastAchievement = "toast.achievement"; // "ДОСТИЖЕНИЕ\n{0}   +{1}"

        // ----- Upgrade tracks -----
        public const string UpgMagnetName     = "upg.magnet.name";     // "МАГНИТ"
        public const string UpgMagnetDesc     = "upg.magnet.desc";
        public const string UpgCityBonusName  = "upg.citybonus.name";  // "БОНУС ГОРОДА"
        public const string UpgCityBonusDesc  = "upg.citybonus.desc";

        // ----- Districts: short (switch buttons) + full (panel title) -----
        public const string DistDowntownShort = "district.downtown.short"; // "ЦЕНТР"
        public const string DistNeonShort     = "district.neon.short";     // "НЕОН"
        public const string DistWinterShort   = "district.winter.short";   // "ЗИМА"
        public const string DistDowntownName  = "district.downtown.name";  // "ЦЕНТР"
        public const string DistNeonName      = "district.neon.name";      // "НЕОНОВЫЙ КВАРТАЛ"
        public const string DistWinterName    = "district.winter.name";    // "ЗИМНИЕ ВЫСОТЫ"

        // ----- Weekly missions (name + description) -----
        public const string MissionFloorsName = "mission.floors.name";       public const string MissionFloorsDesc = "mission.floors.desc";
        public const string MissionPerfectsName = "mission.perfects.name";   public const string MissionPerfectsDesc = "mission.perfects.desc";
        public const string MissionDailyName = "mission.daily.name";         public const string MissionDailyDesc = "mission.daily.desc";
        public const string MissionTallName = "mission.tall.name";           public const string MissionTallDesc = "mission.tall.desc";
        public const string MissionChainName = "mission.chain.name";         public const string MissionChainDesc = "mission.chain.desc";
        public const string MissionResidentsName = "mission.residents.name"; public const string MissionResidentsDesc = "mission.residents.desc";
        public const string MissionDistrictName = "mission.district.name";   public const string MissionDistrictDesc = "mission.district.desc";
        public const string MissionStreakName = "mission.streak.name";       public const string MissionStreakDesc = "mission.streak.desc";

        // ----- Achievements (name + description) -----
        public const string AchTowers5Name = "ach.towers5.name";       public const string AchTowers5Desc = "ach.towers5.desc";
        public const string AchTowers50Name = "ach.towers50.name";     public const string AchTowers50Desc = "ach.towers50.desc";
        public const string AchTowers200Name = "ach.towers200.name";   public const string AchTowers200Desc = "ach.towers200.desc";
        public const string AchResidents1kName = "ach.res1k.name";     public const string AchResidents1kDesc = "ach.res1k.desc";
        public const string AchResidents10kName = "ach.res10k.name";   public const string AchResidents10kDesc = "ach.res10k.desc";
        public const string AchPerfects100Name = "ach.perf100.name";   public const string AchPerfects100Desc = "ach.perf100.desc";
        public const string AchStreak7Name = "ach.streak7.name";       public const string AchStreak7Desc = "ach.streak7.desc";
        public const string AchStreak30Name = "ach.streak30.name";     public const string AchStreak30Desc = "ach.streak30.desc";
        public const string AchHeight50Name = "ach.height50.name";     public const string AchHeight50Desc = "ach.height50.desc";
        public const string AchD3Name = "ach.d3.name";                 public const string AchD3Desc = "ach.d3.desc";

        // ----- Cosmetic skins (display names) -----
        public const string SkinClassic  = "skin.classic";
        public const string SkinPastel   = "skin.pastel";
        public const string SkinSteel    = "skin.steel";
        public const string SkinNeonGlow = "skin.neonglow";
        public const string SkinArctic   = "skin.arctic";
        public const string CraneHemp    = "crane.hemp";
        public const string CraneSteel   = "crane.steel";
        public const string CraneGold    = "crane.gold";
        public const string CraneNeon    = "crane.neon";
    }
}
