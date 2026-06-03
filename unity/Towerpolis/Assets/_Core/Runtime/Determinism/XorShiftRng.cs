using System;

namespace Towerpolis.Core.Determinism
{
    /// <summary>
    /// Deterministic, cross-platform pseudo-random generator (xorshift64* — Marsaglia/Vigna).
    /// Pure integer math, so it produces an identical sequence on every device/CPU. That is
    /// mandatory for the daily seed (the same crane-sway pattern for every player on a given day)
    /// and underpins the rule that nothing random/scored is ever derived from PhysX.
    /// NOT cryptographically secure — do not use for anything security-sensitive.
    /// </summary>
    public sealed class XorShiftRng
    {
        const ulong DefaultSeed = 0x9E3779B97F4A7C15UL; // golden ratio; used when seed == 0
        const ulong StarMul     = 0x2545F4914F6CDD1DUL;

        ulong _state;

        public XorShiftRng(ulong seed)
        {
            _state = seed == 0UL ? DefaultSeed : seed;
        }

        /// <summary>Raw internal state — capture/restore to resume a run's RNG exactly.</summary>
        public ulong State => _state;

        /// <summary>Next 64-bit value across the full range.</summary>
        public ulong NextULong()
        {
            ulong x = _state;
            x ^= x >> 12;
            x ^= x << 25;
            x ^= x >> 27;
            _state = x;
            return x * StarMul;
        }

        /// <summary>Next 32-bit value (high bits of the 64-bit output are best distributed).</summary>
        public uint NextUInt() => (uint)(NextULong() >> 32);

        /// <summary>Uniform int in [0, maxExclusive). Unbiased via rejection sampling.</summary>
        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "must be > 0");

            uint range = (uint)maxExclusive;
            uint limit = uint.MaxValue - (uint.MaxValue % range); // largest exact multiple of range
            uint v;
            do { v = NextUInt(); } while (v >= limit);
            return (int)(v % range);
        }

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int NextInt(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "max must be > min");

            long span = (long)maxExclusive - minInclusive;
            if (span > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "range too large");

            return minInclusive + NextInt((int)span);
        }

        /// <summary>Uniform double in [0,1) with 53-bit precision (IEEE-754 deterministic).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);

        /// <summary>Uniform float in [0,1).</summary>
        public float NextFloat() => (float)NextDouble();
    }
}
