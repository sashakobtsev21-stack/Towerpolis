using System;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class UpgradeServiceTests
    {
        static readonly CoreConfig Cfg = new CoreConfig();
        static readonly int[] MagnetCosts = { 80, 200, 450, 900 }; // cost to reach level 1..4
        const int MagnetMax = 4;

        // --- TryPurchase ---

        [Test]
        public void TryPurchase_SucceedsWhenAffordable_DeductsAndIncrements()
        {
            var (ok, newCoins, newLevel) = UpgradeService.TryPurchase("magnet", 0, MagnetCosts, MagnetMax, 100);
            Assert.That(ok, Is.True);
            Assert.That(newCoins, Is.EqualTo(20));   // 100 − 80
            Assert.That(newLevel, Is.EqualTo(1));
        }

        [Test]
        public void TryPurchase_ExactCoins_Succeeds()
        {
            var (ok, newCoins, newLevel) = UpgradeService.TryPurchase("magnet", 1, MagnetCosts, MagnetMax, 200);
            Assert.That(ok, Is.True);
            Assert.That(newCoins, Is.Zero);
            Assert.That(newLevel, Is.EqualTo(2));
        }

        [Test]
        public void TryPurchase_FailsWhenInsufficient_LeavesStateUnchanged()
        {
            var (ok, newCoins, newLevel) = UpgradeService.TryPurchase("magnet", 0, MagnetCosts, MagnetMax, 79);
            Assert.That(ok, Is.False);
            Assert.That(newCoins, Is.EqualTo(79)); // unchanged
            Assert.That(newLevel, Is.EqualTo(0));
        }

        [Test]
        public void TryPurchase_FailsAtMaxLevel()
        {
            var (ok, newCoins, newLevel) = UpgradeService.TryPurchase("magnet", MagnetMax, MagnetCosts, MagnetMax, 99999);
            Assert.That(ok, Is.False);
            Assert.That(newCoins, Is.EqualTo(99999));
            Assert.That(newLevel, Is.EqualTo(MagnetMax));
        }

        [Test]
        public void TryPurchase_NullCosts_Throws()
        {
            Assert.Throws<ArgumentNullException>(
                () => UpgradeService.TryPurchase("magnet", 0, null!, 4, 100));
        }

        // --- Gameplay effect: daily suppression ---

        [Test]
        public void MagnetFraction_IsZeroWhenDaily_RegardlessOfLevel()
        {
            Assert.That(UpgradeService.GetMagnetFraction(MagnetMax, Cfg, isDaily: true), Is.EqualTo(0f));
        }

        [Test]
        public void MagnetFraction_AppliesConfigWhenEndless()
        {
            Assert.That(UpgradeService.GetMagnetFraction(4, Cfg, isDaily: false), Is.EqualTo(0.45f).Within(1e-6f));
            Assert.That(UpgradeService.GetMagnetFraction(0, Cfg, isDaily: false), Is.EqualTo(0f)); // unupgraded = no help
        }

        [Test]
        public void Fraction_ClampsLevelAboveTable()
        {
            // A level beyond the table clamps to the last entry (defensive against a stale save).
            Assert.That(UpgradeService.GetMagnetFraction(99, Cfg, isDaily: false), Is.EqualTo(0.45f).Within(1e-6f));
        }

        // --- UpgradeState ---

        [Test]
        public void UpgradeState_Default_IsAllZero()
        {
            var s = UpgradeState.Default;
            Assert.That(s.MagnetLevel, Is.Zero);
            Assert.That(s.CityBonusLevel, Is.Zero);
        }

        [Test]
        public void UpgradeState_WithMagnet_ReplacesOnlyMagnet()
        {
            var s = UpgradeState.Default.WithMagnet(3);
            Assert.That(s.MagnetLevel, Is.EqualTo(3));
            Assert.That(s.CityBonusLevel, Is.Zero);
        }
    }
}
