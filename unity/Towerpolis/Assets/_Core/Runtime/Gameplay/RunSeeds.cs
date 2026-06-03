using Towerpolis.Core.Determinism;

namespace Towerpolis.Core.Gameplay
{
    /// <summary>
    /// Per-run seed derivation (spec §1.5 / OQ-11). Each gameplay RNG stream is derived from the run
    /// seed via a DISTINCT SplitMix64 salt, so advancing one stream (e.g. the crane swing) never
    /// perturbs another (e.g. the block-type sequence). This keeps every stream golden-testable in
    /// isolation and underpins the daily-seed determinism (same seed → same run on every device).
    /// </summary>
    public static class RunSeeds
    {
        /// <summary>Hardcoded MVP run seed (spec §1.3). Daily-seed variant is post-MVP.</summary>
        public const ulong SeedMvp = 0xDEADBEEFCAFEF00DUL;

        public const ulong SaltBlock = 0xB10C5EEDB10C5EEDUL;
        public const ulong SaltSwing = 0x5717650557176505UL;

        /// <summary>RNG stream for the block-type sequence.</summary>
        public static XorShiftRng BlockRng(ulong runSeed)
            => new XorShiftRng(SeedMix.SplitMix64(runSeed ^ SaltBlock));

        /// <summary>RNG stream for the crane swing phase.</summary>
        public static XorShiftRng SwingRng(ulong runSeed)
            => new XorShiftRng(SeedMix.SplitMix64(runSeed ^ SaltSwing));
    }
}
