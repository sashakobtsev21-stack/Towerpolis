using System.Collections.Generic;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    /// <summary>Save migration + load robustness + clamp edges — the branches that protect old/partial saves
    /// and out-of-range upgrade levels from corrupting state.</summary>
    public class SaveRobustnessTests
    {
        static CoreConfig Cfg() => new CoreConfig();

        // ----- SaveMigration -----

        [Test]
        public void Migrate_Null_ReturnsFreshCurrentVersion()
        {
            var s = SaveMigration.Migrate(null);
            Assert.That(s, Is.Not.Null);
            Assert.That(s.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
        }

        [Test]
        public void Migrate_PreVersioning_TreatedAsV1AndUpgraded()
        {
            var s = new SaveData { SchemaVersion = 0 };
            SaveMigration.Migrate(s);
            Assert.That(s.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
        }

        [Test]
        public void Migrate_V1_NullFields_FilledWithDefaults() // the null/empty side of every V1→V2 guard
        {
            var s = new SaveData
            {
                SchemaVersion = 1,
                ActiveMissionIds = null, MissionProgress = null,
                CompletedMissionIds = null, CompletedAchievementIds = null,
                LoginCalendarLastClaim = null, ActiveWeekKey = null,
            };
            SaveMigration.Migrate(s);
            Assert.That(s.SchemaVersion, Is.EqualTo(2));
            Assert.That(s.ActiveMissionIds, Is.Not.Null);
            Assert.That(s.MissionProgress, Is.Not.Null);
            Assert.That(s.CompletedMissionIds, Is.Not.Null);
            Assert.That(s.CompletedAchievementIds, Is.Not.Null);
            Assert.That(s.LoginCalendarLastClaim, Is.Empty);
            Assert.That(s.ActiveWeekKey, Is.Empty);
        }

        [Test]
        public void Migrate_AlreadyCurrent_NoChange()
        {
            var s = new SaveData { SchemaVersion = SaveData.CurrentVersion, Coins = 42 };
            SaveMigration.Migrate(s);
            Assert.That(s.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(s.Coins, Is.EqualTo(42));
        }

        // ----- CityState.FromSave robustness -----

        [Test]
        public void FromSave_Null_ReturnsFreshGuestCity()
        {
            var state = CityState.FromSave(null, Cfg());
            Assert.That(state.Coins, Is.Zero);
            Assert.That(state.ActiveDistrictId, Is.EqualTo("downtown"));
        }

        [Test]
        public void FromSave_EmptyStrings_FallBackToDefaults() // the IsNullOrEmpty/?? branches
        {
            var s = new SaveData { ActiveDistrictId = "", ActiveWeekKey = null };
            var state = CityState.FromSave(s, Cfg());
            Assert.That(state.ActiveDistrictId, Is.EqualTo("downtown"));
            Assert.That(state.ActiveWeekKey, Is.Empty);
        }

        // ----- Clamp edges -----

        [Test]
        public void CityBonusedReward_ClampsLevel_AndHandlesEmptyTable()
        {
            var c = Cfg();
            Assert.That(CoinEarnCalculator.CityBonusedReward(100, -5, c), Is.EqualTo(100)); // < 0 → level 0 (×1.0)
            int top = (int)System.Math.Floor(100 * c.CityBonusMultipliers[c.CityBonusMultipliers.Length - 1]);
            Assert.That(CoinEarnCalculator.CityBonusedReward(100, 999, c), Is.EqualTo(top)); // ≥ len → last
            var empty = new CoreConfig { CityBonusMultipliers = new float[0] };
            Assert.That(CoinEarnCalculator.CityBonusedReward(100, 0, empty), Is.EqualTo(100)); // empty table → base
        }

        [Test]
        public void Magnet_ClampsLevelOutOfRange()
        {
            var c = Cfg();
            Assert.That(UpgradeService.GetMagnetFraction(-1, c, false), Is.EqualTo(c.MagnetFractions[0]));
            Assert.That(UpgradeService.GetMagnetFraction(999, c, false),
                Is.EqualTo(c.MagnetFractions[c.MagnetFractions.Length - 1]));
        }

        [Test]
        public void Mission_DailyRuns_NotCountedForEndlessRun() // the false side of IsDailyRun
        {
            var endless = new MissionEvent(5, 0, 0, 0, false, "downtown", 0);
            var p = MissionTracker.Record(in endless,
                new[] { new MissionInfo("daily", MissionMetric.DailyRunsCompleted, 5, 1) },
                new Dictionary<string, int>());
            Assert.That(p["daily"], Is.Zero);
        }
    }
}
