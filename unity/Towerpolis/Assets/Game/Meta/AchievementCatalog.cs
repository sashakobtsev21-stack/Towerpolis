using Towerpolis.Core.Meta;
using Towerpolis.Game.UI;

namespace Towerpolis.Game.Meta
{
    /// <summary>An achievement: the Core <see cref="AchievementInfo"/> plus its display text
    /// (progression-spec §4.3). Permanent, one-time, pays coins on unlock.</summary>
    public readonly struct AchievementDef
    {
        public readonly AchievementInfo Info;
        public readonly string Name, Description;
        public AchievementDef(AchievementInfo info, string name, string description)
        {
            Info = info; Name = name; Description = description;
        }
    }

    /// <summary>The 10 permanent achievements (progression-spec §4.3).</summary>
    public static class AchievementCatalog
    {
        public static readonly AchievementDef[] All =
        {
            new AchievementDef(new AchievementInfo("ach_towers_5",      AchievementMetric.TotalTowers,        5,     100), LocKeys.AchTowers5Name,     LocKeys.AchTowers5Desc),
            new AchievementDef(new AchievementInfo("ach_towers_50",     AchievementMetric.TotalTowers,        50,    200), LocKeys.AchTowers50Name,    LocKeys.AchTowers50Desc),
            new AchievementDef(new AchievementInfo("ach_towers_200",    AchievementMetric.TotalTowers,        200,   300), LocKeys.AchTowers200Name,   LocKeys.AchTowers200Desc),
            new AchievementDef(new AchievementInfo("ach_residents_1k",  AchievementMetric.TotalResidents,     1000,  150), LocKeys.AchResidents1kName, LocKeys.AchResidents1kDesc),
            new AchievementDef(new AchievementInfo("ach_residents_10k", AchievementMetric.TotalResidents,     10000, 400), LocKeys.AchResidents10kName,LocKeys.AchResidents10kDesc),
            new AchievementDef(new AchievementInfo("ach_perfects_100",  AchievementMetric.TotalPerfects,      100,   200), LocKeys.AchPerfects100Name, LocKeys.AchPerfects100Desc),
            new AchievementDef(new AchievementInfo("ach_streak_7",      AchievementMetric.LongestStreak,      7,     200), LocKeys.AchStreak7Name,     LocKeys.AchStreak7Desc),
            new AchievementDef(new AchievementInfo("ach_streak_30",     AchievementMetric.LongestStreak,      30,    500), LocKeys.AchStreak30Name,    LocKeys.AchStreak30Desc),
            new AchievementDef(new AchievementInfo("ach_height_50",     AchievementMetric.BestFloorCount,     50,    250), LocKeys.AchHeight50Name,    LocKeys.AchHeight50Desc),
            new AchievementDef(new AchievementInfo("ach_d3_complete",   AchievementMetric.DistrictsCompleted, 3,     300), LocKeys.AchD3Name,          LocKeys.AchD3Desc),
        };

        public static readonly AchievementInfo[] Infos;

        static AchievementCatalog()
        {
            Infos = new AchievementInfo[All.Length];
            for (int i = 0; i < All.Length; i++) Infos[i] = All[i].Info;
        }
    }
}
