using TMPro;

namespace Towerpolis.Game.UI
{
    /// <summary>
    /// One-time UI font setup. The default TMP font asset usually ships with a STATIC Latin-only atlas, so
    /// Cyrillic renders as missing boxes. Switching it to DYNAMIC makes TMP rasterise any glyph present in
    /// the underlying source font (LiberationSans includes Cyrillic) on demand — so Russian text shows up
    /// without bundling a custom font asset. Called once from the HUDs' Start.
    /// </summary>
    public static class UiFont
    {
        static bool _done;

        public static void EnsureCyrillic()
        {
            if (_done) return;
            _done = true;
            TMP_FontAsset f = TMP_Settings.defaultFontAsset;
            if (f != null && f.atlasPopulationMode != AtlasPopulationMode.Dynamic)
                f.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        }
    }
}
