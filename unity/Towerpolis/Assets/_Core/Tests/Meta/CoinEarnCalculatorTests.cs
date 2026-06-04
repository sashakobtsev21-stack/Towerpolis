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
    }
}
