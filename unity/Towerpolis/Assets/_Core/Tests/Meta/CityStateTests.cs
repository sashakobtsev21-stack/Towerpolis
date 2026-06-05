using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class CityStateTests
    {
        static CoreConfig Cfg() => new CoreConfig();
        static DistrictInfo D(string id, int cap, int goal, int rc, int rg) => new DistrictInfo(id, cap, goal, rc, rg);

        [Test]
        public void EndEndlessRun_DepositsAndEarnsCoins()
        {
            var s = new CityState(Cfg());
            var outcome = s.EndEndlessRun(D("downtown", 20, 10000, 200, 0), new RunResult(10, 20, 500, 3), timestampUtcTicks: 111);

            Assert.That(outcome.Deposited, Is.True);
            Assert.That(outcome.CoinsEarned, Is.EqualTo(10 + 3 * 2)); // 16
            Assert.That(outcome.DistrictPopulation, Is.EqualTo(20));
            Assert.That(outcome.DistrictCompletedNow, Is.False);
            Assert.That(s.Coins, Is.EqualTo(16));
            Assert.That(s.TotalPopulation, Is.EqualTo(20));
            Assert.That(s.Leaderboard.Get(LocalLeaderboard.EndlessBest), Is.EqualTo(500));
        }

        [Test]
        public void ZeroFloorRun_DepositsNothing_NoCoins()
        {
            var s = new CityState(Cfg());
            var outcome = s.EndEndlessRun(D("downtown", 20, 10000, 200, 0), new RunResult(0, 0, 0, 0), timestampUtcTicks: 0);

            Assert.That(outcome.Deposited, Is.False);
            Assert.That(outcome.CoinsEarned, Is.Zero);
            Assert.That(s.TotalPopulation, Is.Zero);
            Assert.That(s.Leaderboard.Get(LocalLeaderboard.StatTowers), Is.Zero);
        }

        [Test]
        public void DistrictComplete_FiresOnce_AndPaysRewardOnce()
        {
            var s = new CityState(Cfg());
            var d = D("mini", cap: 5, goal: 6, rc: 100, rg: 1);

            var first = s.EndEndlessRun(d, new RunResult(3, 7, 100, 0), timestampUtcTicks: 1); // pop 7 >= 6 → completes
            Assert.That(first.DistrictCompletedNow, Is.True);
            Assert.That(first.GemsEarned, Is.EqualTo(1));
            Assert.That(first.CoinsEarned, Is.EqualTo(3 + 100)); // run 3 + reward 100
            Assert.That(first.DistrictRewardCoins, Is.EqualTo(100)); // the reward portion alone (for the complete screen)
            Assert.That(s.Gems, Is.EqualTo(1));
            Assert.That(s.IsRewarded("mini"), Is.True);

            var second = s.EndEndlessRun(d, new RunResult(2, 5, 50, 0), timestampUtcTicks: 2); // already rewarded
            Assert.That(second.DistrictCompletedNow, Is.False);
            Assert.That(second.GemsEarned, Is.Zero);
            Assert.That(second.CoinsEarned, Is.EqualTo(2)); // run only, no second reward
            Assert.That(second.DistrictRewardCoins, Is.Zero); // no reward second time
            Assert.That(s.Gems, Is.EqualTo(1)); // unchanged
        }

        [Test]
        public void DistrictCompletes_ViaBestNReplacement_NoSoftLock()
        {
            var s = new CityState(Cfg());
            var d = D("mini", cap: 2, goal: 50, rc: 100, rg: 1);

            // Fill both plots with small towers: grid full, pop 20 < goal 50. (Old behaviour bricked here.)
            s.EndEndlessRun(d, new RunResult(2, 10, 0, 0), 1);
            var full = s.EndEndlessRun(d, new RunResult(2, 10, 0, 0), 2);
            Assert.That(full.DistrictCompletedNow, Is.False);
            Assert.That(s.Grids["mini"].Population, Is.EqualTo(20));

            // Keep improving: bigger towers replace the smallest until the goal is reached.
            s.EndEndlessRun(d, new RunResult(8, 30, 0, 0), 3);          // replaces a 10 → pop 40
            var done = s.EndEndlessRun(d, new RunResult(9, 30, 0, 0), 4); // replaces the other 10 → pop 60 ≥ 50
            Assert.That(done.DistrictCompletedNow, Is.True);
            Assert.That(done.DistrictRewardCoins, Is.EqualTo(100));
            Assert.That(s.Grids["mini"].Population, Is.EqualTo(60));
        }

        [Test]
        public void EndDailyRun_FirstOfDay_AwardsBonusAndStreak()
        {
            var s = new CityState(Cfg());
            var d = D("downtown", 20, 100000, 200, 0);
            var outcome = s.EndDailyRun(d, new RunResult(5, 10, 200, 1), timestampUtcTicks: 1, dateKey: "2026-06-04");

            // run 5+2 = 7, + first-win 50, milestone(streak 1)=0
            Assert.That(outcome.CoinsEarned, Is.EqualTo(7 + 50));
            Assert.That(outcome.StreakMilestoneCoins, Is.Zero);
            Assert.That(s.Streak.Current, Is.EqualTo(1));
            Assert.That(s.Leaderboard.Get(LocalLeaderboard.DailyToday("2026-06-04")), Is.EqualTo(200));
        }

        [Test]
        public void DailyStreak_HitsMilestoneOnDay3()
        {
            var s = new CityState(Cfg());
            var d = D("downtown", 20, 100000, 200, 0);
            var r = new RunResult(5, 10, 200, 0);

            s.EndDailyRun(d, r, 1, "2026-06-04");
            s.EndDailyRun(d, r, 2, "2026-06-05");
            var day3 = s.EndDailyRun(d, r, 3, "2026-06-06");

            Assert.That(s.Streak.Current, Is.EqualTo(3));
            Assert.That(day3.StreakMilestoneCoins, Is.EqualTo(75));
        }

        [Test]
        public void TotalPopulation_SumsAcrossDistricts()
        {
            var s = new CityState(Cfg());
            s.EndEndlessRun(D("downtown", 20, 100000, 0, 0), new RunResult(10, 20, 100, 0), 1);
            s.EndEndlessRun(D("neon", 20, 100000, 0, 0), new RunResult(8, 16, 80, 0), 2);
            Assert.That(s.TotalPopulation, Is.EqualTo(36));
        }
    }
}
