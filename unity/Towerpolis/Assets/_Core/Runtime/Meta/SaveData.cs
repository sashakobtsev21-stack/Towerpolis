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
        public const int CurrentVersion = 2;

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

        // --- Phase 4 progression (schema v2; progression-spec §7). Defaults below ARE the v1→v2 values;
        //     a v1 JSON simply leaves them at these initialisers. Not yet round-tripped through CityState
        //     (that's the upcoming Unity integration) — added here so the schema + migration are in place. ---
        public int MagnetLevel;     // 0–4
        public int SlowMoLevel;     // 0–4
        public int CityBonusLevel;  // 0–3

        public List<string> OwnedBlockSkins = new() { "skin_default" };
        public string EquippedBlockSkin = "skin_default";
        public List<string> OwnedCraneSkins = new() { "crane_default" };
        public string EquippedCraneSkin = "crane_default";

        public int StreakFreezeCharges; // 0–StreakFreezeMaxCharges

        public int LoginCalendarDay;            // 0 = not started; 1–30 last claimed slot
        public string LoginCalendarLastClaim = "";

        public string ActiveWeekKey = "";       // ISO week of the active mission set
        public List<string> ActiveMissionIds = new();
        public List<IntEntry> MissionProgress = new();
        public List<string> CompletedMissionIds = new();

        public List<string> CompletedAchievementIds = new();

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
