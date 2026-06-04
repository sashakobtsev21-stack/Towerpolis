using System.Collections.Generic;

namespace Towerpolis.Core.Meta
{
    /// <summary>One deposited tower in the save (ADR-0007).</summary>
    public sealed class PlotSave
    {
        public int FloorCount;
        public int Residents;
        public long TimestampUtcTicks;
    }

    /// <summary>One district's saved plots (ADR-0007).</summary>
    public sealed class DistrictSave
    {
        public string Id = "";
        public int Capacity;
        public List<PlotSave> Plots = new();
    }

    /// <summary>
    /// Plain, serialisable snapshot of <see cref="CityState"/> (ADR-0007). Public fields + initialised
    /// collections so any JSON serialiser (Unity-side: Newtonsoft / System.Text.Json) round-trips it; Core
    /// itself takes no JSON dependency. <see cref="SchemaVersion"/> drives forward-only migrations
    /// (<see cref="SaveMigration"/>). The Unity <c>SaveManager</c> owns the actual file I/O.
    /// </summary>
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
        public Dictionary<string, int> Leaderboard = new();
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
                Leaderboard = new Dictionary<string, int>(s.Leaderboard.Records),
                RewardedDistricts = new List<string>(s.RewardedDistricts),
                Districts = new List<DistrictSave>(),
            };

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
    }
}
