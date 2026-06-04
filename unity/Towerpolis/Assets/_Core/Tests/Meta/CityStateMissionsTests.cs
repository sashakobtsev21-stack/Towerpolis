using System;
using System.Collections.Generic;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class CityStateMissionsTests
    {
        static readonly CoreConfig Cfg = new CoreConfig();
        static DistrictInfo D(string id, int cap, int goal, int rc, int rg) => new DistrictInfo(id, cap, goal, rc, rg);
        static readonly DistrictInfo Big = D("downtown", 100, 1_000_000, 0, 0); // never completes → isolate run coins

        static readonly AchievementInfo[] NoAch = Array.Empty<AchievementInfo>();
        static readonly MissionInfo[] NoMissions = Array.Empty<MissionInfo>();

        static MissionInfo[] Pool() => new[]
        {
            new MissionInfo("m_floors",    MissionMetric.FloorsPlaced,        50,  100),
            new MissionInfo("m_perfects",  MissionMetric.PerfectDrops,        10,  120),
            new MissionInfo("m_daily",     MissionMetric.DailyRunsCompleted,  3,   150),
            new MissionInfo("m_tall",      MissionMetric.TowerHeight,         30,  120),
            new MissionInfo("m_chain",     MissionMetric.PerfectChainLength,  8,   150),
            new MissionInfo("m_residents", MissionMetric.ResidentsHoused,     400, 100),
            new MissionInfo("m_neon",      MissionMetric.DistrictRunsCompleted, 6, 80, "neon"),
            new MissionInfo("m_streak",    MissionMetric.StreakDays,          5,   200),
        };

        // --- weekly draw / reset ---

        [Test]
        public void EnsureWeeklyMissions_DrawsThree_StableWithinWeek()
        {
            var s = new CityState(Cfg);
            s.EnsureWeeklyMissions("2026-W23", 100UL, Pool());
            var first = new List<string>(s.ActiveMissionIds);
            Assert.That(first.Count, Is.EqualTo(3));
            Assert.That(first, Is.Unique);

            s.EnsureWeeklyMissions("2026-W23", 100UL, Pool()); // same week → no redraw
            Assert.That(s.ActiveMissionIds, Is.EqualTo(first));
        }

        [Test]
        public void EnsureWeeklyMissions_RedrawsAndResetsOnNewWeek()
        {
            var s = new CityState(Cfg);
            var floorsOnly = new[] { new MissionInfo("m_floors", MissionMetric.FloorsPlaced, 50, 100) };
            s.EnsureWeeklyMissions("2026-W23", 1UL, floorsOnly);
            var r = new RunResult(20, 40, 100, 0);
            s.ProcessRunSystems(r, false, "downtown", "2026-W23", 1UL, floorsOnly, NoAch);
            Assert.That(s.MissionProgress["m_floors"], Is.EqualTo(20));

            s.EnsureWeeklyMissions("2026-W24", 2UL, floorsOnly); // new week → reset
            Assert.That(s.MissionProgress.Count, Is.Zero);
            Assert.That(s.ActiveWeekKey, Is.EqualTo("2026-W24"));
        }

        // --- mission progress & rewards ---

        [Test]
        public void ProcessRunSystems_CompletesFloorsMission_PaysReward()
        {
            var s = new CityState(Cfg);
            var floorsOnly = new[] { new MissionInfo("m_floors", MissionMetric.FloorsPlaced, 50, 100) };
            var r = new RunResult(60, 120, 999, 5);
            s.EndEndlessRun(Big, r, 1);
            int before = s.Coins;

            RunSystemsOutcome o = s.ProcessRunSystems(r, false, "downtown", "2026-W23", 7UL, floorsOnly, NoAch);
            Assert.That(o.CompletedMissions, Contains.Item("m_floors"));
            Assert.That(o.BonusCoins, Is.EqualTo(100));
            Assert.That(s.Coins, Is.EqualTo(before + 100));
        }

        [Test]
        public void ProcessRunSystems_DoesNotPayTwice()
        {
            var s = new CityState(Cfg);
            var floorsOnly = new[] { new MissionInfo("m_floors", MissionMetric.FloorsPlaced, 50, 100) };
            var r = new RunResult(60, 120, 999, 0);
            s.EndEndlessRun(Big, r, 1);
            s.ProcessRunSystems(r, false, "downtown", "2026-W23", 7UL, floorsOnly, NoAch);
            int afterFirst = s.Coins;
            RunSystemsOutcome again = s.ProcessRunSystems(r, false, "downtown", "2026-W23", 7UL, floorsOnly, NoAch);
            Assert.That(again.CompletedMissions, Is.Empty);
            Assert.That(s.Coins, Is.EqualTo(afterFirst)); // already completed → no double pay
        }

        // --- achievements ---

        [Test]
        public void ProcessRunSystems_UnlocksAchievement_PaysReward()
        {
            var s = new CityState(Cfg);
            var achs = new[] { new AchievementInfo("ach_height_50", AchievementMetric.BestFloorCount, 50, 250) };
            var r = new RunResult(60, 120, 999, 0);
            s.EndEndlessRun(Big, r, 1); // BestFloorCount → 60
            int before = s.Coins;

            RunSystemsOutcome o = s.ProcessRunSystems(r, false, "downtown", "2026-W23", 1UL, NoMissions, achs);
            Assert.That(o.UnlockedAchievements, Contains.Item("ach_height_50"));
            Assert.That(s.Coins, Is.EqualTo(before + 250));
            Assert.That(s.CompletedAchievementIds, Contains.Item("ach_height_50"));
        }

        // --- lifetime stats ---

        [Test]
        public void LifetimeStats_AccumulateAcrossRuns()
        {
            var s = new CityState(Cfg);
            s.EndEndlessRun(Big, new RunResult(20, 40, 100, 3), 1);
            s.EndEndlessRun(Big, new RunResult(35, 70, 200, 5), 2);
            Assert.That(s.LifetimePerfects, Is.EqualTo(8));  // 3 + 5
            Assert.That(s.BestFloorCount, Is.EqualTo(35));   // max(20, 35)
            Assert.That(s.TotalTowers, Is.EqualTo(2));       // two deposits
        }

        // --- persistence ---

        [Test]
        public void RoundTrip_PreservesMissionsAchievementsAndStats()
        {
            var s = new CityState(Cfg);
            var missions = new[] { new MissionInfo("m_floors", MissionMetric.FloorsPlaced, 50, 100) };
            var achs = new[] { new AchievementInfo("ach_height_50", AchievementMetric.BestFloorCount, 50, 250) };
            var r = new RunResult(60, 120, 999, 7);
            s.EndEndlessRun(Big, r, 1);
            s.ProcessRunSystems(r, false, "downtown", "2026-W23", 5UL, missions, achs);

            CityState loaded = CityState.FromSave(SaveData.From(s), Cfg);
            Assert.That(loaded.LifetimePerfects, Is.EqualTo(s.LifetimePerfects));
            Assert.That(loaded.BestFloorCount, Is.EqualTo(60));
            Assert.That(loaded.ActiveWeekKey, Is.EqualTo("2026-W23"));
            Assert.That(loaded.ActiveMissionIds, Is.EqualTo(s.ActiveMissionIds));
            Assert.That(loaded.MissionProgress["m_floors"], Is.EqualTo(60));
            Assert.That(loaded.CompletedMissionIds, Is.EquivalentTo(s.CompletedMissionIds));
            Assert.That(loaded.CompletedAchievementIds, Is.EquivalentTo(s.CompletedAchievementIds));
        }
    }
}
