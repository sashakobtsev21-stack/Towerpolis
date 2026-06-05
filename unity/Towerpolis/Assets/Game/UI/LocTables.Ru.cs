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

            // ----- Meta HUD chrome -----
            { LocKeys.MetaCity,           "ГОРОД" },
            { LocKeys.MetaBonuses,        "БОНУСЫ" },
            { LocKeys.MetaSkins,          "СКИНЫ" },
            { LocKeys.MetaGoals,          "ЦЕЛИ" },
            { LocKeys.MetaClose,          "ЗАКРЫТЬ" },
            { LocKeys.MetaUpgradesTitle,  "УЛУЧШЕНИЯ" },
            { LocKeys.MetaSectionBlocks,  "БЛОКИ" },
            { LocKeys.MetaSectionCrane,   "КРАН" },
            { LocKeys.MetaSectionWeekly,  "МИССИИ НЕДЕЛИ" },
            { LocKeys.MetaSectionAchieve, "ДОСТИЖЕНИЯ" },
            { LocKeys.MetaUpgHint,        "Монеты: +1 за этаж · +2 за идеальную постановку · награды за район/цели" },

            // ----- Meta HUD dynamic -----
            { LocKeys.MetaResidents,    "ЖИЛЬЦЫ  {0}" },
            { LocKeys.MetaPopulation,   "НАСЕЛЕНИЕ  {0}  /  {1}" },
            { LocKeys.MetaCoins,        "МОНЕТЫ  {0}" },
            { LocKeys.MetaUpgRow,       "{0}   Ур {1} / {2}" },
            { LocKeys.MetaBuy,          "КУПИТЬ {0}" },
            { LocKeys.MetaMax,          "МАКС" },
            { LocKeys.MetaLoginClaim,   "ЗАБРАТЬ ПОДАРОК" },
            { LocKeys.MetaLoginTaken,   "ПОДАРОК ВЗЯТ" },
            { LocKeys.MetaSkinEquipped, "НАДЕТО" },
            { LocKeys.MetaSkinEquip,    "НАДЕТЬ" },
            { LocKeys.MetaSkinLocked,   "ЗАКРЫТО" },
            { LocKeys.MetaDone,         "ГОТОВО" },
            { LocKeys.MetaMissionLine,  "{0}    {1}/{2}    +{3}" },
            { LocKeys.MetaAchLine,      "{0} — {1}" },

            // ----- Toasts -----
            { LocKeys.ToastDistrict,    "РАЙОН ЗАВЕРШЁН!" },
            { LocKeys.ToastMission,     "МИССИЯ ВЫПОЛНЕНА\n{0}   +{1}" },
            { LocKeys.ToastAchievement, "ДОСТИЖЕНИЕ\n{0}   +{1}" },

            // ----- Upgrades -----
            { LocKeys.UpgMagnetName,    "МАГНИТ" },
            { LocKeys.UpgMagnetDesc,    "Подтягивает блок к центру (Endless)" },
            { LocKeys.UpgCityBonusName, "БОНУС ГОРОДА" },
            { LocKeys.UpgCityBonusDesc, "Больше монет за достройку района" },

            // ----- Districts -----
            { LocKeys.DistDowntownShort, "ЦЕНТР" },
            { LocKeys.DistNeonShort,     "НЕОН" },
            { LocKeys.DistWinterShort,   "ЗИМА" },
            { LocKeys.DistDowntownName,  "ЦЕНТР" },
            { LocKeys.DistNeonName,      "НЕОНОВЫЙ КВАРТАЛ" },
            { LocKeys.DistWinterName,    "ЗИМНИЕ ВЫСОТЫ" },

            // ----- Weekly missions -----
            { LocKeys.MissionFloorsName,    "Строитель" },    { LocKeys.MissionFloorsDesc,    "Построй 200 этажей" },
            { LocKeys.MissionPerfectsName,  "Меткий" },       { LocKeys.MissionPerfectsDesc,  "50 идеальных постановок" },
            { LocKeys.MissionDailyName,     "Завсегдатай" },  { LocKeys.MissionDailyDesc,     "Сыграй «День» 5 дней" },
            { LocKeys.MissionTallName,      "Высотка" },      { LocKeys.MissionTallDesc,      "40 этажей за забег" },
            { LocKeys.MissionChainName,     "В ударе" },      { LocKeys.MissionChainDesc,     "Серия из 8 идеальных" },
            { LocKeys.MissionResidentsName, "Домовладелец" }, { LocKeys.MissionResidentsDesc, "Посели 400 жильцов" },
            { LocKeys.MissionDistrictName,  "Местный" },      { LocKeys.MissionDistrictDesc,  "Заверши 6 забегов" },
            { LocKeys.MissionStreakName,    "Преданный" },    { LocKeys.MissionStreakDesc,    "Серия в 5 дней" },

            // ----- Achievements -----
            { LocKeys.AchTowers5Name,     "Первая пятёрка" }, { LocKeys.AchTowers5Desc,     "Построй 5 башен" },
            { LocKeys.AchTowers50Name,    "Застройщик" },     { LocKeys.AchTowers50Desc,    "Построй 50 башен" },
            { LocKeys.AchTowers200Name,   "Мегаполис" },      { LocKeys.AchTowers200Desc,   "Построй 200 башен" },
            { LocKeys.AchResidents1kName, "Тысяча" },         { LocKeys.AchResidents1kDesc, "Посели 1 000 жильцов" },
            { LocKeys.AchResidents10kName,"Десять тысяч" },   { LocKeys.AchResidents10kDesc,"Посели 10 000 жильцов" },
            { LocKeys.AchPerfects100Name, "Снайпер" },        { LocKeys.AchPerfects100Desc, "100 идеальных" },
            { LocKeys.AchStreak7Name,     "Неделя подряд" },  { LocKeys.AchStreak7Desc,     "Серия 7 дней" },
            { LocKeys.AchStreak30Name,    "Месяц подряд" },   { LocKeys.AchStreak30Desc,    "Серия 30 дней" },
            { LocKeys.AchHeight50Name,    "Небоскрёб" },      { LocKeys.AchHeight50Desc,    "50 этажей за забег" },
            { LocKeys.AchD3Name,          "Три района" },     { LocKeys.AchD3Desc,          "Заверши все 3 района" },

            // ----- Cosmetic skins -----
            { LocKeys.SkinClassic,  "Классика" },
            { LocKeys.SkinPastel,   "Пастель" },
            { LocKeys.SkinSteel,    "Сталь" },
            { LocKeys.SkinNeonGlow, "Неон" },
            { LocKeys.SkinArctic,   "Арктика" },
            { LocKeys.CraneHemp,    "Канат" },
            { LocKeys.CraneSteel,   "Сталь" },
            { LocKeys.CraneGold,    "Золото" },
            { LocKeys.CraneNeon,    "Неон" },
        };
    }
}
