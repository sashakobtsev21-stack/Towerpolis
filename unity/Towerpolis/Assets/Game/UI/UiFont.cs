namespace Towerpolis.Game.UI
{
    /// <summary>
    /// Cyrillic is already handled by the default TMP font's OWN per-font fallback
    /// ("LiberationSans SDF" → "LiberationSans SDF - Fallback", which is Dynamic + readable and carries the
    /// Cyrillic glyphs), so there is nothing to do at runtime.
    ///
    /// This used to flip the MAIN font to Dynamic to "enable Cyrillic", but the main atlas is NOT
    /// CPU-readable and its runtime sourceFontFile is null — so that only spammed
    /// "Unable to add the requested character … make the texture readable" on every Cyrillic glyph while
    /// the fallback was already rendering the text correctly. Left as a no-op so the HUDs' call sites don't
    /// churn. (Pre-launch we can bake a dedicated Cyrillic SDF font; see the font known-issue note.)
    /// </summary>
    public static class UiFont
    {
        public static void EnsureCyrillic() { /* no-op — the per-font fallback renders Cyrillic */ }
    }
}
