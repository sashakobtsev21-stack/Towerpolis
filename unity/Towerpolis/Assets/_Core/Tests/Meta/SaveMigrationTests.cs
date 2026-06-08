using NUnit.Framework;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    /// <summary>
    /// Golden fixtures for the forward-only save migration (ADR-0007). These guard against player
    /// data loss when an old save loads on a newer schema — the path SaveMigration.cs documents but
    /// previously had no test for.
    /// </summary>
    public class SaveMigrationTests
    {
        [Test]
        public void Migrate_Null_ReturnsFreshSaveAtCurrentVersion()
        {
            var s = SaveMigration.Migrate(null);
            Assert.That(s, Is.Not.Null);
            Assert.That(s.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
        }

        [Test]
        public void Migrate_UnversionedSave_TreatedAsV1AndUpgraded()
        {
            var s = new SaveData { SchemaVersion = 0 };
            SaveMigration.Migrate(s);
            Assert.That(s.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
        }

        [Test]
        public void Migrate_NegativeVersion_TreatedAsV1AndUpgraded()
        {
            var s = new SaveData { SchemaVersion = -5 };
            SaveMigration.Migrate(s);
            Assert.That(s.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
        }

        [Test]
        public void Migrate_V1_BumpsToCurrent_AndGuardsNullCollections()
        {
            var s = new SaveData
            {
                SchemaVersion = 1,
                ActiveMissionIds = null,
                MissionProgress = null,
                CompletedMissionIds = null,
                CompletedAchievementIds = null,
                LoginCalendarLastClaim = null,
                ActiveWeekKey = null,
            };
            SaveMigration.Migrate(s);

            Assert.That(s.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(s.ActiveMissionIds, Is.Not.Null);
            Assert.That(s.MissionProgress, Is.Not.Null);
            Assert.That(s.CompletedMissionIds, Is.Not.Null);
            Assert.That(s.CompletedAchievementIds, Is.Not.Null);
            Assert.That(s.LoginCalendarLastClaim, Is.EqualTo(""));
            Assert.That(s.ActiveWeekKey, Is.EqualTo(""));
        }

        [Test]
        public void Migrate_V1_PreservesExistingPlayerData()
        {
            var s = new SaveData { SchemaVersion = 1, Coins = 123, Gems = 9, ActiveDistrictId = "neon", StreakCurrent = 4 };
            SaveMigration.Migrate(s);

            Assert.That(s.Coins, Is.EqualTo(123));
            Assert.That(s.Gems, Is.EqualTo(9));
            Assert.That(s.ActiveDistrictId, Is.EqualTo("neon"));
            Assert.That(s.StreakCurrent, Is.EqualTo(4));
        }

        [Test]
        public void Migrate_V2_BumpsToV3_WithPrestigeDefaultsAtZero()
        {
            var s = new SaveData { SchemaVersion = 2, Coins = 50 };
            SaveMigration.Migrate(s);

            Assert.That(s.SchemaVersion, Is.EqualTo(3));
            Assert.That(s.Coins, Is.EqualTo(50));              // v2 data preserved
            Assert.That(s.TotalPrestigeStars, Is.EqualTo(0));  // "never prestiged" defaults
            Assert.That(s.PrestigeCount, Is.EqualTo(0));
            Assert.That(s.LifetimeBestPopulation, Is.EqualTo(0));
        }

        [Test]
        public void Migrate_AlreadyCurrent_IsIdempotent()
        {
            var s = new SaveData { SchemaVersion = SaveData.CurrentVersion, Coins = 500, StreakCurrent = 7, TotalPrestigeStars = 12 };
            SaveMigration.Migrate(s);

            Assert.That(s.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(s.Coins, Is.EqualTo(500));
            Assert.That(s.StreakCurrent, Is.EqualTo(7));
            Assert.That(s.TotalPrestigeStars, Is.EqualTo(12));
        }
    }
}
