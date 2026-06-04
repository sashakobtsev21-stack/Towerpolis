using NUnit.Framework;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class DistrictGoalTests
    {
        [TestCase(0, 1200, false)]
        [TestCase(1199, 1200, false)]
        [TestCase(1200, 1200, true)]
        [TestCase(1500, 1200, true)]
        public void IsReached_AtOrAboveGoal(int population, int goal, bool expected)
        {
            Assert.That(DistrictGoal.IsReached(population, goal), Is.EqualTo(expected));
        }

        [Test]
        public void ZeroGoal_NeverReached()
        {
            Assert.That(DistrictGoal.IsReached(0, 0), Is.False);
            Assert.That(DistrictGoal.IsReached(999, 0), Is.False);
        }
    }
}
