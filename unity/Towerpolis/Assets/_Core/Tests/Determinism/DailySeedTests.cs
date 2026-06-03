using System;
using NUnit.Framework;
using Towerpolis.Core.Determinism;

namespace Towerpolis.Core.Tests.Determinism
{
    public class DailySeedTests
    {
        [Test]
        public void SameDate_SameSeed()
        {
            Assert.That(DailySeed.ForDate(2026, 1, 1), Is.EqualTo(DailySeed.ForDate(2026, 1, 1)));
        }

        [Test]
        public void AdjacentDates_DifferentSeeds()
        {
            Assert.That(DailySeed.ForDate(2026, 1, 1), Is.Not.EqualTo(DailySeed.ForDate(2026, 1, 2)));
            Assert.That(DailySeed.ForDate(2026, 1, 31), Is.Not.EqualTo(DailySeed.ForDate(2026, 2, 1)));
            Assert.That(DailySeed.ForDate(2025, 12, 31), Is.Not.EqualTo(DailySeed.ForDate(2026, 1, 1)));
        }

        [Test]
        public void ForDateUtc_UsesOnlyTheDateComponent()
        {
            var morning = new DateTime(2026, 6, 3, 6, 0, 0, DateTimeKind.Utc);
            var night = new DateTime(2026, 6, 3, 23, 59, 0, DateTimeKind.Utc);
            Assert.That(DailySeed.ForDateUtc(morning), Is.EqualTo(DailySeed.ForDateUtc(night)));
        }

        [Test]
        public void RngForDate_IsBitForBitIdenticalAcrossDevices()
        {
            // Two independent "devices" computing today's run must agree exactly — the whole
            // point of the daily seed.
            var deviceA = DailySeed.RngForDate(2026, 6, 3);
            var deviceB = DailySeed.RngForDate(2026, 6, 3);
            for (int i = 0; i < 100; i++)
                Assert.That(deviceA.NextInt(1000), Is.EqualTo(deviceB.NextInt(1000)), "diverged at {0}", i);
        }

        [Test]
        public void Golden_LocksTheDailySeed()
        {
            // Regression lock: this value must never change, or historical daily seeds would shift.
            Assert.That(DailySeed.ForDate(2026, 1, 1), Is.EqualTo(GoldenNewYear2026));
        }

        const ulong GoldenNewYear2026 = 1097110885808060133UL; // ForDate(2026,1,1)
    }
}
