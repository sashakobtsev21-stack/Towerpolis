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
            Assert.That(o.ResidentsAdded, Is.EqualTo(2));         // Standard = 2, flat (no perfect bonus)
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

            Assert.That(run.Score, Is.EqualTo(2100), "floor scores (Miss F9 bounced)");
            Assert.That(run.TotalResidents, Is.EqualTo(18)); // 9 Standard floors × 2 (flat)
            Assert.That(run.RunScore, Is.EqualTo(2280), "2100 + residents 180");
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
            Assert.That(o.ResidentsAdded, Is.EqualTo(3));          // Balcony = 3, flat
            Assert.That(run.RunScore, Is.EqualTo(350 + 3 * 10));
        }

        [Test]
        public void PerfectPremium_ScoresAndResidents()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Premium, 0f);
            Assert.That(o.ScoreGained, Is.EqualTo(200 * 2 + 50)); // base 200 ×2 + chain(1)
            Assert.That(o.ResidentsAdded, Is.EqualTo(5));          // Premium = 5, flat
            Assert.That(run.RunScore, Is.EqualTo(450 + 5 * 10));
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
    }
}
