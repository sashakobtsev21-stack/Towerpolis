namespace Towerpolis.Core.Determinism
{
    /// <summary>
    /// SplitMix64 finalizer (Vigna) — turns a small or structured key into a well-distributed
    /// 64-bit value. Shared by <see cref="DailySeed"/> and by per-stream run-seed derivation
    /// (each gameplay RNG stream is salted then mixed so the streams stay independent).
    /// </summary>
    public static class SeedMix
    {
        public static ulong SplitMix64(ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
            x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
            return x ^ (x >> 31);
        }
    }
}
