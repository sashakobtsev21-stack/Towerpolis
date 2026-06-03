using System;

namespace Towerpolis.Core.Determinism
{
    /// <summary>
    /// Maps a calendar day to a stable 64-bit seed so every player worldwide gets the SAME
    /// crane-sway sequence on a given date — the game's "daily seed" heartbeat (§4.2 of the GDD).
    /// Pure by design: the date is always an input, never read from the system clock here, so the
    /// Core stays deterministic and unit-testable. The Unity layer passes <c>DateTime.UtcNow</c>.
    /// </summary>
    public static class DailySeed
    {
        /// <summary>Stable seed for a given UTC year/month/day.</summary>
        public static ulong ForDate(int year, int month, int day)
        {
            // Pack as YYYYMMDD, then run SplitMix64 so adjacent days yield well-separated seeds
            // (a raw incrementing key would give visibly similar sequences day to day).
            ulong key = (ulong)(year * 10000 + month * 100 + day);
            return SeedMix.SplitMix64(key ^ 0xA24BAED4963EE407UL);
        }

        /// <summary>Stable seed for the date component (Y/M/D) of a UTC DateTime.</summary>
        public static ulong ForDateUtc(DateTime utc) => ForDate(utc.Year, utc.Month, utc.Day);

        /// <summary>A ready-to-use RNG seeded for the given UTC date.</summary>
        public static XorShiftRng RngForDate(int year, int month, int day)
            => new XorShiftRng(ForDate(year, month, day));

        /// <summary>A ready-to-use RNG seeded for the date component of a UTC DateTime.</summary>
        public static XorShiftRng RngForDateUtc(DateTime utc)
            => new XorShiftRng(ForDateUtc(utc));
    }
}
