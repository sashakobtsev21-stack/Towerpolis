using NUnit.Framework;
using Towerpolis.Core.Determinism;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Tests.Gameplay
{
    /// <summary>Per-run seed derivation: each gameplay RNG stream must be reproducible from the run seed and
    /// independent of the others (distinct salts) — the backbone of cross-device daily-seed fairness.</summary>
    public class RunSeedsTests
    {
        static int[] Take(XorShiftRng rng, int n)
        {
            var a = new int[n];
            for (int i = 0; i < n; i++) a[i] = rng.NextInt(1_000_000);
            return a;
        }

        [Test]
        public void BlockRng_SameSeed_IdenticalStream()
        {
            Assert.That(Take(RunSeeds.BlockRng(42UL), 64), Is.EqualTo(Take(RunSeeds.BlockRng(42UL), 64)));
        }

        [Test]
        public void BlockAndSwing_SameSeed_IndependentStreams() // distinct salts → advancing one can't perturb the other
        {
            Assert.That(Take(RunSeeds.BlockRng(42UL), 64), Is.Not.EqualTo(Take(RunSeeds.SwingRng(42UL), 64)));
        }

        [Test]
        public void DifferentSeeds_DifferentStreams()
        {
            Assert.That(Take(RunSeeds.BlockRng(1UL), 64), Is.Not.EqualTo(Take(RunSeeds.BlockRng(2UL), 64)));
        }
    }
}
