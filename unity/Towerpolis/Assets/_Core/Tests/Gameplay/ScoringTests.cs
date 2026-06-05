using NUnit.Framework;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Tests.Gameplay
{
    public class ScoringTests
    {
        static CoreConfig Cfg() => new CoreConfig();

        // Chain-bonus tier boundaries (spec §6.2): <=2→50, <=5→150, <=10→350, >10→600.
        [TestCase(0, 0)]
        [TestCase(1, 50)]
        [TestCase(2, 50)]
        [TestCase(3, 150)]
        [TestCase(5, 150)]
        [TestCase(6, 350)]
        [TestCase(10, 350)]
        [TestCase(11, 600)]
        [TestCase(100, 600)]
        public void ChainBonus_TierBoundaries(int chain, int expected)
        {
            Assert.That(Scoring.ChainBonus(Cfg(), chain), Is.EqualTo(expected));
        }

        // Combo resident bonus (Phase A): default table {0,1,2,4}, index clamped both ends.
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(3, 4)]
        [TestCase(4, 4)]   // above the table → clamped to last
        [TestCase(-1, 0)]  // below 0 → clamped to first
        public void ComboResidentBonus_ClampsIndex(int level, int expected)
        {
            Assert.That(Scoring.ComboResidentBonus(Cfg(), level), Is.EqualTo(expected));
        }

        [Test]
        public void ComboResidentBonus_EmptyTable_ReturnsZero()
        {
            var cfg = new CoreConfig { ComboResidentBonus = new int[0] };
            Assert.That(Scoring.ComboResidentBonus(cfg, 2), Is.Zero);
        }

        // Trophy-roof bonus (Phase C): defaults {4→8, 8→20, 12→40, 20→70}, largest threshold ≤ maxChain.
        [TestCase(0, 0)]
        [TestCase(3, 0)]
        [TestCase(4, 8)]
        [TestCase(7, 8)]
        [TestCase(8, 20)]
        [TestCase(11, 20)]
        [TestCase(12, 40)]
        [TestCase(19, 40)]
        [TestCase(20, 70)]
        [TestCase(100, 70)]
        public void TrophyRoofBonus_TierBoundaries(int maxChain, int expected)
        {
            Assert.That(Scoring.TrophyRoofBonus(Cfg(), maxChain), Is.EqualTo(expected));
        }

        [TestCase(FloorType.Standard, 100)]
        [TestCase(FloorType.Balcony, 150)]
        [TestCase(FloorType.Premium, 200)]
        public void BaseScore_PerType(FloorType type, int expected)
        {
            Assert.That(Scoring.BaseScore(Cfg(), type), Is.EqualTo(expected));
        }

        [TestCase(FloorType.Standard, 2)]
        [TestCase(FloorType.Balcony, 3)]
        [TestCase(FloorType.Premium, 5)]
        public void BaseResidents_PerType(FloorType type, int expected)
        {
            Assert.That(Scoring.BaseResidents(Cfg(), type), Is.EqualTo(expected));
        }

        [Test]
        public void FloorScore_CoversBalconyAndPremium()
        {
            var c = Cfg();
            // Balcony base 150
            Assert.That(Scoring.FloorScore(c, FloorType.Balcony, Grade.Good, 0), Is.EqualTo(150));
            Assert.That(Scoring.FloorScore(c, FloorType.Balcony, Grade.Sloppy, 0), Is.EqualTo(75));
            Assert.That(Scoring.FloorScore(c, FloorType.Balcony, Grade.Perfect, 1), Is.EqualTo(300 + 50));
            // Premium base 200
            Assert.That(Scoring.FloorScore(c, FloorType.Premium, Grade.Good, 0), Is.EqualTo(200));
            Assert.That(Scoring.FloorScore(c, FloorType.Premium, Grade.Perfect, 3), Is.EqualTo(400 + 150));
            // Miss scores zero regardless of type/chain
            Assert.That(Scoring.FloorScore(c, FloorType.Premium, Grade.Miss, 5), Is.EqualTo(0));
        }

        [Test]
        public void GradeMultiplier_MatchesSpecDefaults()
        {
            var c = Cfg();
            Assert.That(Scoring.GradeMultiplier(c, Grade.Perfect), Is.EqualTo(2.0f));
            Assert.That(Scoring.GradeMultiplier(c, Grade.Good), Is.EqualTo(1.0f));
            Assert.That(Scoring.GradeMultiplier(c, Grade.Sloppy), Is.EqualTo(0.5f));
            Assert.That(Scoring.GradeMultiplier(c, Grade.Miss), Is.EqualTo(0.0f));
        }
    }
}
