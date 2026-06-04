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
