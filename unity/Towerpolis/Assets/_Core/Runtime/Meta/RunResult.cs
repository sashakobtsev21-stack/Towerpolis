using Towerpolis.Core.Gameplay;

namespace Towerpolis.Core.Meta
{
    /// <summary>
    /// The frozen, deterministic result of one finished run — the unit the meta layer deposits, scores
    /// and banks coins from (meta-spec §1.3, §5). Built from a <see cref="TowerRun"/>; carries no engine
    /// state, so the city/economy logic stays Unity-free and unit-testable.
    /// </summary>
    public readonly struct RunResult
    {
        public readonly int FloorCount;       // floors placed (excludes the base)
        public readonly int TotalResidents;   // residents to deposit into the city
        public readonly int RunScore;         // leaderboard score (floors + resident bonus)
        public readonly int PerfectDrops;     // cumulative Perfect placements (coins + stats)

        public RunResult(int floorCount, int totalResidents, int runScore, int perfectDrops)
        {
            FloorCount = floorCount;
            TotalResidents = totalResidents;
            RunScore = runScore;
            PerfectDrops = perfectDrops;
        }

        /// <summary>Snapshot the final state of a (typically ended) run.</summary>
        public static RunResult From(TowerRun run)
        {
            if (run is null) throw new System.ArgumentNullException(nameof(run));
            return new RunResult(run.FloorCount, run.TotalResidents, run.RunScore, run.TotalPerfects);
        }
    }
}
