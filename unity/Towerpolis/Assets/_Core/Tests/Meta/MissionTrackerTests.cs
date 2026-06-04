using System.Collections.Generic;
using NUnit.Framework;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class MissionTrackerTests
    {
        static readonly Dictionary<string, int> Empty = new Dictionary<string, int>();

        static MissionInfo Floors() => new MissionInfo("m_floors_weekly", MissionMetric.FloorsPlaced, 200, 100);
        static MissionInfo Perfects() => new MissionInfo("m_perfects_weekly", MissionMetric.PerfectDrops, 50, 120);
        static MissionInfo NeonRuns() => new MissionInfo("m_district_runs", MissionMetric.DistrictRunsCompleted, 6, 80, "neon");

        static MissionEvent Run(int floors = 0, int perfects = 0, int residents = 0, int chain = 0,
            bool daily = false, string district = "downtown", int streak = 0)
            => new MissionEvent(floors, perfects, residents, chain, daily, district, streak);

        [Test]
        public void Record_IncrementsFloorsPlaced()
        {
            var p = MissionTracker.Record(Run(floors: 25), new[] { Floors() }, Empty);
            Assert.That(p["m_floors_weekly"], Is.EqualTo(25));
        }

        [Test]
        public void Record_Cumulative_AddsAcrossRuns()
        {
            var p = MissionTracker.Record(Run(floors: 25), new[] { Floors() }, Empty);
            p = MissionTracker.Record(Run(floors: 30), new[] { Floors() }, p);
            Assert.That(p["m_floors_weekly"], Is.EqualTo(55));
        }

        [Test]
        public void Record_IgnoresMetricNotInActiveSet()
        {
            var p = MissionTracker.Record(Run(floors: 25, perfects: 10), new[] { Floors() }, Empty);
            Assert.That(p.ContainsKey("m_perfects_weekly"), Is.False);
        }

        [Test]
        public void Record_PeakMetric_KeepsBestNotSum()
        {
            var chainMission = new MissionInfo("m_perfect_chain", MissionMetric.PerfectChainLength, 8, 150);
            var p = MissionTracker.Record(Run(chain: 5), new[] { chainMission }, Empty);
            p = MissionTracker.Record(Run(chain: 3), new[] { chainMission }, p); // lower → keep 5
            Assert.That(p["m_perfect_chain"], Is.EqualTo(5));
        }

        [Test]
        public void Record_DoesNotMutateInput()
        {
            var input = new Dictionary<string, int>();
            MissionTracker.Record(Run(floors: 10), new[] { Floors() }, input);
            Assert.That(input.Count, Is.Zero); // returned a new map
        }

        [Test]
        public void DistrictFilter_OnlyCountsMatchingDistrict()
        {
            var p = MissionTracker.Record(Run(district: "downtown"), new[] { NeonRuns() }, Empty);
            Assert.That(p["m_district_runs"], Is.Zero); // wrong district → no increment
            p = MissionTracker.Record(Run(district: "neon"), new[] { NeonRuns() }, p);
            Assert.That(p["m_district_runs"], Is.EqualTo(1));
        }

        [Test]
        public void IsComplete_TrueAtThreshold()
        {
            var p = new Dictionary<string, int> { ["m_floors_weekly"] = 200 };
            Assert.That(MissionTracker.IsComplete("m_floors_weekly", p, 200), Is.True);
            Assert.That(MissionTracker.IsComplete("m_floors_weekly", p, 201), Is.False);
            Assert.That(MissionTracker.IsComplete("missing", p, 1), Is.False);
        }

        // --- Weekly draw ---

        static readonly string[] Pool =
        {
            "m_floors_weekly", "m_perfects_weekly", "m_daily_runs", "m_tall_tower",
            "m_perfect_chain", "m_residents_weekly", "m_district_runs", "m_streak_days",
        };

        [Test]
        public void DrawWeeklyMissions_Returns3Distinct()
        {
            var draw = MissionTracker.DrawWeeklyMissions(Pool, 123456789UL);
            Assert.That(draw.Count, Is.EqualTo(3));
            Assert.That(draw, Is.Unique);
            foreach (string id in draw) Assert.That(Pool, Contains.Item(id));
        }

        [Test]
        public void DrawWeeklyMissions_DeterministicForSameSeed()
        {
            var a = MissionTracker.DrawWeeklyMissions(Pool, 999UL);
            var b = MissionTracker.DrawWeeklyMissions(Pool, 999UL);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void DrawWeeklyMissions_DifferentSeedsUsuallyDiffer()
        {
            var a = MissionTracker.DrawWeeklyMissions(Pool, 1UL);
            var b = MissionTracker.DrawWeeklyMissions(Pool, 2UL);
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void DrawWeeklyMissions_CountAbovePool_ReturnsWholePool()
        {
            var small = new[] { "a", "b" };
            var draw = MissionTracker.DrawWeeklyMissions(small, 42UL, 3);
            Assert.That(draw.Count, Is.EqualTo(2));
            Assert.That(draw, Is.Unique);
        }
    }
}
