using System;
using System.Collections.Generic;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    /// <summary>Edge branches across the meta layer: the empty SaveData entry ctor, every mission &
    /// achievement metric (incl. an unknown one), the upgrade default arm, crane-skin equip, and a
    /// freeze-granting login claim.</summary>
    public class MetaEdgeTests
    {
        static CoreConfig Cfg() => new CoreConfig();

        [Test]
        public void IntEntry_DefaultCtor_EmptyKeyZeroValue()
        {
            var e = new IntEntry();
            Assert.That(e.Key, Is.Empty);
            Assert.That(e.Value, Is.Zero);
        }

        [Test]
        public void MissionTracker_AppliesEveryMetricKind()
        {
            // (floors, perfects, residents, maxChain, isDaily, district, streak)
            var evt = new MissionEvent(10, 5, 20, 7, true, "downtown", 3);
            var missions = new[]
            {
                new MissionInfo("perf",    MissionMetric.PerfectDrops,       50, 1),
                new MissionInfo("res",     MissionMetric.ResidentsHoused,    400, 1),
                new MissionInfo("daily",   MissionMetric.DailyRunsCompleted, 5,  1),
                new MissionInfo("tall",    MissionMetric.TowerHeight,        40, 1),
                new MissionInfo("streak",  MissionMetric.StreakDays,         5,  1),
                new MissionInfo("unknown", (MissionMetric)999,               1,  1),
            };
            var p = MissionTracker.Record(evt, missions, new Dictionary<string, int>());
            Assert.That(p["perf"], Is.EqualTo(5));
            Assert.That(p["res"], Is.EqualTo(20));
            Assert.That(p["daily"], Is.EqualTo(1));
            Assert.That(p["tall"], Is.EqualTo(10));   // peak metric
            Assert.That(p["streak"], Is.EqualTo(3));  // peak metric
            Assert.That(p["unknown"], Is.Zero);       // default: unchanged
        }

        [Test]
        public void Achievement_TotalPerfects_AndUnknownMetric()
        {
            var defs = new[]
            {
                new AchievementInfo("perf100", AchievementMetric.TotalPerfects, 100, 200),
                new AchievementInfo("weird",   (AchievementMetric)999,         1,   0),
            };
            var newly = AchievementEvaluator.Evaluate(new AchievementSnapshot(0, 0, 100, 0, 0, 0), defs, Array.Empty<string>());
            Assert.That(newly, Contains.Item("perf100"));      // TotalPerfects metric
            Assert.That(newly, Does.Not.Contain("weird"));     // unknown metric → value 0 < threshold
        }

        [Test]
        public void CityState_CompletionQueries_DefaultFalse()
        {
            var s = new CityState(Cfg());
            Assert.That(s.IsMissionCompleted("nope"), Is.False);
            Assert.That(s.IsAchievementUnlocked("nope"), Is.False);
        }

        [Test]
        public void CityState_TryBuyUpgrade_UnknownKind_ReturnsFalse()
        {
            var s = new CityState(Cfg());
            Assert.That(s.TryBuyUpgrade((UpgradeKind)99), Is.False);
        }

        [Test]
        public void CityState_EquipCraneSkin_OnlyWhenOwned()
        {
            var s = new CityState(Cfg());
            Assert.That(s.EquipCraneSkin("crane_steel"), Is.False);     // not owned yet
            Assert.That(s.TryBuyCraneSkin("crane_steel", 0), Is.True);  // free → now owned
            Assert.That(s.EquipCraneSkin("crane_steel"), Is.True);
            Assert.That(s.EquippedCraneSkin, Is.EqualTo("crane_steel"));
        }

        [Test]
        public void CityState_LoginClaim_GrantsFreezeOnDay3()
        {
            var s = new CityState(Cfg());
            s.ClaimLogin("2026-06-01"); // day 1
            s.ClaimLogin("2026-06-02"); // day 2
            s.ClaimLogin("2026-06-03"); // day 3 → +1 freeze charge (calendar slot 3)
            Assert.That(s.FreezeCharges, Is.EqualTo(1));
        }
    }
}
