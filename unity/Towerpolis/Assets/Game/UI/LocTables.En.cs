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

            // ----- Meta HUD chrome -----
            { LocKeys.MetaMenu,           "MENU" },
            { LocKeys.MetaCity,           "CITY" },
            { LocKeys.MetaBonuses,        "BONUSES" },
            { LocKeys.MetaGoals,          "GOALS" },
            { LocKeys.MetaClose,          "CLOSE" },
            { LocKeys.MetaUpgradesTitle,  "UPGRADES" },
            { LocKeys.MetaSettings,       "SETTINGS" },
            { LocKeys.MetaLanguage,       "LANGUAGE" },
            { LocKeys.MetaSoundOn,        "SOUND ON" },
            { LocKeys.MetaSoundOff,       "SOUND OFF" },
            { LocKeys.MetaReset,          "RESET" },
            { LocKeys.MetaResetConfirm,   "SURE?" },
            { LocKeys.MetaSectionWeekly,  "WEEKLY MISSIONS" },
            { LocKeys.MetaSectionAchieve, "ACHIEVEMENTS" },
            { LocKeys.MetaUpgHint,        "Coins: +1 per floor · +2 per perfect drop · district/goal rewards" },

            // ----- Meta HUD dynamic -----
            { LocKeys.MetaResidents,    "RESIDENTS  {0}" },
            { LocKeys.MetaPopulation,   "POPULATION  {0}  /  {1}" },
            { LocKeys.MetaCoins,        "COINS  {0}" },
            { LocKeys.MetaUpgRow,       "{0}   Lv {1} / {2}" },
            { LocKeys.MetaBuy,          "BUY {0}" },
            { LocKeys.MetaMax,          "MAX" },
            { LocKeys.MetaLoginClaim,   "CLAIM GIFT" },
            { LocKeys.MetaLoginTaken,   "GIFT CLAIMED" },
            { LocKeys.MetaDone,         "DONE" },
            { LocKeys.MetaMissionLine,  "{0}    {1}/{2}    +{3}" },
            { LocKeys.MetaAchLine,      "{0} — {1}" },

            // ----- Toasts -----
            { LocKeys.ToastDistrict,    "DISTRICT COMPLETE!" },
            { LocKeys.ToastMission,     "MISSION COMPLETE\n{0}   +{1}" },
            { LocKeys.ToastAchievement, "ACHIEVEMENT\n{0}   +{1}" },

            // ----- Upgrades -----
            { LocKeys.UpgMagnetName,    "MAGNET" },
            { LocKeys.UpgMagnetDesc,    "Nudges the block toward centre (Endless)" },
            { LocKeys.UpgCityBonusName, "CITY BONUS" },
            { LocKeys.UpgCityBonusDesc, "More coins for completing a district" },

            // ----- Districts -----
            { LocKeys.DistDowntownShort, "DOWNTOWN" },
            { LocKeys.DistNeonShort,     "NEON" },
            { LocKeys.DistWinterShort,   "WINTER" },
            { LocKeys.DistDowntownName,  "DOWNTOWN" },
            { LocKeys.DistNeonName,      "NEON QUARTER" },
            { LocKeys.DistWinterName,    "WINTER HEIGHTS" },

            // ----- Weekly missions -----
            { LocKeys.MissionFloorsName,    "Builder" },   { LocKeys.MissionFloorsDesc,    "Place 200 floors" },
            { LocKeys.MissionPerfectsName,  "Sharp" },     { LocKeys.MissionPerfectsDesc,  "50 perfect drops" },
            { LocKeys.MissionDailyName,     "Regular" },   { LocKeys.MissionDailyDesc,     "Play Daily 5 days" },
            { LocKeys.MissionTallName,      "High-rise" }, { LocKeys.MissionTallDesc,      "40 floors in one run" },
            { LocKeys.MissionChainName,     "On Fire" },   { LocKeys.MissionChainDesc,     "A chain of 8 perfects" },
            { LocKeys.MissionResidentsName, "Landlord" },  { LocKeys.MissionResidentsDesc, "House 400 residents" },
            { LocKeys.MissionDistrictName,  "Local" },     { LocKeys.MissionDistrictDesc,  "Finish 6 runs" },
            { LocKeys.MissionStreakName,    "Devoted" },   { LocKeys.MissionStreakDesc,    "A 5-day streak" },

            // ----- Achievements -----
            { LocKeys.AchTowers5Name,     "First Five" },     { LocKeys.AchTowers5Desc,     "Build 5 towers" },
            { LocKeys.AchTowers50Name,    "Developer" },      { LocKeys.AchTowers50Desc,    "Build 50 towers" },
            { LocKeys.AchTowers200Name,   "Metropolis" },     { LocKeys.AchTowers200Desc,   "Build 200 towers" },
            { LocKeys.AchResidents1kName, "Thousand" },       { LocKeys.AchResidents1kDesc, "House 1,000 residents" },
            { LocKeys.AchResidents10kName,"Ten Thousand" },   { LocKeys.AchResidents10kDesc,"House 10,000 residents" },
            { LocKeys.AchPerfects100Name, "Sniper" },         { LocKeys.AchPerfects100Desc, "100 perfects" },
            { LocKeys.AchStreak7Name,     "Week Straight" },  { LocKeys.AchStreak7Desc,     "A 7-day streak" },
            { LocKeys.AchStreak30Name,    "Month Straight" }, { LocKeys.AchStreak30Desc,    "A 30-day streak" },
            { LocKeys.AchHeight50Name,    "Skyscraper" },     { LocKeys.AchHeight50Desc,    "50 floors in one run" },
            { LocKeys.AchD3Name,          "Three Districts" },{ LocKeys.AchD3Desc,          "Finish all 3 districts" },

            // ----- Cosmetic skins -----
            { LocKeys.SkinClassic,  "Classic" },
            { LocKeys.SkinPastel,   "Pastel" },
            { LocKeys.SkinSteel,    "Steel" },
            { LocKeys.SkinNeonGlow, "Neon Glow" },
            { LocKeys.SkinArctic,   "Arctic" },
            { LocKeys.CraneHemp,    "Hemp" },
            { LocKeys.CraneSteel,   "Steel" },
            { LocKeys.CraneGold,    "Gold" },
            { LocKeys.CraneNeon,    "Neon" },
        };
    }
}
