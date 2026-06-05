using NUnit.Framework;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Tests.Gameplay
{
    /// <summary>Edge branches: unknown-FloorType score/resident fallbacks, and the Sloppy grade path —
    /// unreachable under the default config (Good==Sloppy threshold), exercised here via a config that
    /// opens a Sloppy band.</summary>
    public class GameplayEdgeTests
    {
        [Test]
        public void UnknownFloorType_ScoresAndHousesZero()
        {
            var c = new CoreConfig();
            var bogus = (FloorType)99;
            Assert.That(Scoring.BaseScore(c, bogus), Is.Zero);
            Assert.That(Scoring.BaseResidents(c, bogus), Is.Zero);
            Assert.That(Scoring.PerfectResidentBonus(c, bogus), Is.Zero);
        }

        [Test]
        public void Grading_ProducesSloppy_WhenBandConfigured()
        {
            var c = new CoreConfig { GoodThreshold = 0.5f, SloppyThreshold = 0.85f };
            Assert.That(Grading.Evaluate(c, 0.7f * c.InitialBlockWidth, c.InitialBlockWidth), Is.EqualTo(Grade.Sloppy));
        }

        [Test]
        public void Sloppy_DoesNotLand_BreaksComboAndStrikes()
        {
            var c = new CoreConfig { GoodThreshold = 0.5f, SloppyThreshold = 0.85f, StrikeLimit = 99 };
            var run = new TowerRun(c);
            run.PlaceBlock(FloorType.Standard, 0f);                         // Perfect → combo 1
            var o = run.PlaceBlock(FloorType.Standard, 0.7f * run.CurrentTopWidth); // Sloppy
            Assert.That(o.Grade, Is.EqualTo(Grade.Sloppy));
            Assert.That(o.FloorPlaced, Is.False);
            Assert.That(o.ScoreGained, Is.Zero);
            Assert.That(run.MissStrikes, Is.EqualTo(1)); // SloppyCostsStrike defaults true
            Assert.That(run.PerfectChain, Is.Zero);
            Assert.That(run.ComboLevel, Is.Zero);
        }

        [Test]
        public void Sloppy_NoStrike_WhenSloppyCostsStrikeFalse()
        {
            var c = new CoreConfig { GoodThreshold = 0.5f, SloppyThreshold = 0.85f, SloppyCostsStrike = false, StrikeLimit = 99 };
            var run = new TowerRun(c);
            var o = run.PlaceBlock(FloorType.Standard, 0.7f * run.CurrentTopWidth);
            Assert.That(o.Grade, Is.EqualTo(Grade.Sloppy));
            Assert.That(run.MissStrikes, Is.Zero);
        }
    }
}
