using Towerpolis.Core.Meta;

namespace Towerpolis.Game.Meta
{
    /// <summary>A weekly-mission template: the Core <see cref="MissionInfo"/> plus its display text
    /// (progression-spec §4.1). Code-authored now; localisation tables + a ScriptableObject land in Phase 5.</summary>
    public readonly struct MissionDef
    {
        public readonly MissionInfo Info;
        public readonly string Name, Description;
        public MissionDef(MissionInfo info, string name, string description)
        {
            Info = info; Name = name; Description = description;
        }
    }

    /// <summary>The 8 weekly-mission templates (progression-spec §4.1). The same 3 are drawn worldwide each
    /// week from the Monday week-seed (see <c>MetaService</c>).</summary>
    public static class MissionCatalog
    {
        public static readonly MissionDef[] All =
        {
            new MissionDef(new MissionInfo("m_floors_weekly",    MissionMetric.FloorsPlaced,         200, 100), "Builder",      "Place 200 floors"),
            new MissionDef(new MissionInfo("m_perfects_weekly",  MissionMetric.PerfectDrops,         50,  120), "Sharp Week",   "Land 50 Perfect drops"),
            new MissionDef(new MissionInfo("m_daily_runs",       MissionMetric.DailyRunsCompleted,   5,   150), "Regular",      "Play the Daily on 5 days"),
            new MissionDef(new MissionInfo("m_tall_tower",       MissionMetric.TowerHeight,          40,  120), "High Rise",    "Reach 40 floors in one run"),
            new MissionDef(new MissionInfo("m_perfect_chain",    MissionMetric.PerfectChainLength,   8,   150), "On a Roll",    "Get a Perfect chain of 8"),
            new MissionDef(new MissionInfo("m_residents_weekly", MissionMetric.ResidentsHoused,      400, 100), "Landlord",     "House 400 residents"),
            new MissionDef(new MissionInfo("m_district_runs",    MissionMetric.DistrictRunsCompleted, 6,  80),  "Local",        "Complete 6 runs"),
            new MissionDef(new MissionInfo("m_streak_days",      MissionMetric.StreakDays,           5,   200), "Devoted",      "Reach a 5-day streak"),
        };

        public static readonly MissionInfo[] Infos;

        static MissionCatalog()
        {
            Infos = new MissionInfo[All.Length];
            for (int i = 0; i < All.Length; i++) Infos[i] = All[i].Info;
        }

        public static MissionDef Get(string id)
        {
            foreach (MissionDef m in All) if (m.Info.MissionId == id) return m;
            return All[0];
        }
    }
}
