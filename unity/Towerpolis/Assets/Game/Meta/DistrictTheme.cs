using UnityEngine;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// The per-district LOOK (meta-spec §2): the 3 body-colour variants per floor type, the glass and
    /// frame tints, and the sky gradient. Phase 3 reuses the shared block meshes recoloured per district
    /// (no new geometry); the BlockSpawner applies the block colours and DistrictSky the sky. Full
    /// palette/skybox/music assets land as DistrictDefinition SOs later (ADR-0007).
    /// </summary>
    public readonly struct DistrictTheme
    {
        public readonly Color[] Standard;
        public readonly Color[] Balcony;
        public readonly Color[] Premium;
        public readonly Color Glass;
        public readonly Color Frame;
        public readonly Color SkyTop;
        public readonly Color SkyHorizon;
        public readonly Color SkyBottom;

        public DistrictTheme(Color[] standard, Color[] balcony, Color[] premium, Color glass, Color frame,
            Color skyTop, Color skyHorizon, Color skyBottom)
        {
            Standard = standard;
            Balcony = balcony;
            Premium = premium;
            Glass = glass;
            Frame = frame;
            SkyTop = skyTop;
            SkyHorizon = skyHorizon;
            SkyBottom = skyBottom;
        }
    }

    public static class DistrictThemes
    {
        // Downtown = the current bright daytime look (1:1 with BlockSpawner's defaults — no regression).
        public static readonly DistrictTheme Downtown = new DistrictTheme(
            standard: new[] { new Color(0.46f, 0.88f, 0.50f), new Color(0.40f, 0.74f, 1.00f), new Color(0.20f, 0.86f, 0.80f) },
            balcony: new[] { new Color(1.00f, 0.86f, 0.30f), new Color(1.00f, 0.40f, 0.46f), new Color(0.66f, 0.92f, 0.36f) },
            premium: new[] { new Color(0.34f, 0.60f, 1.00f), new Color(0.70f, 0.50f, 0.98f), new Color(1.00f, 0.84f, 0.36f) },
            glass: new Color(0.46f, 0.66f, 0.86f),
            frame: new Color(0.94f, 0.93f, 0.88f),
            skyTop: new Color(0.20f, 0.46f, 0.83f), skyHorizon: new Color(0.68f, 0.85f, 0.97f), skyBottom: new Color(0.52f, 0.68f, 0.86f));

        // Neon Quarter — dark walls, electric accents, night sky.
        public static readonly DistrictTheme Neon = new DistrictTheme(
            standard: new[] { new Color(0.12f, 0.46f, 0.55f), new Color(0.20f, 0.26f, 0.58f), new Color(0.42f, 0.16f, 0.58f) },
            balcony: new[] { new Color(0.92f, 0.16f, 0.56f), new Color(1.00f, 0.36f, 0.62f), new Color(0.10f, 0.78f, 0.82f) },
            premium: new[] { new Color(0.20f, 0.42f, 1.00f), new Color(0.30f, 0.92f, 0.56f), new Color(0.62f, 0.26f, 0.96f) },
            glass: new Color(0.20f, 0.85f, 0.92f),
            frame: new Color(0.30f, 0.33f, 0.42f),
            skyTop: new Color(0.05f, 0.05f, 0.17f), skyHorizon: new Color(0.34f, 0.16f, 0.46f), skyBottom: new Color(0.10f, 0.08f, 0.26f));

        // Winter Heights — snow-white walls, icy accents, pale overcast sky.
        public static readonly DistrictTheme Winter = new DistrictTheme(
            standard: new[] { new Color(0.92f, 0.95f, 0.98f), new Color(0.74f, 0.85f, 0.95f), new Color(0.80f, 0.92f, 0.86f) },
            balcony: new[] { new Color(0.70f, 0.84f, 0.95f), new Color(0.86f, 0.82f, 0.93f), new Color(0.96f, 0.86f, 0.88f) },
            premium: new[] { new Color(0.85f, 0.92f, 0.97f), new Color(0.55f, 0.72f, 0.62f), new Color(0.95f, 0.90f, 0.76f) },
            glass: new Color(0.72f, 0.85f, 0.92f),
            frame: new Color(0.96f, 0.97f, 0.99f),
            skyTop: new Color(0.62f, 0.72f, 0.84f), skyHorizon: new Color(0.88f, 0.91f, 0.95f), skyBottom: new Color(0.78f, 0.82f, 0.88f));

        public static DistrictTheme For(string id) => id switch
        {
            "neon" => Neon,
            "winter" => Winter,
            _ => Downtown,
        };
    }
}
