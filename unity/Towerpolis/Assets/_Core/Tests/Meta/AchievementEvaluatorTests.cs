using System;
using System.Collections.Generic;
using NUnit.Framework;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class AchievementEvaluatorTests
    {
        static readonly AchievementInfo[] Defs =
        {
            new AchievementInfo("ach_towers_5",   AchievementMetric.TotalTowers,        5,    100),
            new AchievementInfo("ach_towers_50",  AchievementMetric.TotalTowers,        50,   200),
            new AchievementInfo("ach_residents_1k", AchievementMetric.TotalResidents,   1000, 150),
            new AchievementInfo("ach_streak_7",   AchievementMetric.LongestStreak,      7,    200),
            new AchievementInfo("ach_height_50",  AchievementMetric.BestFloorCount,     50,   250),
            new AchievementInfo("ach_d3_complete", AchievementMetric.DistrictsCompleted, 3,   300),
        };

        static AchievementSnapshot Stats(int towers = 0, int residents = 0, int perfects = 0,
            int streak = 0, int bestFloor = 0, int districts = 0)
            => new AchievementSnapshot(towers, residents, perfects, streak, bestFloor, districts);

        static readonly string[] None = Array.Empty<string>();

        [Test]
        public void TriggerOnThreshold()
        {
            var newly = AchievementEvaluator.Evaluate(Stats(towers: 5), Defs, None);
            Assert.That(newly, Contains.Item("ach_towers_5"));
            Assert.That(newly, Does.Not.Contain("ach_towers_50"));
        }

        [Test]
        public void DoesNotTriggerBelowThreshold()
        {
            var newly = AchievementEvaluator.Evaluate(Stats(towers: 4), Defs, None);
            Assert.That(newly, Does.Not.Contain("ach_towers_5"));
        }

        [Test]
        public void DoesNotRetriggerAlreadyCompleted()
        {
            var completed = new[] { "ach_towers_5" };
            var newly = AchievementEvaluator.Evaluate(Stats(towers: 5), Defs, completed);
            Assert.That(newly, Does.Not.Contain("ach_towers_5"));
        }

        [Test]
        public void MultipleTriggeredAtOnce()
        {
            var newly = AchievementEvaluator.Evaluate(Stats(towers: 50), Defs, None);
            Assert.That(newly, Contains.Item("ach_towers_5"));
            Assert.That(newly, Contains.Item("ach_towers_50"));
        }

        [Test]
        public void DistinctMetrics_EvaluatedIndependently()
        {
            var newly = AchievementEvaluator.Evaluate(Stats(streak: 7, bestFloor: 50, districts: 3), Defs, None);
            Assert.That(newly, Contains.Item("ach_streak_7"));
            Assert.That(newly, Contains.Item("ach_height_50"));
            Assert.That(newly, Contains.Item("ach_d3_complete"));
            Assert.That(newly, Does.Not.Contain("ach_towers_5")); // towers=0
        }

        [Test]
        public void NullCompleted_TreatedAsNoneUnlocked()
        {
            var newly = AchievementEvaluator.Evaluate(Stats(towers: 5), Defs, null!);
            Assert.That(newly, Contains.Item("ach_towers_5"));
        }

        [Test]
        public void NullDefinitions_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => AchievementEvaluator.Evaluate(Stats(towers: 5), null!, None));
        }
    }
}
