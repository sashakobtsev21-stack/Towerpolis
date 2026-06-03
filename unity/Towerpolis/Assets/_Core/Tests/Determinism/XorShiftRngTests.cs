using NUnit.Framework;
using Towerpolis.Core.Determinism;

namespace Towerpolis.Core.Tests.Determinism
{
    public class XorShiftRngTests
    {
        [Test]
        public void SameSeed_ProducesIdenticalSequence()
        {
            var a = new XorShiftRng(12345);
            var b = new XorShiftRng(12345);
            for (int i = 0; i < 1000; i++)
                Assert.That(a.NextULong(), Is.EqualTo(b.NextULong()), "diverged at {0}", i);
        }

        [Test]
        public void DifferentSeeds_Diverge()
        {
            var a = new XorShiftRng(1);
            var b = new XorShiftRng(2);
            Assert.That(a.NextULong(), Is.Not.EqualTo(b.NextULong()));
        }

        [Test]
        public void ZeroSeed_IsHandled_AndDeterministic()
        {
            var a = new XorShiftRng(0);
            var b = new XorShiftRng(0);
            Assert.That(a.NextULong(), Is.EqualTo(b.NextULong()));
            Assert.That(a.NextULong(), Is.Not.EqualTo(0UL));
        }

        [Test]
        public void NextInt_StaysInRange()
        {
            var r = new XorShiftRng(99);
            for (int i = 0; i < 10000; i++)
                Assert.That(r.NextInt(7), Is.InRange(0, 6));
        }

        [Test]
        public void NextIntMinMax_StaysInRange()
        {
            var r = new XorShiftRng(99);
            for (int i = 0; i < 10000; i++)
                Assert.That(r.NextInt(-3, 4), Is.InRange(-3, 3)); // [-3,4)
        }

        [Test]
        public void NextDouble_IsUnitInterval()
        {
            var r = new XorShiftRng(7);
            for (int i = 0; i < 10000; i++)
            {
                double d = r.NextDouble();
                Assert.That(d, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }

        [Test]
        public void State_RoundTrips_ContinuesSequence()
        {
            var r = new XorShiftRng(42);
            r.NextULong();
            r.NextULong();
            var restored = new XorShiftRng(r.State);
            // restored resumes exactly where r left off
            Assert.That(restored.NextULong(), Is.EqualTo(r.NextULong()));
        }

        [Test]
        public void NextInt_RejectsNonPositiveBound()
        {
            var r = new XorShiftRng(1);
            Assert.That(() => r.NextInt(0), Throws.TypeOf<System.ArgumentOutOfRangeException>());
        }

        [Test]
        public void Golden_LocksTheAlgorithm()
        {
            // Regression lock: changing the PRNG would silently shift every daily seed worldwide.
            var r = new XorShiftRng(0xDEADBEEF);
            Assert.That(r.NextULong(), Is.EqualTo(GoldenFirst));
        }

        const ulong GoldenFirst = 5049962699329485530UL; // xorshift64* of seed 0xDEADBEEF
    }
}
