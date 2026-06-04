using System;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class DailyStreakTests
    {
        static readonly CoreConfig Cfg = new CoreConfig();

        [Test]
        public void FirstPlay_StartsStreakAtOne()
        {
            var s = DailyStreak.Record(DailyStreakState.Empty, "2026-06-04");
            Assert.That(s.Current, Is.EqualTo(1));
            Assert.That(s.Longest, Is.EqualTo(1));
            Assert.That(s.LastDate, Is.EqualTo("2026-06-04"));
        }

        [Test]
        public void SameDay_IsIdempotent()
        {
            var s1 = DailyStreak.Record(DailyStreakState.Empty, "2026-06-04");
            var s2 = DailyStreak.Record(s1, "2026-06-04");
            Assert.That(s2.Current, Is.EqualTo(1));
            Assert.That(s2.Longest, Is.EqualTo(1));
        }

        [Test]
        public void ConsecutiveDays_Increment()
        {
            var s = DailyStreakState.Empty;
            s = DailyStreak.Record(s, "2026-06-04");
            s = DailyStreak.Record(s, "2026-06-05");
            s = DailyStreak.Record(s, "2026-06-06");
            Assert.That(s.Current, Is.EqualTo(3));
            Assert.That(s.Longest, Is.EqualTo(3));
        }

        [Test]
        public void MissedDay_ResetsToOne_ButLongestPersists()
        {
            var s = DailyStreakState.Empty;
            s = DailyStreak.Record(s, "2026-06-04");
            s = DailyStreak.Record(s, "2026-06-05"); // current 2
            s = DailyStreak.Record(s, "2026-06-08"); // gap → reset to 1
            Assert.That(s.Current, Is.EqualTo(1));
            Assert.That(s.Longest, Is.EqualTo(2));
        }

        [Test]
        public void CrossesMonthBoundary()
        {
            var s = DailyStreak.Record(DailyStreakState.Empty, "2026-06-30");
            s = DailyStreak.Record(s, "2026-07-01");
            Assert.That(s.Current, Is.EqualTo(2));
        }

        [Test]
        public void HasPlayed_MatchesLastDate()
        {
            var s = DailyStreak.Record(DailyStreakState.Empty, "2026-06-04");
            Assert.That(DailyStreak.HasPlayed(s, "2026-06-04"), Is.True);
            Assert.That(DailyStreak.HasPlayed(s, "2026-06-05"), Is.False);
            Assert.That(DailyStreak.HasPlayed(DailyStreakState.Empty, "2026-06-04"), Is.False);
        }

        [Test]
        public void EmptyKey_Throws()
        {
            Assert.Throws<ArgumentException>(() => DailyStreak.Record(DailyStreakState.Empty, ""));
        }

        [TestCase(3, 75)]
        [TestCase(7, 200)]
        [TestCase(14, 400)]
        [TestCase(30, 1000)]
        public void MilestoneCoins_AtThresholds(int streak, int expected)
        {
            Assert.That(DailyStreak.MilestoneCoins(streak, Cfg), Is.EqualTo(expected));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(8)]
        [TestCase(31)]
        public void MilestoneCoins_OffThresholds_AreZero(int streak)
        {
            Assert.That(DailyStreak.MilestoneCoins(streak, Cfg), Is.Zero);
        }
    }
}
