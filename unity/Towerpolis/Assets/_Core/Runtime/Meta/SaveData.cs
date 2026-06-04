using System;
using System.Collections.Generic;

namespace Towerpolis.Core.Meta
{
    /// <summary>A leaderboard record as a serialisable pair (Unity JsonUtility can't serialise a Dictionary).</summary>
    [Serializable]
    public sealed class IntEntry
    {
        public string Key = "";
        public int Value;

        public IntEntry() { }
        public IntEntry(string key, int value) { Key = key; Value = value; }
    }

    /// <summary>One deposited tower in the save (ADR-0007).</summary>
    [Serializable]
    public sealed class PlotSave
    {
        public int FloorCount;
        public int Residents;
        public long TimestampUtcTicks;
    }

    /// <summary>One district's saved plots (ADR-0007).</summary>
    [Serializable]
    public sealed class DistrictSave
    {
        public string Id = "";
        public int Capacity;
        public List<PlotSave> Plots = new();
    }

    /// <summary>
    /// Plain, serialisable snapshot of <see cref="CityState"/> (ADR-0007). Public fields, initialised
    /// collections, and only JsonUtility-friendly types (no Dictionary) so the Unity <c>SaveManager</c>
    /// round-trips it with the built-in serialiser — Core takes no JSON dependency. <see cref="SchemaVersion"/>
    /// drives forward-only migrations (<see cref="SaveMigration"/>).
    /// </summary>
    [Serializable]
    public sealed class SaveData
    {
        public const int CurrentVersion = 1;

        public int SchemaVersion = CurrentVersion;
        public int Coins;
        public int Gems;
        public string ActiveDistrictId = "downtown";
        public int StreakCurrent;
        public int StreakLongest;
        public string StreakLastDate = "";
        public List<DistrictSave> Districts = new();
        public List<IntEntry> Leaderboard = new();
        public List<string> RewardedDistricts = new();

        /// <summary>Snapshot the current meta-state for saving. Inverse of <see cref="CityState.FromSave"/>.</summary>
        public static SaveData From(CityState s)
        {
            var save = new SaveData
            {
                SchemaVersion = CurrentVersion,
                Coins = s.Coins,
                Gems = s.Gems,
                ActiveDistrictId = s.ActiveDistrictId,
                StreakCurrent = s.Streak.Current,
                StreakLongest = s.Streak.Longest,
                StreakLastDate = s.Streak.LastDate,
                RewardedDistricts = new List<string>(s.RewardedDistricts),
                Districts = new List<DistrictSave>(),
                Leaderboard = new List<IntEntry>(),
            };

            foreach (KeyValuePair<string, int> rec in s.Leaderboard.Records)
                save.Leaderboard.Add(new IntEntry(rec.Key, rec.Value));

            foreach (KeyValuePair<string, CityGrid> kv in s.Grids)
            {
                var ds = new DistrictSave { Id = kv.Key, Capacity = kv.Value.Capacity };
                foreach (Plot plot in kv.Value.Plots)
                {
                    if (!plot.Occupied) continue;
                    ds.Plots.Add(new PlotSave
                    {
                        FloorCount = plot.FloorCount,
                        Residents = plot.Residents,
                        TimestampUtcTicks = plot.TimestampUtcTicks,
                    });
                }
                save.Districts.Add(ds);
            }
            return save;
        }

        /// <summary>The leaderboard pairs as a dictionary (for <see cref="LocalLeaderboard"/>).</summary>
        public Dictionary<string, int> LeaderboardMap()
        {
            var map = new Dictionary<string, int>();
            foreach (IntEntry e in Leaderboard)
                if (!string.IsNullOrEmpty(e.Key)) map[e.Key] = e.Value;
            return map;
        }
    }
}
