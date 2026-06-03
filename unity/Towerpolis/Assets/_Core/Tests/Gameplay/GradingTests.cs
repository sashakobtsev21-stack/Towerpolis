using NUnit.Framework;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Tests.Gameplay
{
    public class GradingTests
    {
        static CoreConfig Cfg() => new CoreConfig();

        [Test]
        public void Center_IsPerfect()
        {
            Assert.That(Grading.Evaluate(Cfg(), 0f, 2.0f), Is.EqualTo(Grade.Perfect));
        }

        // Bands at the full 2.0 m top: Perfect ≤0.30 (snap), Good ≤1.80 (caught), Miss above (bounce).
        [TestCase(0.00f, Grade.Perfect)]
        [TestCase(0.30f, Grade.Perfect)]
        [TestCase(0.3001f, Grade.Good)]
        [TestCase(1.00f, Grade.Good)]
        [TestCase(1.80f, Grade.Good)]
        [TestCase(1.8001f, Grade.Miss)]
        [TestCase(1.95f, Grade.Miss)]
        public void Bands_AtFullWidth(float offset, Grade expected)
        {
            Assert.That(Grading.Evaluate(Cfg(), offset, 2.0f), Is.EqualTo(expected));
        }

        [Test]
        public void Bands_AreSymmetricForNegativeOffset()
        {
            Assert.That(Grading.Evaluate(Cfg(), -0.20f, 2.0f), Is.EqualTo(Grade.Perfect));
            Assert.That(Grading.Evaluate(Cfg(), -0.80f, 2.0f), Is.EqualTo(Grade.Good));
            Assert.That(Grading.Evaluate(Cfg(), -1.90f, 2.0f), Is.EqualTo(Grade.Miss));
        }

        // Bands scale with the top width — same absolute offset grades worse on a narrower surface.
        [Test]
        public void Bands_ScaleWithTopWidth()
        {
            var cfg = Cfg();
            Assert.That(Grading.Evaluate(cfg, 0.15f, 1.0f), Is.EqualTo(Grade.Perfect)); // r=0.15
            Assert.That(Grading.Evaluate(cfg, 0.20f, 1.0f), Is.EqualTo(Grade.Good));    // r=0.20
            Assert.That(Grading.Evaluate(cfg, 0.06f, 0.4f), Is.EqualTo(Grade.Perfect)); // r=0.15 on min width
            Assert.That(Grading.Evaluate(cfg, 0.40f, 0.4f), Is.EqualTo(Grade.Miss));    // r=1.0 on min width
        }

        [Test]
        public void NonPositiveWidth_IsMiss()
        {
            Assert.That(Grading.Evaluate(Cfg(), 0f, 0f), Is.EqualTo(Grade.Miss));
        }
    }
}
