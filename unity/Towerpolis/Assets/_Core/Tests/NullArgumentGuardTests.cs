using System;
using System.Collections.Generic;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests
{
    /// <summary>Contract guards: every public Core entry point documented to reject a null argument must
    /// throw <see cref="ArgumentNullException"/> — never an NRE or silent garbage. Covers the throw-side
    /// branch of each guard and acts as a small "don't crash on bad input" robustness net.</summary>
    public class NullArgumentGuardTests
    {
        static readonly RunResult AnyResult = new RunResult(1, 1, 1, 0);

        [Test]
        public void Grading_NullCfg_Throws()
            => Assert.Throws<ArgumentNullException>(() => Grading.Evaluate(null!, 0f, 1f));

        [Test]
        public void Scoring_NullCfg_EveryMethodThrows()
        {
            Assert.Throws<ArgumentNullException>(() => Scoring.BaseScore(null!, FloorType.Standard));
            Assert.Throws<ArgumentNullException>(() => Scoring.GradeMultiplier(null!, Grade.Perfect));
            Assert.Throws<ArgumentNullException>(() => Scoring.ChainBonus(null!, 1));
            Assert.Throws<ArgumentNullException>(() => Scoring.BaseResidents(null!, FloorType.Standard));
            Assert.Throws<ArgumentNullException>(() => Scoring.ComboResidentBonus(null!, 0));
            Assert.Throws<ArgumentNullException>(() => Scoring.PerfectResidentBonus(null!, FloorType.Standard));
            Assert.Throws<ArgumentNullException>(() => Scoring.TrophyRoofBonus(null!, 4));
            Assert.Throws<ArgumentNullException>(() => Scoring.FloorScore(null!, FloorType.Standard, Grade.Perfect, 1));
        }

        [Test]
        public void TowerRun_NullCfg_Throws()
            => Assert.Throws<ArgumentNullException>(() => new TowerRun(null!));

        [Test]
        public void CityState_NullCfg_Throws()
            => Assert.Throws<ArgumentNullException>(() => new CityState(null!));

        [Test]
        public void RunResult_FromNull_Throws()
            => Assert.Throws<ArgumentNullException>(() => RunResult.From(null!));

        [Test]
        public void CoinEarnCalculator_NullCfg_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => CoinEarnCalculator.RunCoins(in AnyResult, null!));
            Assert.Throws<ArgumentNullException>(() => CoinEarnCalculator.CityBonusedReward(100, 0, null!));
        }

        [Test]
        public void DailyStreak_MilestoneCoins_NullCfg_Throws()
            => Assert.Throws<ArgumentNullException>(() => DailyStreak.MilestoneCoins(7, null!));

        [Test]
        public void LoginCalendar_Claim_NullCfg_Throws()
            => Assert.Throws<ArgumentNullException>(() => LoginCalendar.Claim(LoginCalendarState.Empty, "2026-06-03", null!));

        [Test]
        public void UpgradeService_NullArgs_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => UpgradeService.TryPurchase("id", 0, null!, 3, 100));
            Assert.Throws<ArgumentNullException>(() => UpgradeService.GetMagnetFraction(0, null!, isDaily: false));
            Assert.Throws<ArgumentNullException>(() => UpgradeService.GetSlowMoFactor(0, null!, isDaily: false));
        }

        [Test]
        public void MissionTracker_NullArgs_Throw()
        {
            var evt = new MissionEvent(1, 1, 1, 1, false, "downtown", 0);
            Assert.Throws<ArgumentNullException>(() => MissionTracker.Record(in evt, null!, new Dictionary<string, int>()));
            Assert.Throws<ArgumentNullException>(() => MissionTracker.DrawWeeklyMissions(null!, 1UL));
        }

        [Test]
        public void AchievementEvaluator_NullDefs_Throws()
        {
            var snap = new AchievementSnapshot(0, 0, 0, 0, 0, 0);
            Assert.Throws<ArgumentNullException>(() => AchievementEvaluator.Evaluate(in snap, null!, Array.Empty<string>()));
        }
    }
}
