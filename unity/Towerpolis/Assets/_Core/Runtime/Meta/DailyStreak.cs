#nullable enable
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
        public readonly int FreezeCharges; // 0–StreakFreezeMaxCharges; a freeze bridges one missed day

        public DailyStreakState(int current, int longest, string? lastDate, int freezeCharges = 0)
        {
            Current = current;
            Longest = longest;
            LastDate = lastDate ?? "";
            FreezeCharges = freezeCharges < 0 ? 0 : freezeCharges;
        }

        public static DailyStreakState Empty => new DailyStreakState(0, 0, "", 0);

        /// <summary>Copy with a new freeze-charge count (e.g. after buying a charge).</summary>
        public DailyStreakState WithFreezeCharges(int charges)
            => new DailyStreakState(Current, Longest, LastDate, charges);
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
        /// playing again the same day is a no-op (returns prev, freezeConsumed=false). A consecutive day
        /// (gap 1) increments the streak. A SINGLE missed day (gap 2) is bridged if
        /// <paramref name="freezeCharges"/> &gt; 0: one charge is consumed and the streak increments as if
        /// consecutive (freezeConsumed=true). A larger gap, the first ever play, or no charge resets to 1.
        /// One charge bridges at most one missed day, so a 2-day gap can never be bridged.
        /// <see cref="DailyStreakState.Longest"/> tracks the peak; the returned state carries the remaining
        /// charges. Pure — the caller decides whether to apply it.</summary>
        public static (DailyStreakState next, bool freezeConsumed) Record(
            in DailyStreakState prev, string todayKey, int freezeCharges = 0)
        {
            if (string.IsNullOrEmpty(todayKey)) throw new ArgumentException("todayKey required", nameof(todayKey));
            if (prev.LastDate == todayKey) return (prev, false); // already counted today

            if (freezeCharges < 0) freezeCharges = 0;
            int gap = DayGap(prev.LastDate, todayKey);

            int current;
            bool freezeConsumed = false;
            int chargesLeft = freezeCharges;

            if (gap == 1)
            {
                current = prev.Current + 1; // consecutive day
            }
            else if (gap == 2 && freezeCharges > 0)
            {
                current = prev.Current + 1; // bridge the single missed day
                chargesLeft = freezeCharges - 1;
                freezeConsumed = true;
            }
            else
            {
                current = 1; // first play, gap too large, or no charge → reset
            }

            int longest = Math.Max(prev.Longest, current);
            return (new DailyStreakState(current, longest, todayKey, chargesLeft), freezeConsumed);
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

        // Whole-day difference (today − prev); -1 if either key is missing/unparseable (treated as a reset).
        static int DayGap(string prevKey, string todayKey)
        {
            if (!TryParse(prevKey, out DateTime prev) || !TryParse(todayKey, out DateTime today)) return -1;
            return (today.Date - prev.Date).Days;
        }

        static bool TryParse(string key, out DateTime value)
            => DateTime.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out value);
    }
}
