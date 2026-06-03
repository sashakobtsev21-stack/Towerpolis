using System;

namespace Towerpolis.Core.Gameplay
{
    /// <summary>
    /// Deterministic placement grading (spec §3, R2). The horizontal offset of the dropped block is
    /// graded as a fraction of the CURRENT top width (the surface being landed on), so the bands stay
    /// honest as slicing narrows the tower. Pure math — never PhysX (ADR-0002).
    /// </summary>
    public static class Grading
    {
        public static Grade Evaluate(CoreConfig cfg, float offsetX, float currentTopWidth)
        {
            if (cfg is null) throw new ArgumentNullException(nameof(cfg));
            if (currentTopWidth <= 0f) return Grade.Miss;

            float ratio = Math.Abs(offsetX) / currentTopWidth;
            if (ratio <= cfg.PerfectThreshold) return Grade.Perfect;
            if (ratio <= cfg.GoodThreshold) return Grade.Good;
            if (ratio <= cfg.SloppyThreshold) return Grade.Sloppy;
            return Grade.Miss;
        }
    }
}
