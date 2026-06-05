using System;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;

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
            // Residents now include the live combo bonus. Combo levels: 1,2,3,3,3,2,1,0,(miss 0),1.
            // Per placed floor (base2 +perfect1 only on P +combo): 4,5,7,7,7,4,3,2,(–),4 = 43.
            Assert.That(run.TotalResidents, Is.EqualTo(43));
            Assert.That(run.RunScore, Is.EqualTo(2530), "2100 + residents 430");
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
        public void Combo_RisesOnPerfects_CapsAtThree()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Assert.That(Place(run, FloorType.Standard, Grade.Perfect).ComboLevel, Is.EqualTo(1));
            Place(run, FloorType.Standard, Grade.Perfect); // 2
            Assert.That(Place(run, FloorType.Standard, Grade.Perfect).ComboLevel, Is.EqualTo(3));
            Place(run, FloorType.Standard, Grade.Perfect); // would be 4
            Assert.That(Place(run, FloorType.Standard, Grade.Perfect).ComboLevel, Is.EqualTo(3)); // capped
        }

        [Test] // E2
        public void Combo_GoodDecaysOneTier()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            for (int i = 0; i < 3; i++) Place(run, FloorType.Standard, Grade.Perfect); // ComboLevel → 3
            Assert.That(run.ComboLevel, Is.EqualTo(3));
            var o = Place(run, FloorType.Standard, Grade.Good);
            Assert.That(o.ComboLevel, Is.EqualTo(2));    // decayed one tier, not killed
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

        [Test] // E6 — the combo bonus is paid on a Good, not only on Perfects
        public void Combo_BonusApplies_OnGood()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            for (int i = 0; i < 3; i++) Place(run, FloorType.Standard, Grade.Perfect); // → level 3
            var o = Place(run, FloorType.Standard, Grade.Good);                         // → level 2
            Assert.That(o.ResidentsAdded, Is.EqualTo(2 + 2)); // base 2 + combo L2 (+2), no perfect bonus
        }

        [Test] // E7 — combo bonus stacks with the per-type Perfect bonus
        public void Combo_BonusStacks_OnPerfect_Premium()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Place(run, FloorType.Premium, Grade.Perfect);  // L1: 5+3+1 = 9
            Place(run, FloorType.Premium, Grade.Perfect);  // L2: 5+3+2 = 10
            var o = Place(run, FloorType.Premium, Grade.Perfect); // L3: 5+3+4 = 12
            Assert.That(o.ComboLevel, Is.EqualTo(3));
            Assert.That(o.ResidentsAdded, Is.EqualTo(12));
        }

        [Test] // E8 — combo and PerfectChain are independent
        public void Combo_Interleaved_IndependentOfPerfectChain()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Grade[] script = { Grade.Perfect, Grade.Good, Grade.Perfect, Grade.Good, Grade.Perfect };
            int[] expectedCombo = { 1, 0, 1, 0, 1 };
            for (int i = 0; i < script.Length; i++)
                Assert.That(Place(run, FloorType.Standard, script[i]).ComboLevel, Is.EqualTo(expectedCombo[i]), $"drop {i}");
            Assert.That(run.PerfectChain, Is.EqualTo(1));
            Assert.That(run.TotalResidents, Is.EqualTo(16)); // 4+2+4+2+4
        }

        [Test] // E9
        public void Combo_DropOutcome_CarriesPostTransitionLevel()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Assert.That(Place(run, FloorType.Standard, Grade.Perfect).ComboLevel, Is.EqualTo(1));
            Assert.That(Place(run, FloorType.Standard, Grade.Good).ComboLevel, Is.EqualTo(0));
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
            // residents: 4,5,7,4,7,(miss 0),4,2,2,4 = 39
            Assert.That(run.TotalResidents, Is.EqualTo(39));
            Assert.That(run.MaxComboLevel, Is.EqualTo(3));
        }

        [Test] // QA gap: the combo bonus dies on the exact floor that decays it to 0
        public void Combo_GoodDecays1To0_NoBonus()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            Place(run, FloorType.Standard, Grade.Perfect);      // combo → 1
            var o = Place(run, FloorType.Standard, Grade.Good);  // combo 1 → 0
            Assert.That(o.ComboLevel, Is.Zero);
            Assert.That(o.ResidentsAdded, Is.EqualTo(2));        // base 2 + combo[0] (0)
        }

        [Test] // QA gap: combo saturates at the cap across a long perfect run
        public void Combo_HeldAtCap_OverLongPerfectRun()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            for (int i = 0; i < 10; i++) Place(run, FloorType.Standard, Grade.Perfect);
            Assert.That(run.ComboLevel, Is.EqualTo(3));
            Assert.That(run.MaxComboLevel, Is.EqualTo(3));
        }

        [Test] // QA gap: the peak must not fall when the live combo decays/breaks
        public void MaxComboLevel_DoesNotDecay_OnGoodOrMiss()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 });
            for (int i = 0; i < 3; i++) Place(run, FloorType.Standard, Grade.Perfect); // → 3
            Assert.That(run.MaxComboLevel, Is.EqualTo(3));
            Place(run, FloorType.Standard, Grade.Good); // combo → 2
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
    }
}
