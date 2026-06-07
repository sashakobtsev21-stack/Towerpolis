using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class SaveDataTests
    {
        static DistrictInfo D(string id, int cap, int goal, int rc, int rg) => new DistrictInfo(id, cap, goal, rc, rg);

        static CityState BuildPlayedCity(CoreConfig cfg)
        {
            var s = new CityState(cfg);
            s.EndEndlessRun(D("downtown", 20, 100000, 200, 0), new RunResult(10, 20, 500, 3), 111);
            s.EndDailyRun(D("downtown", 20, 100000, 200, 0), new RunResult(8, 16, 400, 2), 222, "2026-06-04");
            s.EndEndlessRun(D("mini", 3, 5, 100, 1), new RunResult(2, 6, 50, 0), 333); // completes "mini"
            return s;
        }

        [Test]
        public void RoundTrip_PreservesMetaState() // Phase-3 gate 2 (Core level)
        {
            var cfg = new CoreConfig();
            CityState s = BuildPlayedCity(cfg);

            SaveData save = SaveData.From(s);
            CityState loaded = CityState.FromSave(save, cfg);

            Assert.That(loaded.Coins, Is.EqualTo(s.Coins));
            Assert.That(loaded.Gems, Is.EqualTo(s.Gems));
            Assert.That(loaded.TotalPopulation, Is.EqualTo(s.TotalPopulation));
            Assert.That(loaded.ActiveDistrictId, Is.EqualTo(s.ActiveDistrictId));

            Assert.That(loaded.Streak.Current, Is.EqualTo(s.Streak.Current));
            Assert.That(loaded.Streak.Longest, Is.EqualTo(s.Streak.Longest));
            Assert.That(loaded.Streak.LastDate, Is.EqualTo(s.Streak.LastDate));

            Assert.That(loaded.IsRewarded("mini"), Is.True);
            Assert.That(loaded.Leaderboard.Get(LocalLeaderboard.EndlessBest),
                        Is.EqualTo(s.Leaderboard.Get(LocalLeaderboard.EndlessBest)));
            Assert.That(loaded.Leaderboard.Get(LocalLeaderboard.DailyBest),
                        Is.EqualTo(s.Leaderboard.Get(LocalLeaderboard.DailyBest)));

            Assert.That(loaded.Grids["downtown"].Population, Is.EqualTo(s.Grids["downtown"].Population));
            Assert.That(loaded.Grids["downtown"].OccupiedCount, Is.EqualTo(2)); // endless + daily deposits
            Assert.That(loaded.Grids["downtown"].Plots[0].FloorCount, Is.EqualTo(10));
        }

        [Test]
        public void From_StampsCurrentSchemaVersion()
        {
            var save = SaveData.From(new CityState(new CoreConfig()));
            Assert.That(save.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
        }

        [Test]
        public void Migrate_NormalisesUnversionedSave()
        {
            var save = new SaveData { SchemaVersion = 0, Coins = 42 };
            SaveData migrated = SaveMigration.Migrate(save);
            Assert.That(migrated.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
            Assert.That(migrated.Coins, Is.EqualTo(42)); // data preserved
        }

        [Test]
        public void Migrate_NullSave_ReturnsFreshDefault()
        {
            SaveData migrated = SaveMigration.Migrate(null!);
            Assert.That(migrated, Is.Not.Null);
            Assert.That(migrated.SchemaVersion, Is.EqualTo(SaveData.CurrentVersion));
        }

        // --- v1 → v2 (Phase 4 progression schema) ---

        [Test]
        public void Migrate_V1ToV2_PreservesV1Fields()
        {
            var v1 = new SaveData { SchemaVersion = 1, Coins = 500, Gems = 12, StreakCurrent = 4, StreakLongest = 9 };
            SaveData m = SaveMigration.Migrate(v1);
            Assert.That(m.SchemaVersion, Is.EqualTo(2));
            Assert.That(m.Coins, Is.EqualTo(500));
            Assert.That(m.Gems, Is.EqualTo(12));
            Assert.That(m.StreakCurrent, Is.EqualTo(4));
            Assert.That(m.StreakLongest, Is.EqualTo(9));
        }

        [Test]
        public void Migrate_V1ToV2_NewFieldsAtDefaults()
        {
            SaveData m = SaveMigration.Migrate(new SaveData { SchemaVersion = 1 });
            Assert.That(m.MagnetLevel, Is.Zero);
            Assert.That(m.CityBonusLevel, Is.Zero);
            Assert.That(m.StreakFreezeCharges, Is.Zero);
            Assert.That(m.LoginCalendarDay, Is.Zero);
        }
    }
}
