using System;
using System.Collections.Generic;

namespace Towerpolis.Core.Meta
{
    /// <summary>What an achievement measures (progression-spec §4.3). Each maps onto a lifetime stat.</summary>
    public enum AchievementMetric
    {
        TotalTowers,
        TotalResidents,
        TotalPerfects,
        LongestStreak,
        BestFloorCount,
        DistrictsCompleted,
    }

    /// <summary>Engine-free, flattened form of an <c>AchievementDefinition</c> ScriptableObject
    /// (progression-spec §4.3) — passed from Unity so the evaluator stays Unity-free.</summary>
    public readonly struct AchievementInfo
    {
        public readonly string AchievementId;
        public readonly AchievementMetric Metric;
        public readonly int Threshold;
        public readonly int RewardCoins;

        public AchievementInfo(string achievementId, AchievementMetric metric, int threshold, int rewardCoins)
        {
            AchievementId = achievementId;
            Metric = metric;
            Threshold = threshold;
            RewardCoins = rewardCoins;
        }
    }

    /// <summary>A snapshot of the player's lifetime stats, used to test achievement thresholds
    /// (progression-spec §4.3).</summary>
    public readonly struct AchievementSnapshot
    {
        public readonly int TotalTowers;
        public readonly int TotalResidents;
        public readonly int TotalPerfects;
        public readonly int LongestStreak;
        public readonly int BestFloorCount;     // max across all districts
        public readonly int DistrictsCompleted; // count of rewarded districts

        public AchievementSnapshot(int totalTowers, int totalResidents, int totalPerfects,
            int longestStreak, int bestFloorCount, int districtsCompleted)
        {
            TotalTowers = totalTowers;
            TotalResidents = totalResidents;
            TotalPerfects = totalPerfects;
            LongestStreak = longestStreak;
            BestFloorCount = bestFloorCount;
            DistrictsCompleted = districtsCompleted;
        }
    }

    /// <summary>
    /// Pure achievement evaluation (progression-spec §4.3): given the current lifetime stats and the set of
    /// already-unlocked ids, return the ids newly crossing their threshold. Permanent (never re-triggers,
    /// never resets); no mutation. Unity-free and NUnit-tested.
    /// </summary>
    public static class AchievementEvaluator
    {
        public static IReadOnlyList<string> Evaluate(
            in AchievementSnapshot stats,
            IEnumerable<AchievementInfo> definitions,
            IReadOnlyCollection<string> completedIds)
        {
            if (definitions == null) throw new ArgumentNullException(nameof(definitions));

            var done = completedIds != null ? new HashSet<string>(completedIds) : new HashSet<string>();
            var newly = new List<string>();

            foreach (AchievementInfo d in definitions)
            {
                if (string.IsNullOrEmpty(d.AchievementId)) continue;
                if (done.Contains(d.AchievementId)) continue; // already unlocked
                if (Value(in stats, d.Metric) >= d.Threshold) newly.Add(d.AchievementId);
            }
            return newly;
        }

        static int Value(in AchievementSnapshot s, AchievementMetric m)
        {
            switch (m)
            {
                case AchievementMetric.TotalTowers:        return s.TotalTowers;
                case AchievementMetric.TotalResidents:     return s.TotalResidents;
                case AchievementMetric.TotalPerfects:      return s.TotalPerfects;
                case AchievementMetric.LongestStreak:      return s.LongestStreak;
                case AchievementMetric.BestFloorCount:     return s.BestFloorCount;
                case AchievementMetric.DistrictsCompleted: return s.DistrictsCompleted;
                default:                                   return 0;
            }
        }
    }
}
