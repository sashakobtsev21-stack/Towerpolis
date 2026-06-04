using Towerpolis.Core.Meta;

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
            new AchievementDef(new AchievementInfo("ach_towers_5",      AchievementMetric.TotalTowers,        5,     100), "Первая пятёрка", "Построй 5 башен"),
            new AchievementDef(new AchievementInfo("ach_towers_50",     AchievementMetric.TotalTowers,        50,    200), "Застройщик",     "Построй 50 башен"),
            new AchievementDef(new AchievementInfo("ach_towers_200",    AchievementMetric.TotalTowers,        200,   300), "Мегаполис",      "Построй 200 башен"),
            new AchievementDef(new AchievementInfo("ach_residents_1k",  AchievementMetric.TotalResidents,     1000,  150), "Тысяча",         "Посели 1 000 жильцов"),
            new AchievementDef(new AchievementInfo("ach_residents_10k", AchievementMetric.TotalResidents,     10000, 400), "Десять тысяч",   "Посели 10 000 жильцов"),
            new AchievementDef(new AchievementInfo("ach_perfects_100",  AchievementMetric.TotalPerfects,      100,   200), "Снайпер",        "100 идеальных"),
            new AchievementDef(new AchievementInfo("ach_streak_7",      AchievementMetric.LongestStreak,      7,     200), "Неделя подряд",  "Серия 7 дней"),
            new AchievementDef(new AchievementInfo("ach_streak_30",     AchievementMetric.LongestStreak,      30,    500), "Месяц подряд",   "Серия 30 дней"),
            new AchievementDef(new AchievementInfo("ach_height_50",     AchievementMetric.BestFloorCount,     50,    250), "Небоскрёб",      "50 этажей за забег"),
            new AchievementDef(new AchievementInfo("ach_d3_complete",   AchievementMetric.DistrictsCompleted, 3,     300), "Три района",     "Заверши все 3 района"),
        };

        public static readonly AchievementInfo[] Infos;

        static AchievementCatalog()
        {
            Infos = new AchievementInfo[All.Length];
            for (int i = 0; i < All.Length; i++) Infos[i] = All[i].Info;
        }
    }
}
