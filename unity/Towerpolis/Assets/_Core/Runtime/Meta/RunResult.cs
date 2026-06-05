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
        public readonly int MaxPerfectChain;  // longest Perfect chain this run (weekly-mission metric)
        public readonly int TrophyRoofResidents; // run-end streak bonus residents, already folded into TotalResidents (Phase C)

        public RunResult(int floorCount, int totalResidents, int runScore, int perfectDrops,
            int maxPerfectChain = 0, int trophyRoofResidents = 0)
        {
            FloorCount = floorCount;
            TotalResidents = totalResidents;
            RunScore = runScore;
            PerfectDrops = perfectDrops;
            MaxPerfectChain = maxPerfectChain;
            TrophyRoofResidents = trophyRoofResidents;
        }

        /// <summary>Snapshot the final state of a (typically ended) run. The Phase C trophy-roof bonus is
        /// folded into <see cref="TotalResidents"/> (so it deposits into the city) and also reported on its
        /// own for the end-screen.</summary>
        public static RunResult From(TowerRun run)
        {
            if (run is null) throw new System.ArgumentNullException(nameof(run));
            int trophy = run.TrophyRoofResidents;
            return new RunResult(run.FloorCount, run.TotalResidents + trophy, run.RunScore,
                run.TotalPerfects, run.MaxPerfectChain, trophy);
        }
    }
}
