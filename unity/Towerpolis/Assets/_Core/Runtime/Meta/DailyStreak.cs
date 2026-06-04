using System;
using System.Globalization;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Meta
{
    /// <summary>Daily-streak save state (meta-spec §3.4). Date keys are UTC <c>"yyyy-MM-dd"</c> strings;
    /// <see cref="LastDate"/> is "" when never played.</summary>
    public readonly struct DailyStreakState
    {
        public readonly int Current;
        public readonly int Longest;
        public readonly string LastDate;

        public DailyStreakState(int current, int longest, string? lastDate)
        {
            Current = current;
            Longest = longest;
            LastDate = lastDate ?? "";
        }

        public static DailyStreakState Empty => new DailyStreakState(0, 0, "");
    }

    /// <summary>
    /// Deterministic daily-streak state machine (meta-spec §3.4). No clock is read here — the Unity layer
    /// passes today's UTC date key. Pure, so it is fully NUnit-testable and exploit-safe in Core (ADR-0002).
    /// </summary>
    public static class DailyStreak
    {
        public static bool HasPlayed(in DailyStreakState s, string todayKey)
            => !string.IsNullOrEmpty(todayKey) && s.LastDate == todayKey;

        /// <summary>Record a completed daily run for <paramref name="todayKey"/>. Idempotent within a day:
        /// playing again the same day is a no-op. A consecutive day increments the streak; any gap (or the
        /// first ever play) resets it to 1. <see cref="DailyStreakState.Longest"/> tracks the peak.</summary>
        public static DailyStreakState Record(in DailyStreakState prev, string todayKey)
        {
            if (string.IsNullOrEmpty(todayKey)) throw new ArgumentException("todayKey required", nameof(todayKey));
            if (prev.LastDate == todayKey) return prev; // already counted today

            int current = IsNextDay(prev.LastDate, todayKey) ? prev.Current + 1 : 1;
            int longest = Math.Max(prev.Longest, current);
            return new DailyStreakState(current, longest, todayKey);
        }

        /// <summary>Coins for hitting a streak milestone exactly (3/7/14/30 → 75/200/400/1000), else 0.</summary>
        public static int MilestoneCoins(int streak, CoreConfig cfg)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            int[] days = cfg.StreakMilestoneDays, coins = cfg.StreakMilestoneCoins;
            int n = Math.Min(days.Length, coins.Length);
            for (int i = 0; i < n; i++)
                if (days[i] == streak) return coins[i];
            return 0;
        }

        static bool IsNextDay(string prevKey, string todayKey)
        {
            if (!TryParse(prevKey, out DateTime prev) || !TryParse(todayKey, out DateTime today)) return false;
            return (today.Date - prev.Date).Days == 1;
        }

        static bool TryParse(string key, out DateTime value)
            => DateTime.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
}
