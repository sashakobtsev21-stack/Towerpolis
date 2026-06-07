using System.Collections.Generic;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    /// <summary>Prestige / endless loop (endless-spec §2): banking population into permanent Stars (a residents
    /// multiplier), the wipe, coin retention, the lifetime-best trophy, and the residents-multiplier in TowerRun.</summary>
    public class PrestigeTests
    {
        static CoreConfig Cfg() => new CoreConfig();
        static DistrictInfo D(string id, int goal) => new DistrictInfo(id, gridCapacity: 4, fillGoal: goal, rewardCoins: 0, rewardGems: 0);

        static readonly IReadOnlyList<DistrictInfo> Three = new[] { D("downtown", 5), D("neon", 5), D("winter", 5) };

        // Complete every district in Three so the city is prestige-ready.
        static void CompleteAll(CityState s)
        {
            long t = 1;
            foreach (DistrictInfo d in Three)
                s.EndEndlessRun(d, new RunResult(3, 6, 0, 0), t++); // pop 6 >= goal 5 → completes
        }

        [Test]
        public void Prestige_BanksStars_WipesGridsAndRewards()
        {
            var s = new CityState(Cfg());
            CompleteAll(s);
            Assert.That(s.IsPrestigeReady(Three), Is.True);

            int stars = s.Prestige();
            Assert.That(stars, Is.GreaterThan(0));
            Assert.That(s.TotalPrestigeStars, Is.EqualTo(stars));
            Assert.That(s.PrestigeCount, Is.EqualTo(1));
            Assert.That(s.Grids.Count, Is.Zero);                 // grids wiped
            Assert.That(s.RewardedDistricts.Count, Is.Zero);     // rewards wiped → districts earnable again
            Assert.That(s.ActiveDistrictId, Is.EqualTo("downtown"));
        }

        [Test]
        public void Prestige_LifetimeBestPopulation_UpdatesOnImprovement()
        {
            var s = new CityState(Cfg());
            s.EndEndlessRun(D("a", 1), new RunResult(1, 5000, 0, 0), 1);
            s.Prestige();
            Assert.That(s.LifetimeBestPopulation, Is.EqualTo(5000));

            s.EndEndlessRun(D("a", 1), new RunResult(1, 3000, 0, 0), 2); // smaller cycle
            s.Prestige();
            Assert.That(s.LifetimeBestPopulation, Is.EqualTo(5000)); // no regression
        }

        [Test]
        public void Prestige_CoinsRetained_ByFraction()
        {
            var s = new CityState(Cfg());
            s.EndEndlessRun(new DistrictInfo("a", 1, 1, rewardCoins: 998, rewardGems: 0), new RunResult(2, 5, 0, 0), 1);
            int before = s.Coins; // 2 run coins + 998 reward = 1000
            Assert.That(before, Is.EqualTo(1000));

            s.Prestige();
            Assert.That(s.Coins, Is.EqualTo(500)); // 50% floored to 10s
        }

        [Test]
        public void Prestige_StarFormula_FloorDivision()
        {
            var s = new CityState(Cfg()); // PrestigeStarsPerPop = 200
            s.EndEndlessRun(D("a", 1), new RunResult(1, 510, 0, 0), 1);
            Assert.That(s.Prestige(), Is.EqualTo(2)); // floor(510 / 200)
            Assert.That(s.TotalPrestigeStars, Is.EqualTo(2));
        }

        [Test]
        public void Prestige_StarFormula_MinimumOne()
        {
            var s = new CityState(Cfg());
            s.EndEndlessRun(D("a", 1), new RunResult(1, 50, 0, 0), 1); // 50/200 = 0 → clamps to 1
            Assert.That(s.Prestige(), Is.EqualTo(1));
        }

        [Test]
        public void PrestigeBonusMult_IsCorrect()
        {
            var s = new CityState(Cfg());
            Assert.That(s.PrestigeBonusMult, Is.EqualTo(1.0f).Within(1e-4f)); // none yet
            s.EndEndlessRun(D("a", 1), new RunResult(1, 5000, 0, 0), 1);
            s.Prestige(); // 5000/200 = 25 stars
            Assert.That(s.TotalPrestigeStars, Is.EqualTo(25));
            Assert.That(s.PrestigeBonusMult, Is.EqualTo(1.25f).Within(1e-4f)); // 1 + 25 * 0.01
        }

        [Test]
        public void IsPrestigeReady_TrueOnlyWhenAllRewarded()
        {
            var s = new CityState(Cfg());
            s.EndEndlessRun(Three[0], new RunResult(3, 6, 0, 0), 1);
            s.EndEndlessRun(Three[1], new RunResult(3, 6, 0, 0), 2);
            Assert.That(s.IsPrestigeReady(Three), Is.False); // 2 of 3
            s.EndEndlessRun(Three[2], new RunResult(3, 6, 0, 0), 3);
            Assert.That(s.IsPrestigeReady(Three), Is.True);  // all 3
        }

        [Test]
        public void SaveRoundTrip_PreservesPrestigeState()
        {
            var cfg = Cfg();
            var s = new CityState(cfg);
            s.EndEndlessRun(D("a", 1), new RunResult(1, 5000, 0, 0), 1);
            s.Prestige();

            CityState loaded = CityState.FromSave(SaveData.From(s), cfg);
            Assert.That(loaded.TotalPrestigeStars, Is.EqualTo(s.TotalPrestigeStars));
            Assert.That(loaded.PrestigeCount, Is.EqualTo(s.PrestigeCount));
            Assert.That(loaded.LifetimeBestPopulation, Is.EqualTo(s.LifetimeBestPopulation));
            Assert.That(loaded.PrestigeBonusMult, Is.EqualTo(s.PrestigeBonusMult).Within(1e-4f));
        }

        [Test]
        public void TowerRun_ResidentMult_ScalesResidents()
        {
            var baseRun = new TowerRun(Cfg());
            int basic = baseRun.PlaceBlock(FloorType.Standard, 0f).ResidentsAdded; // 2 + perfect 1 + combo L1 (1) = 4

            var boosted = new TowerRun(Cfg(), residentMult: 2.0f);
            int doubled = boosted.PlaceBlock(FloorType.Standard, 0f).ResidentsAdded;

            Assert.That(basic, Is.EqualTo(4));
            Assert.That(doubled, Is.EqualTo(8)); // floor(4 * 2.0)
        }

        [Test]
        public void Prestige_UpgradesReset_CoinsPartiallyRetained()
        {
            var s = new CityState(Cfg());
            s.EndEndlessRun(new DistrictInfo("a", 1, 1, rewardCoins: 1000, rewardGems: 0), new RunResult(0, 0, 0, 0), 1);
            // 0-floor run deposits nothing, so seed coins via a second small completing run.
            s.EndEndlessRun(new DistrictInfo("b", 1, 1, rewardCoins: 1000, rewardGems: 0), new RunResult(2, 5, 0, 0), 2);
            Assert.That(s.TryBuyUpgrade(UpgradeKind.Magnet), Is.True);
            Assert.That(s.TryBuyUpgrade(UpgradeKind.Magnet), Is.True); // Magnet → level 2
            Assert.That(s.Upgrades.MagnetLevel, Is.EqualTo(2));

            int before = s.Coins;
            s.Prestige();
            Assert.That(s.Upgrades.MagnetLevel, Is.Zero); // upgrades reset
            Assert.That(s.Coins, Is.EqualTo((int)(before * 0.5f / 10f) * 10)); // partial retain
        }
    }
}
