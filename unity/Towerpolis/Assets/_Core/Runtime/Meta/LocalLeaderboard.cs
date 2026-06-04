#nullable enable
using System.Collections.Generic;

namespace Towerpolis.Core.Meta
{
    /// <summary>
    /// Local (solo) leaderboard records (meta-spec §4). Plain keyed int store so it serialises trivially
    /// and an online service can later POST the same key/value payload (ADR-0007). All "is-new-best"
    /// logic is here in Core (Unity-free, NUnit-tested); the Unity <c>ILeaderboardService</c> just wraps
    /// it. Streak lives in <see cref="DailyStreakState"/>, not here, to avoid a second source of truth.
    /// </summary>
    public sealed class LocalLeaderboard
    {
        public const string EndlessBest = "endless_best_score";
        public const string DailyBest = "daily_best_score";
        public const string StatTowers = "stat_towers_built";
        public const string StatResidents = "stat_total_residents";
        public const string StatPerfects = "stat_total_perfects";

        public static string DistrictBest(string id) => "district_best_" + id;
        public static string DistrictBestFloors(string id) => "district_best_floors_" + id;
        public static string DailyToday(string dateKey) => "daily_today_" + dateKey;

        readonly Dictionary<string, int> _records;

        public LocalLeaderboard() : this(null) { }

        public LocalLeaderboard(IDictionary<string, int>? seed)
            => _records = seed != null ? new Dictionary<string, int>(seed) : new Dictionary<string, int>();

        public IReadOnlyDictionary<string, int> Records => _records;

        public int Get(string key) => _records.TryGetValue(key, out int v) ? v : 0;

        /// <summary>Submit an endless run; returns true if it set a new all-time endless best.</summary>
        public bool SubmitEndless(in RunResult r, string districtId)
        {
            bool newBest = SetBest(EndlessBest, r.RunScore);
            SetBest(DistrictBest(districtId), r.RunScore);
            SetBest(DistrictBestFloors(districtId), r.FloorCount);
            RecordStats(in r);
            return newBest;
        }

        /// <summary>Submit a daily-seed run for <paramref name="dateKey"/>; returns true on a new daily best.</summary>
        public bool SubmitDaily(in RunResult r, string dateKey)
        {
            bool newBest = SetBest(DailyBest, r.RunScore);
            SetBest(DailyToday(dateKey), r.RunScore);
            RecordStats(in r);
            return newBest;
        }

        void RecordStats(in RunResult r)
        {
            if (r.FloorCount > 0) _records[StatTowers] = Get(StatTowers) + 1;
            _records[StatResidents] = Get(StatResidents) + r.TotalResidents;
            _records[StatPerfects] = Get(StatPerfects) + r.PerfectDrops;
        }

        bool SetBest(string key, int value)
        {
            if (value <= Get(key)) return false;
            _records[key] = value;
            return true;
        }
    }
}
