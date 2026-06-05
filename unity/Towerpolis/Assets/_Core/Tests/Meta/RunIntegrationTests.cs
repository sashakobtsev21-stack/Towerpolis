using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    /// <summary>End-to-end across the run → result → city seam: a scripted run is banked and we assert the
    /// whole outcome (coins, population, district reward) lines up with the per-system formulas, plus the
    /// daily-seed determinism invariant, a balance guardrail, and long-run invariants. These guard the
    /// integration (not just each unit) against future balance/tuning edits.</summary>
    public class RunIntegrationTests
    {
        static void Place(TowerRun run, FloorType t, Grade g)
        {
            float w = run.CurrentTopWidth;
            float off = g switch { Grade.Perfect => 0f, Grade.Good => 0.5f * w, _ => 0.95f * w };
            run.PlaceBlock(t, off);
        }

        static TowerRun ScriptedRun(CoreConfig cfg, params Grade[] script)
        {
            var run = new TowerRun(cfg);
            foreach (var g in script) Place(run, FloorType.Standard, g);
            return run;
        }

        [Test] // a finished run banks the documented coins + deposits its residents into the city
        public void RunBanks_CoinsAndPopulation_EndToEnd()
        {
            var cfg = new CoreConfig { StrikeLimit = 99 };
            RunResult r = RunResult.From(ScriptedRun(cfg, Grade.Perfect, Grade.Perfect, Grade.Good, Grade.Perfect, Grade.Good));

            var city = new CityState(cfg);
            var d = new DistrictInfo("test", 20, 100000, 200, 0); // huge fill goal → no completion reward
            RunEndOutcome o = city.EndEndlessRun(d, r, timestampUtcTicks: 1);

            int expectedCoins = r.FloorCount * cfg.CoinPerFloor + r.PerfectDrops * cfg.CoinBonusPerfect;
            Assert.That(o.CoinsEarned, Is.EqualTo(expectedCoins));
            Assert.That(city.Coins, Is.EqualTo(expectedCoins));
            Assert.That(city.TotalPopulation, Is.EqualTo(r.TotalResidents));
            Assert.That(o.DistrictCompletedNow, Is.False);
        }

        [Test] // completing a district pays its reward (+gems) once, on top of run coins
        public void DistrictCompletion_PaysRewardOnce()
        {
            var cfg = new CoreConfig { StrikeLimit = 99 };
            RunResult r = RunResult.From(ScriptedRun(cfg, Grade.Perfect, Grade.Perfect, Grade.Perfect));
            var city = new CityState(cfg);
            var d = new DistrictInfo("mini", 20, 1, 500, 2); // fill goal 1 → completes on the first deposit

            RunEndOutcome first = city.EndEndlessRun(d, r, 1);
            Assert.That(first.DistrictCompletedNow, Is.True);
            Assert.That(first.GemsEarned, Is.EqualTo(2));
            int runCoins = r.FloorCount * cfg.CoinPerFloor + r.PerfectDrops * cfg.CoinBonusPerfect;
            Assert.That(first.CoinsEarned, Is.GreaterThan(runCoins), "should include the district reward");

            RunEndOutcome second = city.EndEndlessRun(d, RunResult.From(ScriptedRun(cfg, Grade.Good)), 2);
            Assert.That(second.DistrictCompletedNow, Is.False, "reward must pay only once");
        }

        [Test] // identical inputs → identical RunResult (the daily-seed fairness invariant)
        public void Deterministic_SameScript_SameResult()
        {
            var cfg = new CoreConfig { StrikeLimit = 99 };
            Grade[] script =
            {
                Grade.Perfect, Grade.Good, Grade.Perfect, Grade.Perfect, Grade.Miss,
                Grade.Perfect, Grade.Good, Grade.Perfect, Grade.Perfect, Grade.Perfect,
            };
            RunResult a = RunResult.From(ScriptedRun(cfg, script));
            RunResult b = RunResult.From(ScriptedRun(cfg, script));
            Assert.That(a.FloorCount, Is.EqualTo(b.FloorCount));
            Assert.That(a.TotalResidents, Is.EqualTo(b.TotalResidents));
            Assert.That(a.RunScore, Is.EqualTo(b.RunScore));
            Assert.That(a.PerfectDrops, Is.EqualTo(b.PerfectDrops));
            Assert.That(a.MaxPerfectChain, Is.EqualTo(b.MaxPerfectChain));
            Assert.That(a.TrophyRoofResidents, Is.EqualTo(b.TrophyRoofResidents));
        }

        [Test] // skill is clearly rewarded: an all-Perfect run houses far more than an all-Good run of equal height
        public void BalanceGuardrail_PerfectRunBeatsGoodRun()
        {
            var cfg = new CoreConfig { StrikeLimit = 99 };
            var perfect = new Grade[30];
            var good = new Grade[30];
            for (int i = 0; i < 30; i++) { perfect[i] = Grade.Perfect; good[i] = Grade.Good; }
            int pRes = RunResult.From(ScriptedRun(cfg, perfect)).TotalResidents;
            int gRes = RunResult.From(ScriptedRun(cfg, good)).TotalResidents;
            Assert.That(pRes, Is.GreaterThanOrEqualTo(2 * gRes), "perfect={0} good={1} — perfects must clearly out-earn", pRes, gRes);
        }

        [Test] // a long mixed run never throws; residents stay monotonic non-negative; score stays non-negative
        public void LongMixedRun_InvariantsHold()
        {
            var cfg = new CoreConfig { StrikeLimit = 999 }; // don't topple — we're stress-testing the math
            var run = new TowerRun(cfg);
            Grade[] cycle = { Grade.Perfect, Grade.Good, Grade.Perfect, Grade.Miss, Grade.Perfect };
            int prev = 0;
            for (int i = 0; i < 300; i++)
            {
                Place(run, FloorType.Standard, cycle[i % cycle.Length]);
                Assert.That(run.TotalResidents, Is.GreaterThanOrEqualTo(prev));
                prev = run.TotalResidents;
            }
            Assert.That(run.TotalResidents, Is.GreaterThan(0));
            Assert.That(run.Score, Is.GreaterThanOrEqualTo(0));
            Assert.That(run.PerfectChain, Is.LessThanOrEqualTo(run.MaxPerfectChain));
        }
    }
}
