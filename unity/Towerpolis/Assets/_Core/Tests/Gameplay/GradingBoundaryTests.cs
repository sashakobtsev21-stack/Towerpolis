using NUnit.Framework;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Tests.Gameplay
{
    /// <summary>
    /// Exact-threshold precision for the core stacker decision (Grading.Evaluate). Locks the boundary
    /// semantics (≤, not <) for Perfect/Good and the degenerate-width guard, so a refactor can't silently
    /// shift the feel. width=1 is used so ratio == |offsetX| (no float drift on the boundary literals).
    /// </summary>
    public class GradingBoundaryTests
    {
        static CoreConfig Cfg() => new CoreConfig(); // PerfectThreshold 0.15, GoodThreshold 0.80

        [Test]
        public void DeadCentre_IsPerfect()
            => Assert.That(Grading.Evaluate(Cfg(), 0f, 1f), Is.EqualTo(Grade.Perfect));

        [Test]
        public void ExactlyAtPerfectThreshold_IsPerfect() // ratio == 0.15, boundary is inclusive (≤)
            => Assert.That(Grading.Evaluate(Cfg(), 0.15f, 1f), Is.EqualTo(Grade.Perfect));

        [Test]
        public void JustAbovePerfectThreshold_IsGood()
            => Assert.That(Grading.Evaluate(Cfg(), 0.151f, 1f), Is.EqualTo(Grade.Good));

        [Test]
        public void ExactlyAtGoodThreshold_IsGood() // ratio == 0.80, inclusive
            => Assert.That(Grading.Evaluate(Cfg(), 0.80f, 1f), Is.EqualTo(Grade.Good));

        [Test]
        public void JustAboveGoodThreshold_IsMiss() // Sloppy collapsed into Good by default → next band is Miss
            => Assert.That(Grading.Evaluate(Cfg(), 0.801f, 1f), Is.EqualTo(Grade.Miss));

        [Test]
        public void NegativeOffset_IsSymmetric() // |offsetX| → left and right of centre grade identically
        {
            Assert.That(Grading.Evaluate(Cfg(), -0.15f, 1f), Is.EqualTo(Grade.Perfect));
            Assert.That(Grading.Evaluate(Cfg(), -0.50f, 1f), Is.EqualTo(Grade.Good));
            Assert.That(Grading.Evaluate(Cfg(), -0.90f, 1f), Is.EqualTo(Grade.Miss));
        }

        [Test]
        public void ZeroWidth_IsMiss() // degenerate top width guard (block shrank to nothing)
            => Assert.That(Grading.Evaluate(Cfg(), 0f, 0f), Is.EqualTo(Grade.Miss));

        [Test]
        public void NegativeWidth_IsMiss()
            => Assert.That(Grading.Evaluate(Cfg(), 0f, -1f), Is.EqualTo(Grade.Miss));

        [Test]
        public void WiderBlock_ToleratesLargerOffset() // same offset, bigger top → smaller ratio → better grade
        {
            // offset 0.30 on a width-1 block = ratio 0.30 (Good); on a width-3 block = ratio 0.10 (Perfect).
            Assert.That(Grading.Evaluate(Cfg(), 0.30f, 1f), Is.EqualTo(Grade.Good));
            Assert.That(Grading.Evaluate(Cfg(), 0.30f, 3f), Is.EqualTo(Grade.Perfect));
        }
    }
}
