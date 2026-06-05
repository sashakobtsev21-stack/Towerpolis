using System.Collections.Generic;
using NUnit.Framework;
using Towerpolis.Core.Determinism;

namespace Towerpolis.Core.Tests.Determinism
{
    /// <summary>SplitMix64 underpins the daily seed and per-stream run-seed derivation, so it must be
    /// deterministic, collision-free over a large window, and well-mixed.</summary>
    public class SeedMixTests
    {
        [Test]
        public void SameInput_SameOutput()
        {
            Assert.That(SeedMix.SplitMix64(12345UL), Is.EqualTo(SeedMix.SplitMix64(12345UL)));
            Assert.That(SeedMix.SplitMix64(0UL), Is.EqualTo(SeedMix.SplitMix64(0UL)));
        }

        [Test]
        public void DistinctInputs_DistinctOutputs() // the finalizer is a bijection — no collisions
        {
            var seen = new HashSet<ulong>();
            for (ulong i = 0; i < 5000; i++)
                Assert.That(seen.Add(SeedMix.SplitMix64(i)), Is.True, "collision at {0}", i);
        }

        [Test]
        public void OneBitInputChange_AvalanchesManyOutputBits()
        {
            int diff = PopCount(SeedMix.SplitMix64(0UL) ^ SeedMix.SplitMix64(1UL));
            Assert.That(diff, Is.GreaterThan(16), "weak avalanche: only {0} bits differ", diff);
        }

        [Test]
        public void ZeroInput_NonZeroOutput()
        {
            Assert.That(SeedMix.SplitMix64(0UL), Is.Not.EqualTo(0UL));
        }

        static int PopCount(ulong x)
        {
            int n = 0;
            while (x != 0) { n += (int)(x & 1UL); x >>= 1; }
            return n;
        }
    }
}
