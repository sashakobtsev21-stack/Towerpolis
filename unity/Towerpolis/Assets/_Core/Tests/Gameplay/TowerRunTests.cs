using System;
using NUnit.Framework;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Tests.Gameplay
{
    public class TowerRunTests
    {
        const float Tol = 1e-4f;

        // Places a block at an offset chosen (from the current top width) to land the desired grade.
        static DropOutcome Place(TowerRun run, FloorType type, Grade desired)
        {
            float w = run.CurrentTopWidth;
            float offset = desired switch
            {
                Grade.Perfect => 0f,
                Grade.Good => 0.20f * w,
                Grade.Sloppy => 0.40f * w,
                _ => 0.70f * w, // Miss
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
        public void Perfect_KeepsWidth_IncrementsChain_Scores()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Standard, 0f);
            Assert.That(o.Grade, Is.EqualTo(Grade.Perfect));
            Assert.That(run.CurrentTopWidth, Is.EqualTo(2.0f).Within(Tol)); // no slice
            Assert.That(run.PerfectChain, Is.EqualTo(1));
            Assert.That(o.ScoreGained, Is.EqualTo(100 * 2 + 50)); // base×2 + chain(1)
            Assert.That(o.ResidentsAdded, Is.EqualTo(3));         // 2 + perfect bonus 1
            Assert.That(run.FloorCount, Is.EqualTo(1));
        }

        [Test]
        public void Good_SlicesWidth_NoStrike()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Standard, 0.40f); // r=0.20 → Good
            Assert.That(o.Grade, Is.EqualTo(Grade.Good));
            Assert.That(run.CurrentTopWidth, Is.EqualTo(1.6f).Within(Tol)); // 2.0 - 0.4
            Assert.That(o.ScoreGained, Is.EqualTo(100));
            Assert.That(run.MissStrikes, Is.Zero);
            Assert.That(run.PerfectChain, Is.Zero);
        }

        [Test]
        public void Sloppy_SlicesWidth_AddsStrike()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Standard, 0.80f); // r=0.40 → Sloppy
            Assert.That(o.Grade, Is.EqualTo(Grade.Sloppy));
            Assert.That(run.CurrentTopWidth, Is.EqualTo(1.2f).Within(Tol)); // 2.0 - 0.8
            Assert.That(o.ScoreGained, Is.EqualTo(50));                     // 100 × 0.5
            Assert.That(run.MissStrikes, Is.EqualTo(1));
        }

        [Test]
        public void Miss_PlacesNothing_StrikesButKeepsWidth()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Premium, 1.50f); // r=0.75 → Miss
            Assert.That(o.Grade, Is.EqualTo(Grade.Miss));
            Assert.That(o.FloorPlaced, Is.False);
            Assert.That(o.ScoreGained, Is.Zero);
            Assert.That(o.ResidentsAdded, Is.Zero);
            Assert.That(run.CurrentTopWidth, Is.EqualTo(2.0f).Within(Tol));
            Assert.That(run.MissStrikes, Is.EqualTo(1));
            Assert.That(run.FloorCount, Is.Zero);
        }

        [Test]
        public void TwoCumulativeStrikes_Topple()
        {
            var run = new TowerRun(new CoreConfig());
            Place(run, FloorType.Standard, Grade.Sloppy);
            Assert.That(run.IsOver, Is.False);
            var second = Place(run, FloorType.Standard, Grade.Sloppy);
            Assert.That(second.Toppled, Is.True);
            Assert.That(run.IsOver, Is.True);
        }

        [Test]
        public void SloppyThenMiss_Topples()
        {
            var run = new TowerRun(new CoreConfig());
            Place(run, FloorType.Standard, Grade.Sloppy);
            var miss = Place(run, FloorType.Standard, Grade.Miss);
            Assert.That(miss.Toppled, Is.True);
            Assert.That(run.IsOver, Is.True);
        }

        [Test]
        public void SloppyCostsStrikeDisabled_NeverStrikesOnSloppy()
        {
            var cfg = new CoreConfig { SloppyCostsStrike = false };
            var run = new TowerRun(cfg);
            for (int i = 0; i < 10; i++) Place(run, FloorType.Standard, Grade.Sloppy);
            Assert.That(run.MissStrikes, Is.Zero);
            Assert.That(run.IsOver, Is.False);
            // scoring/residents are independent of the strike flag
            Assert.That(run.Score, Is.GreaterThan(0));
            Assert.That(run.TotalResidents, Is.GreaterThan(0));
        }

        [Test]
        public void Perfect_CorrectsLeanBy25Percent()
        {
            var run = new TowerRun(new CoreConfig());
            run.PlaceBlock(FloorType.Standard, 0.40f); // Good → lean += 0.40 × 0.15 = 0.06
            Assert.That(run.LeanOffset, Is.EqualTo(0.06f).Within(Tol));
            run.PlaceBlock(FloorType.Standard, 0f);    // Perfect → lean ×= 0.75
            Assert.That(run.LeanOffset, Is.EqualTo(0.045f).Within(Tol));
        }

        [Test]
        public void SliceNeverGoesBelowMinBlockWidth()
        {
            var cfg = new CoreConfig { SloppyCostsStrike = false, StrikeLimit = 999 };
            var run = new TowerRun(cfg);
            for (int i = 0; i < 40; i++)
            {
                Place(run, FloorType.Standard, Grade.Sloppy);
                Assert.That(run.CurrentTopWidth, Is.GreaterThanOrEqualTo(cfg.MinBlockWidth - Tol));
            }
            Assert.That(run.CurrentTopWidth, Is.EqualTo(cfg.MinBlockWidth).Within(Tol));
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

        // Spec §6.4 worked example: 10 Standard floors — P,P,P,P,P,G,G,G,Sloppy,P → 2410 total.
        [Test]
        public void WorkedExample_Section64_Scores2410_With26Residents()
        {
            var run = new TowerRun(new CoreConfig());
            Grade[] script =
            {
                Grade.Perfect, Grade.Perfect, Grade.Perfect, Grade.Perfect, Grade.Perfect,
                Grade.Good, Grade.Good, Grade.Good, Grade.Sloppy, Grade.Perfect,
            };
            foreach (var g in script) Place(run, FloorType.Standard, g);

            Assert.That(run.Score, Is.EqualTo(2150), "sum of floor scores");
            Assert.That(run.TotalResidents, Is.EqualTo(26));
            Assert.That(run.RunScore, Is.EqualTo(2410), "floor scores 2150 + residents 260");
            Assert.That(run.MissStrikes, Is.EqualTo(1)); // the single Sloppy
            Assert.That(run.IsOver, Is.False);
            Assert.That(run.PerfectChain, Is.EqualTo(1)); // last drop restarted the chain
        }

        [Test]
        public void PerfectBalcony_ScoresAndResidents()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Balcony, 0f);
            Assert.That(o.Grade, Is.EqualTo(Grade.Perfect));
            Assert.That(o.ScoreGained, Is.EqualTo(150 * 2 + 50)); // base 150 ×2 + chain(1)
            Assert.That(o.ResidentsAdded, Is.EqualTo(4));          // 3 + perfect bonus
            Assert.That(run.RunScore, Is.EqualTo(350 + 4 * 10));   // floor + resident bonus
        }

        [Test]
        public void PerfectPremium_ScoresAndResidents()
        {
            var run = new TowerRun(new CoreConfig());
            var o = run.PlaceBlock(FloorType.Premium, 0f);
            Assert.That(o.ScoreGained, Is.EqualTo(200 * 2 + 50)); // base 200 ×2 + chain(1)
            Assert.That(o.ResidentsAdded, Is.EqualTo(5));          // 4 + perfect bonus
            Assert.That(run.RunScore, Is.EqualTo(450 + 5 * 10));
        }

        [Test]
        public void NegativeOffset_AccumulatesNegativeLean()
        {
            var run = new TowerRun(new CoreConfig());
            run.PlaceBlock(FloorType.Standard, -0.40f); // Good → lean += -0.40 × 0.15
            Assert.That(run.LeanOffset, Is.EqualTo(-0.06f).Within(Tol));
            run.PlaceBlock(FloorType.Standard, 0f);     // Perfect → lean ×= 0.75
            Assert.That(run.LeanOffset, Is.EqualTo(-0.045f).Within(Tol));
        }

        [Test]
        public void ToppleDrop_StillScoresThatFloor()
        {
            var run = new TowerRun(new CoreConfig());
            Place(run, FloorType.Standard, Grade.Sloppy);          // strike 1, +50
            var second = Place(run, FloorType.Standard, Grade.Sloppy); // strike 2 → topple, +50
            Assert.That(second.Toppled, Is.True);
            Assert.That(second.ScoreGained, Is.EqualTo(50));
            Assert.That(run.Score, Is.EqualTo(100)); // both Sloppy floors are scored before topple
        }

        [Test]
        public void Miss_ResetsPerfectChain()
        {
            var run = new TowerRun(new CoreConfig { StrikeLimit = 99 }); // avoid topple
            run.PlaceBlock(FloorType.Standard, 0f);
            run.PlaceBlock(FloorType.Standard, 0f);
            run.PlaceBlock(FloorType.Standard, 0f);
            Assert.That(run.PerfectChain, Is.EqualTo(3));
            run.PlaceBlock(FloorType.Standard, 1.5f); // Miss
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
