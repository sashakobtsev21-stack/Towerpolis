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

        // Bands at the full 2.0 m top: Perfect ≤0.20, Good ≤0.60, Sloppy ≤1.00, Miss above (inclusive ≤).
        [TestCase(0.00f, Grade.Perfect)]
        [TestCase(0.20f, Grade.Perfect)]
        [TestCase(0.2001f, Grade.Good)]
        [TestCase(0.60f, Grade.Good)]
        [TestCase(0.6001f, Grade.Sloppy)]
        [TestCase(1.00f, Grade.Sloppy)]
        [TestCase(1.0001f, Grade.Miss)]
        [TestCase(1.50f, Grade.Miss)]
        public void Bands_AtFullWidth(float offset, Grade expected)
        {
            Assert.That(Grading.Evaluate(Cfg(), offset, 2.0f), Is.EqualTo(expected));
        }

        [Test]
        public void Bands_AreSymmetricForNegativeOffset()
        {
            Assert.That(Grading.Evaluate(Cfg(), -0.20f, 2.0f), Is.EqualTo(Grade.Perfect));
            Assert.That(Grading.Evaluate(Cfg(), -0.80f, 2.0f), Is.EqualTo(Grade.Sloppy));
        }

        // R2: bands scale with the CURRENT top width — same absolute offset grades worse on a narrow tower.
        [Test]
        public void Bands_ScaleWithTopWidth()
        {
            var cfg = Cfg();
            Assert.That(Grading.Evaluate(cfg, 0.10f, 1.0f), Is.EqualTo(Grade.Perfect)); // r=0.10
            Assert.That(Grading.Evaluate(cfg, 0.11f, 1.0f), Is.EqualTo(Grade.Good));    // r=0.11
            Assert.That(Grading.Evaluate(cfg, 0.04f, 0.4f), Is.EqualTo(Grade.Perfect)); // r=0.10 on min width
            Assert.That(Grading.Evaluate(cfg, 0.30f, 0.4f), Is.EqualTo(Grade.Miss));    // r=0.75 on min width
        }

        [Test]
        public void NonPositiveWidth_IsMiss()
        {
            Assert.That(Grading.Evaluate(Cfg(), 0f, 0f), Is.EqualTo(Grade.Miss));
        }
    }
}
