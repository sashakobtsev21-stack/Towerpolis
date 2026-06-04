namespace Towerpolis.Game.Gameplay
{
    /// <summary>Global tap suppression so a modal UI (e.g. the city view) doesn't also drop a block.
    /// The HUD sets <see cref="Suppress"/> while a panel is open; the controller ignores taps then.</summary>
    public static class InputGate
    {
        public static bool Suppress;
    }
}
