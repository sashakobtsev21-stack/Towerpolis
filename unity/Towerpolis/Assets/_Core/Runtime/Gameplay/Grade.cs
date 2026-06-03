namespace Towerpolis.Core.Gameplay
{
    /// <summary>Placement grade for a dropped block (spec §3). Derived from deterministic offset
    /// math, never from PhysX (ADR-0002).</summary>
    public enum Grade
    {
        Perfect,
        Good,
        Sloppy,
        Miss,
    }
}
