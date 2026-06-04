using NUnit.Framework;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class LocalLeaderboardTests
    {
        [Test]
        public void SubmitEndless_SetsBests_AndReportsNewBest()
        {
            var lb = new LocalLeaderboard();
            bool best = lb.SubmitEndless(new RunResult(12, 24, 500, 3), "downtown");

            Assert.That(best, Is.True);
            Assert.That(lb.Get(LocalLeaderboard.EndlessBest), Is.EqualTo(500));
            Assert.That(lb.Get(LocalLeaderboard.DistrictBest("downtown")), Is.EqualTo(500));
            Assert.That(lb.Get(LocalLeaderboard.DistrictBestFloors("downtown")), Is.EqualTo(12));
        }

        [Test]
        public void SubmitEndless_DoesNotRegress()
        {
            var lb = new LocalLeaderboard();
            lb.SubmitEndless(new RunResult(20, 40, 900, 5), "downtown");
            bool best = lb.SubmitEndless(new RunResult(5, 10, 200, 1), "downtown");

            Assert.That(best, Is.False);
            Assert.That(lb.Get(LocalLeaderboard.EndlessBest), Is.EqualTo(900));
            Assert.That(lb.Get(LocalLeaderboard.DistrictBestFloors("downtown")), Is.EqualTo(20));
        }

        [Test]
        public void LifetimeStats_Accumulate()
        {
            var lb = new LocalLeaderboard();
            lb.SubmitEndless(new RunResult(10, 20, 300, 3), "downtown");
            lb.SubmitEndless(new RunResult(6, 12, 150, 1), "downtown");
            lb.SubmitEndless(new RunResult(0, 0, 0, 0), "downtown"); // zero-floor: no tower counted

            Assert.That(lb.Get(LocalLeaderboard.StatTowers), Is.EqualTo(2));
            Assert.That(lb.Get(LocalLeaderboard.StatResidents), Is.EqualTo(32));
            Assert.That(lb.Get(LocalLeaderboard.StatPerfects), Is.EqualTo(4));
        }

        [Test]
        public void SubmitDaily_SetsDailyBestAndToday()
        {
            var lb = new LocalLeaderboard();
            lb.SubmitDaily(new RunResult(9, 18, 420, 2), "2026-06-04");

            Assert.That(lb.Get(LocalLeaderboard.DailyBest), Is.EqualTo(420));
            Assert.That(lb.Get(LocalLeaderboard.DailyToday("2026-06-04")), Is.EqualTo(420));
        }

        [Test]
        public void Keys_AreStableStrings()
        {
            Assert.That(LocalLeaderboard.DistrictBest("neon"), Is.EqualTo("district_best_neon"));
            Assert.That(LocalLeaderboard.DailyToday("2026-06-04"), Is.EqualTo("daily_today_2026-06-04"));
        }
    }
}
