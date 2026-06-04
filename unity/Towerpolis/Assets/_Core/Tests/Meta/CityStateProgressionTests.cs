using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class CityStateProgressionTests
    {
        static readonly CoreConfig Cfg = new CoreConfig();
        static DistrictInfo D(string id, int cap, int goal, int rc, int rg) => new DistrictInfo(id, cap, goal, rc, rg);

        // Seed a spendable coin balance by completing a high-reward throwaway district.
        static CityState Rich()
        {
            var s = new CityState(Cfg);
            s.EndEndlessRun(D("seed", 10, 1, 5000, 0), new RunResult(3, 6, 50, 0), 1);
            return s;
        }

        // --- Upgrades ---

        [Test]
        public void BuyUpgrade_Magnet_DeductsAndLevelsUp()
        {
            var s = Rich();
            int before = s.Coins;
            Assert.That(s.TryBuyUpgrade(UpgradeKind.Magnet), Is.True);
            Assert.That(s.Upgrades.MagnetLevel, Is.EqualTo(1));
            Assert.That(s.Coins, Is.EqualTo(before - Cfg.MagnetUpgradeCosts[0])); // −80
        }

        [Test]
        public void BuyUpgrade_FailsWhenUnaffordable()
        {
            var s = new CityState(Cfg); // 0 coins
            Assert.That(s.TryBuyUpgrade(UpgradeKind.Magnet), Is.False);
            Assert.That(s.Upgrades.MagnetLevel, Is.Zero);
            Assert.That(s.Coins, Is.Zero);
        }

        [Test]
        public void BuyUpgrade_CapsAtMaxLevel()
        {
            var s = Rich();
            for (int i = 0; i < Cfg.MagnetUpgradeCosts.Length; i++)
                Assert.That(s.TryBuyUpgrade(UpgradeKind.Magnet), Is.True);
            Assert.That(s.Upgrades.MagnetLevel, Is.EqualTo(Cfg.MagnetUpgradeCosts.Length)); // 4
            Assert.That(s.TryBuyUpgrade(UpgradeKind.Magnet), Is.False); // maxed
        }

        [Test]
        public void CityBonus_MultipliesDistrictReward()
        {
            var s = Rich();
            s.TryBuyUpgrade(UpgradeKind.CityBonus); // L1
            s.TryBuyUpgrade(UpgradeKind.CityBonus); // L2
            s.TryBuyUpgrade(UpgradeKind.CityBonus); // L3 → ×1.50
            Assert.That(s.Upgrades.CityBonusLevel, Is.EqualTo(3));

            RunEndOutcome o = s.EndEndlessRun(D("downtown", 10, 1, 500, 0), new RunResult(2, 4, 20, 0), 2);
            Assert.That(o.DistrictCompletedNow, Is.True);
            Assert.That(o.CoinsEarned, Is.EqualTo(2 + 750)); // run coins (2) + 500×1.50
        }

        // --- Cosmetics ---

        [Test]
        public void BuyBlockSkin_ThenEquip()
        {
            var s = Rich();
            Assert.That(s.TryBuyBlockSkin("skin_pastel", 150), Is.True);
            Assert.That(s.OwnedBlockSkins, Does.Contain("skin_pastel"));
            Assert.That(s.EquipBlockSkin("skin_pastel"), Is.True);
            Assert.That(s.EquippedBlockSkin, Is.EqualTo("skin_pastel"));
        }

        [Test]
        public void BuySkin_FailsWhenAlreadyOwned()
        {
            var s = Rich();
            Assert.That(s.TryBuyBlockSkin("skin_default", 150), Is.False); // owned by default
        }

        [Test]
        public void BuySkin_GatedByDistrict()
        {
            var s = Rich(); // "seed" rewarded, "neon" not
            Assert.That(s.TryBuyCraneSkin("crane_neon", 400, requiredDistrictId: "neon"), Is.False);
            // complete neon → gate opens
            s.EndEndlessRun(D("neon", 10, 1, 0, 0), new RunResult(2, 4, 10, 0), 3);
            Assert.That(s.TryBuyCraneSkin("crane_neon", 400, requiredDistrictId: "neon"), Is.True);
        }

        [Test]
        public void EquipSkin_FailsWhenNotOwned()
        {
            var s = Rich();
            Assert.That(s.EquipBlockSkin("skin_pastel"), Is.False); // not bought
        }

        // --- Streak freeze & login ---

        [Test]
        public void BuyFreezeCharge_IncrementsAndCaps()
        {
            var s = Rich();
            for (int i = 0; i < Cfg.StreakFreezeMaxCharges; i++)
                Assert.That(s.TryBuyFreezeCharge(), Is.True);
            Assert.That(s.FreezeCharges, Is.EqualTo(Cfg.StreakFreezeMaxCharges));
            Assert.That(s.TryBuyFreezeCharge(), Is.False); // capped
        }

        [Test]
        public void ClaimLogin_BanksCoinsAndAdvances()
        {
            var s = Rich();
            int before = s.Coins;
            Assert.That(s.CanClaimLogin("2026-06-04"), Is.True);
            LoginCalendarReward reward = s.ClaimLogin("2026-06-04");
            Assert.That(reward.DayNumber, Is.EqualTo(1));
            Assert.That(s.Coins, Is.EqualTo(before + reward.Coins));
            Assert.That(s.Login.Day, Is.EqualTo(1));
            Assert.That(s.CanClaimLogin("2026-06-04"), Is.False); // once per day
        }

        // --- Persistence round-trip ---

        [Test]
        public void RoundTrip_PreservesProgression()
        {
            var s = Rich();
            s.TryBuyUpgrade(UpgradeKind.Magnet);
            s.TryBuyUpgrade(UpgradeKind.SlowMo);
            s.TryBuyUpgrade(UpgradeKind.CityBonus);
            s.TryBuyBlockSkin("skin_pastel", 150);
            s.EquipBlockSkin("skin_pastel");
            s.TryBuyFreezeCharge();
            s.ClaimLogin("2026-06-04");

            CityState loaded = CityState.FromSave(SaveData.From(s), Cfg);

            Assert.That(loaded.Coins, Is.EqualTo(s.Coins));
            Assert.That(loaded.Upgrades.MagnetLevel, Is.EqualTo(1));
            Assert.That(loaded.Upgrades.SlowMoLevel, Is.EqualTo(1));
            Assert.That(loaded.Upgrades.CityBonusLevel, Is.EqualTo(1));
            Assert.That(loaded.OwnedBlockSkins, Does.Contain("skin_pastel"));
            Assert.That(loaded.EquippedBlockSkin, Is.EqualTo("skin_pastel"));
            Assert.That(loaded.FreezeCharges, Is.EqualTo(s.FreezeCharges));
            Assert.That(loaded.Login.Day, Is.EqualTo(1));
        }
    }
}
