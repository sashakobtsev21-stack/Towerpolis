using System;
using System.Collections.Generic;
using Towerpolis.Core.Determinism;

namespace Towerpolis.Core.Meta
{
    /// <summary>What a mission measures (progression-spec §4.1). Cumulative metrics sum across the week's
    /// runs; peak metrics keep the best single-run value.</summary>
    public enum MissionMetric
    {
        FloorsPlaced,           // cumulative
        PerfectDrops,           // cumulative
        DailyRunsCompleted,     // cumulative (one daily run per UTC day)
        TowerHeight,            // peak (best run height)
        PerfectChainLength,     // peak (best chain in a run)
        ResidentsHoused,        // cumulative
        DistrictRunsCompleted,  // cumulative, filtered by district
        StreakDays,             // peak (highest streak reached)
    }

    /// <summary>Engine-free, flattened form of a <c>MissionDefinition</c> ScriptableObject — the Unity layer
    /// passes these into Core so the tracker stays Unity-free (progression-spec §4.1).</summary>
    public readonly struct MissionInfo
    {
        public readonly string MissionId;
        public readonly MissionMetric Metric;
        public readonly int Target;
        public readonly int RewardCoins;
        public readonly string FilterDistrictId; // "" = any district

        public MissionInfo(string missionId, MissionMetric metric, int target, int rewardCoins, string filterDistrictId = "")
        {
            MissionId = missionId;
            Metric = metric;
            Target = target;
            RewardCoins = rewardCoins;
            FilterDistrictId = filterDistrictId;
        }
    }

    /// <summary>One run's contribution to mission progress (progression-spec §4.2), built in Unity from
    /// <c>RunResult</c> + context. Plain value type, no engine dependency.</summary>
    public readonly struct MissionEvent
    {
        public readonly int FloorsPlaced;
        public readonly int PerfectDrops;
        public readonly int ResidentsHoused;
        public readonly int MaxPerfectChain;
        public readonly bool IsDailyRun;
        public readonly string DistrictId;
        public readonly int NewStreakValue; // streak after this run (0 if not a daily run)

        public MissionEvent(int floorsPlaced, int perfectDrops, int residentsHoused, int maxPerfectChain,
            bool isDailyRun, string districtId, int newStreakValue)
        {
            FloorsPlaced = floorsPlaced;
            PerfectDrops = perfectDrops;
            ResidentsHoused = residentsHoused;
            MaxPerfectChain = maxPerfectChain;
            IsDailyRun = isDailyRun;
            DistrictId = districtId;
            NewStreakValue = newStreakValue;
        }
    }

    /// <summary>
    /// Deterministic weekly-mission progress + the seeded weekly draw (progression-spec §4.1/§4.2). All pure:
    /// <see cref="Record"/> returns a NEW progress map (never mutates the input), and
    /// <see cref="DrawWeeklyMissions"/> picks the same 3 missions worldwide for a given week seed, so the
    /// rotation is shared and cheat-proof. Unity-free and NUnit-tested.
    /// </summary>
    public static class MissionTracker
    {
        /// <summary>Fold one run's <paramref name="evt"/> into the progress map for every active mission.</summary>
        public static Dictionary<string, int> Record(
            in MissionEvent evt,
            IEnumerable<MissionInfo> activeMissions,
            IReadOnlyDictionary<string, int> currentProgress)
        {
            if (activeMissions == null) throw new ArgumentNullException(nameof(activeMissions));

            var result = new Dictionary<string, int>();
            if (currentProgress != null)
                foreach (KeyValuePair<string, int> kv in currentProgress) result[kv.Key] = kv.Value;

            foreach (MissionInfo m in activeMissions)
            {
                if (string.IsNullOrEmpty(m.MissionId)) continue;
                result.TryGetValue(m.MissionId, out int cur);
                result[m.MissionId] = ApplyMetric(in m, in evt, cur);
            }
            return result;
        }

        /// <summary>True once progress for <paramref name="missionId"/> reaches <paramref name="target"/>.</summary>
        public static bool IsComplete(string missionId, IReadOnlyDictionary<string, int> progress, int target)
        {
            if (progress == null || string.IsNullOrEmpty(missionId)) return false;
            return progress.TryGetValue(missionId, out int p) && p >= target;
        }

        /// <summary>Pick <paramref name="count"/> distinct mission ids from the pool, deterministic per week
        /// seed (seeded Fisher-Yates). Same seed → same list, everywhere.</summary>
        public static List<string> DrawWeeklyMissions(IEnumerable<string> allMissionIds, ulong weekSeed, int count = 3)
        {
            if (allMissionIds == null) throw new ArgumentNullException(nameof(allMissionIds));

            var pool = new List<string>(allMissionIds);
            int n = pool.Count;
            int take = Math.Min(count, n);
            var rng = new XorShiftRng(weekSeed);
            for (int i = 0; i < take; i++)
            {
                int j = i + rng.NextInt(n - i); // swap a pick from [i, n) into slot i
                (pool[i], pool[j]) = (pool[j], pool[i]);
            }
            return pool.GetRange(0, take);
        }

        static int ApplyMetric(in MissionInfo m, in MissionEvent e, int cur)
        {
            switch (m.Metric)
            {
                case MissionMetric.FloorsPlaced:       return cur + e.FloorsPlaced;
                case MissionMetric.PerfectDrops:       return cur + e.PerfectDrops;
                case MissionMetric.ResidentsHoused:    return cur + e.ResidentsHoused;
                case MissionMetric.DailyRunsCompleted: return cur + (e.IsDailyRun ? 1 : 0);
                case MissionMetric.DistrictRunsCompleted:
                    bool match = string.IsNullOrEmpty(m.FilterDistrictId) || m.FilterDistrictId == e.DistrictId;
                    return cur + (match ? 1 : 0);
                case MissionMetric.TowerHeight:        return Math.Max(cur, e.FloorsPlaced);
                case MissionMetric.PerfectChainLength: return Math.Max(cur, e.MaxPerfectChain);
                case MissionMetric.StreakDays:         return Math.Max(cur, e.NewStreakValue);
                default:                               return cur;
            }
        }
    }
}
