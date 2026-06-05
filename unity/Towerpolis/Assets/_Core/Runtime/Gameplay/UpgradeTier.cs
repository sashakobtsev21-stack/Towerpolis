namespace Towerpolis.Core.Gameplay
{
    /// <summary>
    /// An earned specialty-block upgrade (Phase C, Tower-Bloxx). A streak of Perfects grants a pending
    /// upgrade that raises the next spawned block to a better floor type (more residents). Ordered to match
    /// <see cref="FloorType"/> (None &lt; Balcony &lt; Premium) so resolving is a simple Max with the seeded type.
    /// </summary>
    public enum UpgradeTier
    {
        None = 0,
        Balcony = 1,
        Premium = 2,
    }
}
