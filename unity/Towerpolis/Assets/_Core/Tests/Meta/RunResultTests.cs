using NUnit.Framework;
using Towerpolis.Core.Gameplay;
using Towerpolis.Core.Meta;

namespace Towerpolis.Core.Tests.Meta
{
    public class RunResultTests
    {
        static void Place(TowerRun run, FloorType type, Grade desired)
        {
            float w = run.CurrentTopWidth;
            float offset = desired switch
            {
                Grade.Perfect => 0f,
                Grade.Good => 0.50f * w,
                _ => 0.95f * w,
            };
            run.PlaceBlock(type, offset);
        }

        [Test]
        public void From_SnapshotsRunState()
        {
            var run = new TowerRun(new CoreConfig());
            Place(run, FloorType.Standard, Grade.Perfect);
            Place(run, FloorType.Standard, Grade.Perfect);
            Place(run, FloorType.Standard, Grade.Good); // resets chain but not TotalPerfects

            var r = RunResult.From(run);
            Assert.That(r.FloorCount, Is.EqualTo(run.FloorCount));
            Assert.That(r.TotalResidents, Is.EqualTo(run.TotalResidents));
            Assert.That(r.RunScore, Is.EqualTo(run.RunScore));
            Assert.That(r.PerfectDrops, Is.EqualTo(2));
        }

        [Test]
        public void From_CarriesMaxPerfectChain_PeakSurvivesAReset()
        {
            var run = new TowerRun(new CoreConfig());
            Place(run, FloorType.Standard, Grade.Perfect); // chain 1
            Place(run, FloorType.Standard, Grade.Perfect); // chain 2 (peak)
            Place(run, FloorType.Standard, Grade.Good);    // chain resets to 0
            Place(run, FloorType.Standard, Grade.Perfect); // chain 1 again

            Assert.That(run.MaxPerfectChain, Is.EqualTo(2));
            Assert.That(RunResult.From(run).MaxPerfectChain, Is.EqualTo(2));
        }

        [Test]
        public void TotalPerfects_CountsOnlyPerfects()
        {
            var run = new TowerRun(new CoreConfig());
            Place(run, FloorType.Standard, Grade.Good);  // not a perfect
            Place(run, FloorType.Standard, Grade.Miss);  // strike 1, not a perfect, not over
            Assert.That(run.TotalPerfects, Is.Zero);
            Assert.That(run.IsOver, Is.False);
        }
    }
}
