using System;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Gameplay
{
    public class TowerRunTests
    {
        const float Tol = 1e-4f;

        // Places a block at an offset chosen to land the desired grade (Perfect snap / Good catch / Miss).
        static DropOutcome Place(TowerRun run, FloorType type, Grade desired)
        {
            float w = run.CurrentTopWidth;
            float offset = desired switch
            {
                Grade.Perfect => 0f,       // r=0   → Perfect (snap zone)
                Grade.Good => 0.50f * w,   // r=0.50 → Good (caught, overhangs)
                _ => 0.95f * w,            // r=0.95 → Miss (bounces off)
            };
            return run.PlaceBlock(type, offset);
        }

        [Test]
        public void NewRun_StartsAtInitialWidth()
        {
            var run = new TowerRun(new CoreConfig());
            Assert.That(run.CurrentTopWidth, Is.EqualTo(2.0f).Within(Tol));
            Assert.That(run.IsOver, Is.False);
            Assert.That(run.Score, Is.Zero);
        }

        [Test]
        public void Perfect_IncrementsChain_Scores()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Standard, 0f);
            Assert.That(o.Grade, Is.EqualTo(Grade.Perfect));
            Assert.That(run.PerfectChain, Is.EqualTo(1));
            Assert.That(o.ScoreGained, Is.EqualTo(100 * 2 + 50)); // base×2 + chain(1)
            Assert.That(o.ResidentsAdded, Is.EqualTo(4));         // Standard 2 + perfect 1 + combo L1 (+1)
            Assert.That(run.FloorCount, Is.EqualTo(1));
        }

        [Test]
        public void Good_Caught_Overhangs_NoStrike()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Standard, 0.60f); // r=0.30 → Good
            Assert.That(o.Grade, Is.EqualTo(Grade.Good));
            Assert.That(o.FloorPlaced, Is.True);
            Assert.That(run.LeanOffset, Is.EqualTo(0.60f * 0.15f).Within(Tol)); // overhang → lean
            Assert.That(o.ScoreGained, Is.EqualTo(100));
            Assert.That(run.MissStrikes, Is.Zero);
            Assert.That(run.PerfectChain, Is.Zero);
        }

        [Test]
        public void NearCenter_IsPerfect_GenerousSnapZone()
        {
            var run = new TowerRun(new CoreConfig());
            // up to 15% of width (0.30 m) still counts Perfect (the magnet/snap zone)
            Assert.That(run.PlaceBlock(FloorType.Standard, 0.29f).Grade, Is.EqualTo(Grade.Perfect));
        }

        [Test]
        public void Miss_OnlyWhenOverlapTiny_BouncesStrikes()
        {
            var run = new TowerRun(new CoreConfig());
            // ~80% offset still catches (Good); beyond that it misses
            Assert.That(run.PlaceBlock(FloorType.Standard, 1.50f).Grade, Is.EqualTo(Grade.Good));
            var o = run.PlaceBlock(FloorType.Premium, 1.90f); // r=0.95 → Miss
            Assert.That(o.Grade, Is.EqualTo(Grade.Miss));
            Assert.That(o.FloorPlaced, Is.False);
            Assert.That(o.ScoreGained, Is.Zero);
            Assert.That(run.MissStrikes, Is.EqualTo(1));
        }

        [Test]
        public void TwoCumulativeMisses_Topple()
        {
            var run = new TowerRun(new CoreConfig());
            Place(run, FloorType.Standard, Grade.Miss);
            Assert.That(run.IsOver, Is.False);
            var second = Place(run, FloorType.Standard, Grade.Miss);
            Assert.That(second.Toppled, Is.True);
            Assert.That(run.IsOver, Is.True);
        }

        [Test]
        public void Perfect_CorrectsLeanBy25Percent()
        {
            var run = new TowerRun(new CoreConfig());
            run.PlaceBlock(FloorType.Standard, 0.60f); // Good → lean += 0.60 × 0.15 = 0.09
            Assert.That(run.LeanOffset, Is.EqualTo(0.09f).Within(Tol));
            run.PlaceBlock(FloorType.Standard, 0f);    // Perfect → lean ×= 0.75
            Assert.That(run.LeanOffset, Is.EqualTo(0.0675f).Within(Tol));
        }

        [Test]
        public void TopWidth_StaysConstant_NoSlicing()
        {
            var run = new TowerRun(new CoreConfig());
            for (int i = 0; i < 40; i++)
            {
                Place(run, FloorType.Standard, Grade.Good);
                Assert.That(run.CurrentTopWidth, Is.EqualTo(2.0f).Within(Tol));
            }
        }

        [Test]
        public void PlaceAfterOver_Throws()
        {
            var run = new TowerRun(new CoreConfig());
            Place(run, FloorType.Standard, Grade.Miss);
            Place(run, FloorType.Standard, Grade.Miss); // 2nd strike → over
            Assert.That(() => run.PlaceBlock(FloorType.Standard, 0f),
                Throws.TypeOf<InvalidOperationException>());
        }

        // 10 Standard floors — P,P,P,P,P,G,G,G,Miss,P. The Miss bounces (scores nothing, costs a strike).
        [Test]
        public void WorkedExample_Scoring()
        {
            var run = new TowerRun(new CoreConfig());
            Grade[] script =
            {
                Grade.Perfect, Grade.Perfect, Grade.Perfect, Grade.Perfect, Grade.Perfect,
                Grade.Good, Grade.Good, Grade.Good, Grade.Miss, Grade.Perfect,
            };
            foreach (var g in script) Place(run, FloorType.Standard, g);

            Assert.That(run.Score, Is.EqualTo(2100), "floor scores (Miss F9 bounced) — combo doesn't change score");
            // Residents include the live combo bonus. Combo (Good holds, Miss resets): 1,2,3,4,5,5,5,5,(miss 0),1.
            // Per placed floor (base2 +perfect1 only on P +combo bonus): 4,5,6,8,11,10,10,10,(–),4 = 68.
            Assert.That(run.TotalResidents, Is.EqualTo(68));
            Assert.That(run.RunScore, Is.EqualTo(2780), "2100 + residents 680");
            Assert.That(run.MissStrikes, Is.EqualTo(1));
            Assert.That(run.FloorCount, Is.EqualTo(9)); // F1-8 + F10 (F9 bounced)
            Assert.That(run.IsOver, Is.False);
            Assert.That(run.PerfectChain, Is.EqualTo(1));
        }

        [Test]
        public void PerfectBalcony_ScoresAndResidents()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Balcony, 0f);
            Assert.That(o.Grade, Is.EqualTo(Grade.Perfect));
            Assert.That(o.ScoreGained, Is.EqualTo(150 * 2 + 50)); // base 150 ×2 + chain(1)
            Assert.That(o.ResidentsAdded, Is.EqualTo(6));          // Balcony 3 + perfect 2 + combo L1 (+1)
            Assert.That(run.RunScore, Is.EqualTo(350 + 6 * 10));
        }

        [Test]
        public void PerfectPremium_ScoresAndResidents()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Premium, 0f);
            Assert.That(o.ScoreGained, Is.EqualTo(200 * 2 + 50)); // base 200 ×2 + chain(1)
            Assert.That(o.ResidentsAdded, Is.EqualTo(9));          // Premium 5 + perfect 3 + combo L1 (+1)
            Assert.That(run.RunScore, Is.EqualTo(450 + 9 * 10));
        }

        [Test]
        public void NegativeOffset_AccumulatesNegativeLean()
        {
            var run = new TowerRun(new CoreConfig());
            run.PlaceBlock(FloorType.Standard, -0.60f); // Good → lean += -0.60 × 0.15
            Assert.That(run.LeanOffset, Is.EqualTo(-0.09f).Within(Tol));
            run.PlaceBlock(FloorType.Standard, 0f);     // Perfect → lean ×= 0.75
            Assert.That(run.LeanOffset, Is.EqualTo(-0.0675f).Within(Tol));
        }

        [Test]
        public void ToppleDrop_Bounces_ScoresNothing()
        {
            var run = new TowerRun(new CoreConfig());
            Place(run, FloorType.Standard, Grade.Miss);             // strike 1, bounces
            var second = Place(run, FloorType.Standard, Grade.Miss); // strike 2 → topple, bounces
            Assert.That(second.Toppled, Is.True);
            Assert.That(second.ScoreGained, Is.Zero);
            Assert.That(run.Score, Is.Zero);
        }

        [Test]
        public void Miss_ResetsPerfectChain()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 }); // avoid topple
            run.PlaceBlock(FloorType.Standard, 0f);
            run.PlaceBlock(FloorType.Standard, 0f);
            run.PlaceBlock(FloorType.Standard, 0f);
            Assert.That(run.PerfectChain, Is.EqualTo(3));
            run.PlaceBlock(FloorType.Standard, 1.95f); // Miss
            Assert.That(run.PerfectChain, Is.Zero);
        }

        [Test]
        public void ChainBonus_Tier4_AppliesAtChain11()
        {
            var run = new TowerRun(new CoreConfig());
            for (int i = 0; i < 10; i++) run.PlaceBlock(FloorType.Standard, 0f); // chain → 10
            Assert.That(run.PerfectChain, Is.EqualTo(10));
            var o = run.PlaceBlock(FloorType.Standard, 0f); // chain → 11
            Assert.That(run.PerfectChain, Is.EqualTo(11));
            Assert.That(o.ScoreGained, Is.EqualTo(100 * 2 + 600)); // base ×2 + tier-4 chain bonus
        }

        // ---------- Phase A: combo → residents (Tower-Bloxx) ----------

        [Test] // E1
        public void Combo_RisesOnPerfects_CapsAtFive()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Assert.That(Place(run, FloorType.Standard, Grade.Perfect).ComboLevel, Is.EqualTo(1));
            Place(run, FloorType.Standard, Grade.Perfect); // 2
            Place(run, FloorType.Standard, Grade.Perfect); // 3
            Place(run, FloorType.Standard, Grade.Perfect); // 4
            Assert.That(Place(run, FloorType.Standard, Grade.Perfect).ComboLevel, Is.EqualTo(5));
            Place(run, FloorType.Standard, Grade.Perfect); // would be 6
            Assert.That(Place(run, FloorType.Standard, Grade.Perfect).ComboLevel, Is.EqualTo(5)); // capped
        }

        [Test] // E2
        public void Combo_GoodHoldsLevel()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            for (int i = 0; i < 3; i++) Place(run, FloorType.Standard, Grade.Perfect); // ComboLevel → 3
            Assert.That(run.ComboLevel, Is.EqualTo(3));
            var o = Place(run, FloorType.Standard, Grade.Good);
            Assert.That(o.ComboLevel, Is.EqualTo(3));    // a Good HOLDS the level (only a strike breaks it)
            Assert.That(run.PerfectChain, Is.Zero);      // chain still resets on a Good
            Assert.That(run.MissStrikes, Is.Zero);
        }

        // (E4 — a Sloppy grade is unreachable via PlaceBlock under the default config: Grading collapses
        //  Sloppy into Good, so the Sloppy combo branch mirrors Miss and can't be exercised here. Miss = E5.)

        [Test] // E3
        public void Combo_GoodAtZero_StaysZero_NoBonus()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            var o = Place(run, FloorType.Standard, Grade.Good);
            Assert.That(o.ComboLevel, Is.Zero);
            Assert.That(o.ResidentsAdded, Is.EqualTo(2)); // base 2 + 0 perfect + 0 combo
        }

        [Test] // E5
        public void Combo_Miss_ResetsToZero()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            for (int i = 0; i < 3; i++) Place(run, FloorType.Standard, Grade.Perfect);
            Assert.That(run.ComboLevel, Is.EqualTo(3));
            var o = Place(run, FloorType.Standard, Grade.Miss);
            Assert.That(o.ComboLevel, Is.Zero);
            Assert.That(o.FloorPlaced, Is.False);
            Assert.That(o.ResidentsAdded, Is.Zero);
        }

        [Test] // E6 — the combo bonus is paid on a Good, not only on Perfects (Good now HOLDS the level)
        public void Combo_BonusApplies_OnGood()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            for (int i = 0; i < 3; i++) Place(run, FloorType.Standard, Grade.Perfect); // → level 3
            var o = Place(run, FloorType.Standard, Grade.Good);                         // holds level 3
            Assert.That(o.ComboLevel, Is.EqualTo(3));
            Assert.That(o.ResidentsAdded, Is.EqualTo(2 + 3)); // base 2 + combo L3 (+3), no perfect bonus
        }

        [Test] // E7 — combo bonus stacks with the per-type Perfect bonus
        public void Combo_BonusStacks_OnPerfect_Premium()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Place(run, FloorType.Premium, Grade.Perfect);  // L1: 5+3+1 = 9
            Place(run, FloorType.Premium, Grade.Perfect);  // L2: 5+3+2 = 10
            var o = Place(run, FloorType.Premium, Grade.Perfect); // L3: 5+3+3 = 11
            Assert.That(o.ComboLevel, Is.EqualTo(3));
            Assert.That(o.ResidentsAdded, Is.EqualTo(11));
        }

        [Test] // E8 — combo holds through Goods; only PerfectChain resets on a Good
        public void Combo_Interleaved_IndependentOfPerfectChain()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Grade[] script = { Grade.Perfect, Grade.Good, Grade.Perfect, Grade.Good, Grade.Perfect };
            int[] expectedCombo = { 1, 1, 2, 2, 3 }; // Good holds, Perfect raises
            for (int i = 0; i < script.Length; i++)
                Assert.That(Place(run, FloorType.Standard, script[i]).ComboLevel, Is.EqualTo(expectedCombo[i]), $"drop {i}");
            Assert.That(run.PerfectChain, Is.EqualTo(1));
            Assert.That(run.TotalResidents, Is.EqualTo(22)); // 4+3+5+4+6
        }

        [Test] // E9
        public void Combo_DropOutcome_CarriesPostTransitionLevel()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Assert.That(Place(run, FloorType.Standard, Grade.Perfect).ComboLevel, Is.EqualTo(1));
            Assert.That(Place(run, FloorType.Standard, Grade.Good).ComboLevel, Is.EqualTo(1)); // a Good holds
        }

        [Test] // E10 — worked example with combo
        public void Combo_WorkedExample_TotalResidents()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Grade[] script =
            {
                Grade.Perfect, Grade.Perfect, Grade.Perfect, Grade.Good, Grade.Perfect,
                Grade.Miss, Grade.Perfect, Grade.Good, Grade.Good, Grade.Perfect,
            };
            foreach (var g in script) Place(run, FloorType.Standard, g);
            // combo (Good holds, Miss resets): 1,2,3,3,4,(miss 0),1,1,1,2
            // residents: 4,5,6,5,8,(miss 0),4,3,3,5 = 43
            Assert.That(run.TotalResidents, Is.EqualTo(43));
            Assert.That(run.MaxComboLevel, Is.EqualTo(4));
        }

        [Test] // a Good HOLDS the combo (not decay), so the floor still earns the live combo bonus
        public void Combo_GoodHoldsLevel1_StillEarnsBonus()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Place(run, FloorType.Standard, Grade.Perfect);      // combo → 1
            var o = Place(run, FloorType.Standard, Grade.Good);  // holds at 1
            Assert.That(o.ComboLevel, Is.EqualTo(1));
            Assert.That(o.ResidentsAdded, Is.EqualTo(2 + 1));    // base 2 + combo[1] (1)
        }

        [Test] // QA gap: combo saturates at the cap across a long perfect run
        public void Combo_HeldAtCap_OverLongPerfectRun()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            for (int i = 0; i < 10; i++) Place(run, FloorType.Standard, Grade.Perfect);
            Assert.That(run.ComboLevel, Is.EqualTo(5));
            Assert.That(run.MaxComboLevel, Is.EqualTo(5));
        }

        [Test] // QA gap: the peak must not fall when the live combo decays/breaks
        public void MaxComboLevel_DoesNotDecay_OnGoodOrMiss()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            for (int i = 0; i < 3; i++) Place(run, FloorType.Standard, Grade.Perfect); // → 3
            Assert.That(run.MaxComboLevel, Is.EqualTo(3));
            Place(run, FloorType.Standard, Grade.Good); // combo holds 3
            Place(run, FloorType.Standard, Grade.Miss); // combo → 0
            Assert.That(run.ComboLevel, Is.Zero);
            Assert.That(run.MaxComboLevel, Is.EqualTo(3)); // peak preserved
        }

        [Test] // QA gap: residents stay non-negative + monotonic over a long mixed run
        public void Combo_LongMixedRun_ResidentsMonotonicNonNegative()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Grade[] cycle = { Grade.Perfect, Grade.Good, Grade.Miss, Grade.Perfect, Grade.Perfect };
            int prev = 0;
            for (int i = 0; i < 200; i++)
            {
                Place(run, FloorType.Standard, cycle[i % cycle.Length]);
                Assert.That(run.TotalResidents, Is.GreaterThanOrEqualTo(prev)); // never decreases
                prev = run.TotalResidents;
            }
            Assert.That(run.TotalResidents, Is.GreaterThan(0));
        }

        [Test]
        public void Combo_Ctor_ThrowsOnShortBonusTable()
        {
            var bad = new CoreConfig { ComboResidentBonus = new[] { 0, 1 }, ComboLevelCap = 3 };
            Assert.That(() => new TowerRun(bad), Throws.TypeOf<ArgumentException>());
        }

        // ---------- Phase C: earned specialty blocks ----------

        static TowerRun Perfects(int n)
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 }); // high limit: a stray strike won't topple
            for (int i = 0; i < n; i++) run.PlaceBlock(FloorType.Standard, 0f); // Perfect
            return run;
        }

        [Test] // SC-01: 4 perfects → Balcony pending; resolved + consumed
        public void Upgrade_Tier1_AtChain4()
        {
            var run = Perfects(4);
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.Balcony));
            Assert.That(run.NextSpawnType(FloorType.Standard), Is.EqualTo(FloorType.Balcony));
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.None)); // consumed
        }

        [Test] // SC-02: 8 perfects → Premium pending (rose from Balcony)
        public void Upgrade_Tier2_AtChain8()
        {
            var run = Perfects(8);
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.Premium));
            Assert.That(run.NextSpawnType(FloorType.Standard), Is.EqualTo(FloorType.Premium));
        }

        [Test] // SC-03: a Good keeps the pending upgrade (only the chain resets)
        public void Upgrade_SurvivesAGood()
        {
            var run = Perfects(4);
            run.PlaceBlock(FloorType.Standard, 0.50f * run.CurrentTopWidth); // Good
            Assert.That(run.PerfectChain, Is.Zero);
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.Balcony));
            Assert.That(run.NextSpawnType(FloorType.Standard), Is.EqualTo(FloorType.Balcony));
        }

        [Test] // SC-04: a strike cancels the pending upgrade
        public void Upgrade_CancelledByMiss()
        {
            var run = Perfects(4);
            run.PlaceBlock(FloorType.Premium, 0.95f * run.CurrentTopWidth); // Miss
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.None));
            Assert.That(run.NextSpawnType(FloorType.Standard), Is.EqualTo(FloorType.Standard));
        }

        [Test] // SC-05: upgrade only RAISES — never downgrades a better seeded type
        public void Upgrade_NeverDowngrades_SeededPremium()
        {
            var run = Perfects(4); // pending Balcony
            Assert.That(run.NextSpawnType(FloorType.Premium), Is.EqualTo(FloorType.Premium));
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.None)); // still consumed
        }

        [Test] // SC-07: chain 12 → still Premium pending
        public void Upgrade_RepeatingPremium_AtChain12()
        {
            var run = Perfects(12);
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.Premium));
        }

        [Test] // SC-10: no upgrade between milestones (chain 3, and chain 5 doesn't re-trigger)
        public void Upgrade_OnlyAtMilestones()
        {
            var run = Perfects(3);
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.None));
            run.PlaceBlock(FloorType.Standard, 0f);            // chain 4 → Balcony
            run.NextSpawnType(FloorType.Standard);             // consume
            run.PlaceBlock(FloorType.Standard, 0f);            // chain 5 → no new trigger
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.None));
        }

        [Test] // SC-11: deterministic — identical perfect sequences resolve identically
        public void Upgrade_Deterministic()
        {
            var a = Perfects(8);
            var b = Perfects(8);
            Assert.That(a.NextSpawnType(FloorType.Standard), Is.EqualTo(b.NextSpawnType(FloorType.Standard)));
        }

        [Test] // SC-08/09: trophy-roof bonus by longest chain, folded into residents
        public void TrophyRoof_ByMaxChain()
        {
            Assert.That(Perfects(3).TrophyRoofResidents, Is.Zero);  // below first threshold
            var run = Perfects(4);
            Assert.That(run.TrophyRoofResidents, Is.EqualTo(8));
            var r = RunResult.From(run);
            Assert.That(r.TrophyRoofResidents, Is.EqualTo(8));
            Assert.That(r.TotalResidents, Is.EqualTo(run.TotalResidents + 8)); // folded into population
        }

        [Test] // M1: earn Balcony at 4, consume it, then earn Premium 4 drops later (chain keeps counting)
        public void Upgrade_Consume_ThenEarnPremium_AtChain8()
        {
            var run = Perfects(4);
            run.NextSpawnType(FloorType.Standard); // consume the Balcony
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.None));
            for (int i = 0; i < 4; i++) run.PlaceBlock(FloorType.Standard, 0f); // chain → 8
            Assert.That(run.PerfectChain, Is.EqualTo(8));
            Assert.That(run.NextSpawnType(FloorType.Standard), Is.EqualTo(FloorType.Premium));
        }

        [Test] // M2: no pending → NextSpawnType is a passthrough for every type
        public void Upgrade_NoPending_NextSpawnType_Passthrough()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Assert.That(run.NextSpawnType(FloorType.Standard), Is.EqualTo(FloorType.Standard));
            Assert.That(run.NextSpawnType(FloorType.Balcony), Is.EqualTo(FloorType.Balcony));
            Assert.That(run.NextSpawnType(FloorType.Premium), Is.EqualTo(FloorType.Premium));
        }

        [Test] // M3: trophy at chain 8 → 20 residents folded once; RunScore unchanged
        public void TrophyRoof_Chain8_FoldedIntoRunResult()
        {
            var run = Perfects(8);
            Assert.That(run.TrophyRoofResidents, Is.EqualTo(20));
            var r = RunResult.From(run);
            Assert.That(r.TotalResidents, Is.EqualTo(run.TotalResidents + 20));
            Assert.That(r.RunScore, Is.EqualTo(run.RunScore)); // trophy is population, not leaderboard score
        }

        [Test] // M4: a run with no Perfects never arms an upgrade and earns no trophy
        public void Upgrade_NoPerfects_NeverArmed_NoTrophy()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            for (int i = 0; i < 6; i++) run.PlaceBlock(FloorType.Standard, 0.50f * run.CurrentTopWidth); // all Good
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.None));
            Assert.That(run.TrophyRoofResidents, Is.Zero);
            Assert.That(RunResult.From(run).TotalResidents, Is.EqualTo(run.TotalResidents));
        }

        [Test] // N1: the Max() resolve relies on FloorType and UpgradeTier sharing integer values
        public void Upgrade_EnumValues_AlignWithFloorType()
        {
            Assert.That((int)UpgradeTier.None, Is.EqualTo((int)FloorType.Standard));
            Assert.That((int)UpgradeTier.Balcony, Is.EqualTo((int)FloorType.Balcony));
            Assert.That((int)UpgradeTier.Premium, Is.EqualTo((int)FloorType.Premium));
        }

        [Test] // N2: upgrade-only is idempotent — Balcony pending on a seeded Balcony stays Balcony
        public void Upgrade_BalconyOnBalcony_Idempotent()
        {
            var run = Perfects(4);
            Assert.That(run.NextSpawnType(FloorType.Balcony), Is.EqualTo(FloorType.Balcony));
            Assert.That(run.PendingUpgrade, Is.EqualTo(UpgradeTier.None));
        }
    }
}
