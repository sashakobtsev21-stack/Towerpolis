using NUnit.Framework;
using Towerpolis.Core.Determinism;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Tests.Gameplay
{
    public class BlockSequenceTests
    {
        static string Encode(FloorType[] seq)
        {
            var c = new char[seq.Length];
            for (int i = 0; i < seq.Length; i++)
                c[i] = seq[i] switch
                {
                    FloorType.Standard => 'S',
                    FloorType.Balcony => 'B',
                    FloorType.Premium => 'P',
                    _ => '?',
                };
            return new string(c);
        }

        [Test]
        public void FirstThreeFloors_AreForcedStandard()
        {
            foreach (ulong seed in new ulong[] { 1, 2, RunSeeds.SeedMvp, 0xFFFFFFFFFFFFFFFFUL })
            {
                var s = BlockSequence.Generate(seed, 3);
                Assert.That(s, Is.All.EqualTo(FloorType.Standard), "seed {0}", seed);
            }
        }

        [Test]
        public void SameSeed_ProducesIdenticalSequence()
        {
            var a = BlockSequence.Generate(RunSeeds.SeedMvp, 200);
            var b = BlockSequence.Generate(RunSeeds.SeedMvp, 200);
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void DifferentSeeds_Differ()
        {
            var a = Encode(BlockSequence.Generate(111, 80));
            var b = Encode(BlockSequence.Generate(222, 80));
            Assert.That(a, Is.Not.EqualTo(b));
        }

        [Test]
        public void NeverMoreThanTwoPremiumInAnyWindowOf5()
        {
            var seq = BlockSequence.Generate(RunSeeds.SeedMvp, 2000);
            for (int i = 0; i + 5 <= seq.Length; i++)
            {
                int premium = 0;
                for (int j = i; j < i + 5; j++)
                    if (seq[j] == FloorType.Premium) premium++;
                Assert.That(premium, Is.LessThanOrEqualTo(2), "window starting at floor {0}", i + 1);
            }
        }

        [Test]
        public void BlockStream_IsIndependentOfSwingStream()
        {
            // Two salted streams from the same run seed must diverge immediately.
            var block = RunSeeds.BlockRng(RunSeeds.SeedMvp);
            var swing = RunSeeds.SwingRng(RunSeeds.SeedMvp);
            bool anyDiff = false;
            for (int i = 0; i < 16; i++)
                if (block.NextULong() != swing.NextULong()) { anyDiff = true; break; }
            Assert.That(anyDiff, Is.True);
        }

        [Test]
        public void Weights_AreRoughlyAsSpecified()
        {
            const int n = 6000;
            var seq = BlockSequence.Generate(RunSeeds.SeedMvp, n);
            int standard = 0, balcony = 0, premium = 0;
            foreach (var t in seq)
            {
                if (t == FloorType.Standard) standard++;
                else if (t == FloorType.Balcony) balcony++;
                else premium++;
            }
            // Loose bounds (forced-Standard prefix + premium cap perturb exact ratios).
            Assert.That(standard, Is.GreaterThan(balcony + premium), "Standard should dominate");
            Assert.That(premium / (double)n, Is.LessThan(0.12), "Premium stays rare");
            Assert.That(balcony / (double)n, Is.GreaterThan(0.10), "Balcony has real presence");
        }

        [Test]
        public void Floor4_IsDrawn_NotForced()
        {
            // Across a fixed seed set, at least one yields a non-Standard floor 4 — proving floor 4
            // comes from the weighted draw, not the forced-Standard prefix (which is floors 1–3).
            bool foundNonStandard = false;
            for (ulong seed = 1; seed <= 50 && !foundNonStandard; seed++)
                if (BlockSequence.Generate(seed, 4)[3] != FloorType.Standard) foundNonStandard = true;
            Assert.That(foundNonStandard, Is.True);
        }

        [Test]
        public void Golden_LocksTheMvpSequence()
        {
            var seq = Encode(BlockSequence.Generate(RunSeeds.SeedMvp, 50));
            Assert.That(seq, Is.EqualTo(GoldenMvp50));
        }

        // First 50 floor types for SeedMvp; locked so an algorithm change can't silently shift
        // a historical daily seed (ADR-0002). Filled from the first green run.
        const string GoldenMvp50 = "SSSSSSSBSSPBSSPSSBSBSPBSBSSSSSPBSSPSSSBSSBBSBBPSBB";
    }
}
