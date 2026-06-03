namespace Towerpolis.Core.Gameplay
{
    /// <summary>
    /// The three gameplay floor types (spec §1.5). The foundation (floor 0) is a fixed anchor, not a
    /// type. <c>Floor_Balcony_2</c> / <c>Base_Ground_2</c> are cosmetic mesh variants chosen in the
    /// Unity layer — never separate gameplay types, so Core grading/scoring stays three-way.
    /// </summary>
    public enum FloorType
    {
        Standard,
        Balcony,
        Premium,
    }
}
