using System;
using NUnit.Framework;
using Towerpolis.Core.Determinism;

namespace Towerpolis.Core.Tests.Determinism
{
    /// <summary>Edge branches of the deterministic primitives — RNG error paths, NextFloat, and the
    /// UTC-DateTime seed — so the determinism core (which underpins daily-seed fairness) is fully covered.</summary>
    public class DeterminismEdgeTests
    {
        [Test]
        public void NextIntRange_ThrowsWhenMaxNotAboveMin()
        {
            var r = new XorShiftRng(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(5, 5));
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(5, 3));
        }

        [Test]
        public void NextIntRange_ThrowsWhenSpanExceedsInt()
        {
            var r = new XorShiftRng(1);
            Assert.Throws<ArgumentOutOfRangeException>(() => r.NextInt(int.MinValue, int.MaxValue));
        }

        [Test]
        public void NextIntRange_StaysInBounds()
        {
            var r = new XorShiftRng(7);
            for (int i = 0; i < 1000; i++)
                Assert.That(r.NextInt(-5, 5), Is.InRange(-5, 4)); // [-5, 5) for ints
        }

        [Test]
        public void NextFloat_InZeroToOne()
        {
            var r = new XorShiftRng(7);
            for (int i = 0; i < 1000; i++)
                Assert.That(r.NextFloat(), Is.GreaterThanOrEqualTo(0f).And.LessThan(1f));
        }

        [Test]
        public void RngForDateUtc_MatchesRngForDate_SameDate()
        {
            var a = DailySeed.RngForDateUtc(new DateTime(2026, 6, 3, 12, 30, 0, DateTimeKind.Utc));
            var b = DailySeed.RngForDate(2026, 6, 3);
            for (int i = 0; i < 50; i++) Assert.That(a.NextInt(1000), Is.EqualTo(b.NextInt(1000)));
        }
    }
}
