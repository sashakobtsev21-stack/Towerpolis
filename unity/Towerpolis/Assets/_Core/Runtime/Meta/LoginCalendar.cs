#nullable enable
using System;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Meta
{
    /// <summary>Login-calendar save state (progression-spec §3.2). <see cref="Day"/> is the last CLAIMED slot
    /// (0 = never started, 1–30 = position in the cycle); <see cref="LastClaim"/> is the UTC date key of that
    /// claim ("" when never claimed).</summary>
    public readonly struct LoginCalendarState
    {
        public readonly int Day;
        public readonly string LastClaim;

        public LoginCalendarState(int day, string? lastClaim)
        {
            Day = day;
            LastClaim = lastClaim ?? "";
        }

        public static LoginCalendarState Empty => new LoginCalendarState(0, "");
    }

    /// <summary>The reward a single calendar claim yields (progression-spec §3.2).</summary>
    public readonly struct LoginCalendarReward
    {
        public readonly int Coins;
        public readonly int FreezeCharges;
        public readonly int DayNumber; // 1–30, for display

        public LoginCalendarReward(int coins, int freezeCharges, int dayNumber)
        {
            Coins = coins;
            FreezeCharges = freezeCharges;
            DayNumber = dayNumber;
        }
    }

    /// <summary>
    /// A 30-day revolving login calendar (progression-spec §3.2). Each UTC day the player opens the app they
    /// may claim ONE slot, advancing the cycle by one and earning that day's reward; the cycle wraps from
    /// day 30 back to day 1. No clock is read here — the Unity layer passes today's UTC date key — so it is
    /// pure and NUnit-testable, and "claim once per day" is exploit-safe (no auto-claim on clock changes).
    /// </summary>
    public static class LoginCalendar
    {
        /// <summary>True if today's slot has not been claimed yet.</summary>
        public static bool CanClaim(in LoginCalendarState s, string todayKey)
            => !string.IsNullOrEmpty(todayKey) && s.LastClaim != todayKey;

        /// <summary>Claim today's slot: advance the cycle by one and return the reward. Pure — returns the new
        /// state + reward; the caller banks the coins/freeze and persists the state. A second claim on the same
        /// UTC day is a no-op (returns the state unchanged with a zero reward).</summary>
        public static (LoginCalendarState next, LoginCalendarReward reward) Claim(
            in LoginCalendarState s, string todayKey, CoreConfig cfg)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            if (string.IsNullOrEmpty(todayKey)) throw new ArgumentException("todayKey required", nameof(todayKey));
            if (s.LastClaim == todayKey) return (s, new LoginCalendarReward(0, 0, s.Day)); // already claimed today

            int cycle = cfg.LoginCalendarCoins.Length;          // 30
            int nextDay = s.Day >= cycle ? 1 : s.Day + 1;       // 0→1 (first claim), 30→1 (wrap)
            int idx = nextDay - 1;

            int coins = idx >= 0 && idx < cfg.LoginCalendarCoins.Length ? cfg.LoginCalendarCoins[idx] : 0;
            int freezes = idx >= 0 && idx < cfg.LoginCalendarFreezes.Length ? cfg.LoginCalendarFreezes[idx] : 0;

            return (new LoginCalendarState(nextDay, todayKey), new LoginCalendarReward(coins, freezes, nextDay));
        }
    }
}
