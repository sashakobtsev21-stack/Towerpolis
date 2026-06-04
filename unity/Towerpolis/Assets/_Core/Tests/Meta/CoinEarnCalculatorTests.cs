using System;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class CoinEarnCalculatorTests
    {
        static readonly CoreConfig Cfg = new CoreConfig(); // CoinPerFloor=1, CoinBonusPerfect=2

        [Test]
        public void ZeroFloorRun_EarnsNothing()
        {
            var r = new RunResult(0, 0, 0, 0);
            Assert.That(CoinEarnCalculator.RunCoins(r, Cfg), Is.Zero);
        }

        [Test]
        public void NoPerfects_EarnsOnePerFloor()
        {
            var r = new RunResult(floorCount: 12, totalResidents: 24, runScore: 999, perfectDrops: 0);
            Assert.That(CoinEarnCalculator.RunCoins(r, Cfg), Is.EqualTo(12));
        }

        [Test]
        public void Perfects_AddBonusEach()
        {
            var r = new RunResult(floorCount: 10, totalResidents: 30, runScore: 999, perfectDrops: 4);
            Assert.That(CoinEarnCalculator.RunCoins(r, Cfg), Is.EqualTo(10 + 4 * 2));
        }

        [Test]
        public void SpecExample_20Floors8Perfects_Is36()
        {
            // meta-spec §5.2 worked example.
            var r = new RunResult(20, 40, 1234, 8);
            Assert.That(CoinEarnCalculator.RunCoins(r, Cfg), Is.EqualTo(36));
        }

        [Test]
        public void HonoursTunedRates()
        {
            var cfg = new CoreConfig { CoinPerFloor = 3, CoinBonusPerfect = 5 };
            var r = new RunResult(10, 0, 0, 2);
            Assert.That(CoinEarnCalculator.RunCoins(r, cfg), Is.EqualTo(10 * 3 + 2 * 5));
        }

        [Test]
        public void NullConfig_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CoinEarnCalculator.RunCoins(new RunResult(1, 1, 1, 0), null!));
        }

        // --- City Bonus (progression-spec §2.5) ---

        [Test]
        public void CityBonus_Level3_MultipliesDistrictReward()
        {
            // 500 base × 1.50 = 750.
            Assert.That(CoinEarnCalculator.CityBonusedReward(500, 3, Cfg), Is.EqualTo(750));
        }

        [Test]
        public void CityBonus_Level0_LeavesRewardUnchanged()
        {
            Assert.That(CoinEarnCalculator.CityBonusedReward(500, 0, Cfg), Is.EqualTo(500));
        }

        [Test]
        public void CityBonus_AddedToRunCoins_WhenDistrictCompleted()
        {
            var r = new RunResult(floorCount: 25, totalResidents: 50, runScore: 999, perfectDrops: 10);
            int runOnly = 25 + 10 * 2;                                  // 45
            int withReward = CoinEarnCalculator.RunCoins(r, Cfg, districtCompletedNow: true, baseDistrictRewardCoins: 500, cityBonusLevel: 3);
            Assert.That(withReward, Is.EqualTo(runOnly + 750));
        }

        [Test]
        public void CityBonus_NotApplied_WhenDistrictNotCompleted()
        {
            var r = new RunResult(25, 50, 999, 10);
            int withFlag = CoinEarnCalculator.RunCoins(r, Cfg, districtCompletedNow: false, baseDistrictRewardCoins: 500, cityBonusLevel: 3);
            Assert.That(withFlag, Is.EqualTo(CoinEarnCalculator.RunCoins(r, Cfg))); // run coins only
        }

        [Test]
        public void CityBonus_DoesNotTouchFloorOrPerfectCoins()
        {
            // The multiplier must never change the per-floor/per-perfect portion, regardless of level.
            var r = new RunResult(25, 50, 999, 10);
            int baseRun = CoinEarnCalculator.RunCoins(r, Cfg);
            int completedNoReward = CoinEarnCalculator.RunCoins(r, Cfg, districtCompletedNow: true, baseDistrictRewardCoins: 0, cityBonusLevel: 3);
            Assert.That(completedNoReward, Is.EqualTo(baseRun));
        }
    }
}
