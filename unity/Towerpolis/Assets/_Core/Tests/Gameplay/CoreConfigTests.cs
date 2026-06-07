using NUnit.Framework;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Tests.Gameplay
{
    /// <summary>Integrity guards on the tunables in <see cref="CoreConfig"/>. These catch a mistuned config
    /// (mismatched parallel arrays, an out-of-range cap, a cheaper-later upgrade) the moment someone edits a
    /// value for balance — exactly the failure mode that's otherwise invisible until runtime.</summary>
    public class CoreConfigTests
    {
        static CoreConfig Cfg() => new CoreConfig();

        [Test]
        public void ComboBonusTable_CoversEveryLevel()
        {
            var c = Cfg();
            Assert.That(c.ComboResidentBonus.Length, Is.GreaterThan(c.ComboLevelCap));
        }

        [Test]
        public void TrophyRoof_ArraysParallel_AndThresholdsAscend()
        {
            var c = Cfg();
            Assert.That(c.TrophyRoofBonusResidents.Length, Is.EqualTo(c.TrophyRoofChainThresholds.Length));
            for (int i = 1; i < c.TrophyRoofChainThresholds.Length; i++)
                Assert.That(c.TrophyRoofChainThresholds[i], Is.GreaterThan(c.TrophyRoofChainThresholds[i - 1]));
        }

        [Test]
        public void UpgradeCosts_AreOneShorterThanTheirEffectTables()
        {
            var c = Cfg();
            // *UpgradeCosts[i] = price to go from level i to i+1, so there's exactly one fewer cost than effect entries.
            Assert.That(c.MagnetUpgradeCosts.Length, Is.EqualTo(c.MagnetFractions.Length - 1));
            Assert.That(c.CityBonusUpgradeCosts.Length, Is.EqualTo(c.CityBonusMultipliers.Length - 1));
        }

        [Test]
        public void UpgradeCosts_Ascend() // each next level must cost more than the last
        {
            var c = Cfg();
            AssertAscending(c.MagnetUpgradeCosts);
            AssertAscending(c.CityBonusUpgradeCosts);
        }

        [Test]
        public void StreakAndLogin_ArraysParallel()
        {
            var c = Cfg();
            Assert.That(c.StreakMilestoneCoins.Length, Is.EqualTo(c.StreakMilestoneDays.Length));
            Assert.That(c.LoginCalendarFreezes.Length, Is.EqualTo(c.LoginCalendarCoins.Length));
        }

        [Test]
        public void Grading_ThresholdsOrdered()
        {
            var c = Cfg();
            Assert.That(c.PerfectThreshold, Is.GreaterThan(0f).And.LessThan(c.GoodThreshold));
            Assert.That(c.GoodThreshold, Is.LessThanOrEqualTo(1.0f));
            Assert.That(c.InitialBlockWidth, Is.GreaterThan(0f));
            Assert.That(c.StrikeLimit, Is.GreaterThan(0));
        }

        [Test]
        public void Residents_NonDecreasingByTier_AndScorePositive()
        {
            var c = Cfg();
            Assert.That(c.ResidentsStandard, Is.GreaterThan(0));
            Assert.That(c.ResidentsBalcony, Is.GreaterThanOrEqualTo(c.ResidentsStandard));
            Assert.That(c.ResidentsPremium, Is.GreaterThanOrEqualTo(c.ResidentsBalcony));
            Assert.That(c.ResidentScoreValue, Is.GreaterThan(0));
            Assert.That(c.CoinPerFloor, Is.GreaterThan(0));
        }

        static void AssertAscending(int[] a)
        {
            for (int i = 1; i < a.Length; i++)
                Assert.That(a[i], Is.GreaterThan(a[i - 1]), "costs must ascend at index {0}", i);
        }
    }
}
