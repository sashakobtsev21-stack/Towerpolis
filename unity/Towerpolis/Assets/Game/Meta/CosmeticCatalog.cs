using UnityEngine;

namespace Towerpolis.Game.Meta
{
    /// <summary>A purchasable block skin (progression-spec §5.1): an alternate body palette that overrides the
    /// district's block colours for a run (the sky stays the district's). Default = no override. Colours are
    /// authored here in code (the actual mesh/material SOs land later); a non-default skin re-tints the 3
    /// body variants per floor type + glass + frame.</summary>
    public readonly struct BlockSkin
    {
        public readonly string Id, DisplayName, RequiredDistrictId;
        public readonly int Cost;
        public readonly bool OverridesBlocks;
        public readonly Color[] Standard, Balcony, Premium;
        public readonly Color Glass, Frame;

        public BlockSkin(string id, string name, int cost, string gate, bool overridesBlocks,
            Color[] standard, Color[] balcony, Color[] premium, Color glass, Color frame)
        {
            Id = id; DisplayName = name; Cost = cost; RequiredDistrictId = gate;
            OverridesBlocks = overridesBlocks;
            Standard = standard; Balcony = balcony; Premium = premium; Glass = glass; Frame = frame;
        }
    }

    /// <summary>A purchasable crane skin (progression-spec §5.2): the rope + hook colours. No gameplay effect,
    /// safe everywhere.</summary>
    public readonly struct CraneSkin
    {
        public readonly string Id, DisplayName, RequiredDistrictId;
        public readonly int Cost;
        public readonly Color RopeColor, HookColor;

        public CraneSkin(string id, string name, int cost, string gate, Color rope, Color hook)
        {
            Id = id; DisplayName = name; Cost = cost; RequiredDistrictId = gate;
            RopeColor = rope; HookColor = hook;
        }
    }

    /// <summary>The launch cosmetics (progression-spec §5). Code-authored palettes that reuse the runtime
    /// recolour pipeline (BlockSpawner / CraneController) — real art-asset skins replace these later.</summary>
    public static class CosmeticCatalog
    {
        static Color C(float r, float g, float b) => new Color(r, g, b);

        public static readonly BlockSkin[] BlockSkins =
        {
            new BlockSkin("skin_default", "Classic", 0, "", false, null, null, null, default, default),

            new BlockSkin("skin_pastel", "Pastel", 150, "", true,
                new[] { C(0.70f,0.92f,0.80f), C(0.72f,0.85f,0.98f), C(0.85f,0.80f,0.96f) },
                new[] { C(1.00f,0.85f,0.72f), C(1.00f,0.78f,0.84f), C(1.00f,0.92f,0.70f) },
                new[] { C(0.78f,0.82f,0.98f), C(0.88f,0.80f,0.96f), C(0.98f,0.90f,0.74f) },
                C(0.80f,0.90f,0.96f), C(1.00f,0.99f,0.96f)),

            new BlockSkin("skin_metal", "Steel", 150, "", true,
                new[] { C(0.55f,0.60f,0.66f), C(0.48f,0.54f,0.60f), C(0.62f,0.66f,0.70f) },
                new[] { C(0.70f,0.72f,0.74f), C(0.58f,0.60f,0.64f), C(0.66f,0.62f,0.58f) },
                new[] { C(0.42f,0.46f,0.52f), C(0.50f,0.54f,0.60f), C(0.60f,0.64f,0.68f) },
                C(0.55f,0.70f,0.78f), C(0.32f,0.35f,0.40f)),

            new BlockSkin("skin_neon_glow", "Neon Glow", 400, "neon", true,
                new[] { C(0.10f,0.80f,0.85f), C(0.20f,0.40f,0.95f), C(0.65f,0.20f,0.90f) },
                new[] { C(0.95f,0.15f,0.60f), C(1.00f,0.35f,0.20f), C(0.85f,0.90f,0.10f) },
                new[] { C(0.15f,0.90f,0.55f), C(0.30f,0.50f,1.00f), C(0.80f,0.25f,0.95f) },
                C(0.20f,0.95f,0.95f), C(0.20f,0.22f,0.30f)),

            new BlockSkin("skin_snow", "Arctic", 400, "winter", true,
                new[] { C(0.95f,0.97f,1.00f), C(0.85f,0.92f,0.98f), C(0.90f,0.96f,0.94f) },
                new[] { C(0.80f,0.90f,0.98f), C(0.92f,0.88f,0.96f), C(0.98f,0.92f,0.90f) },
                new[] { C(0.90f,0.95f,1.00f), C(0.78f,0.86f,0.92f), C(0.96f,0.94f,0.88f) },
                C(0.80f,0.90f,0.95f), C(1.00f,1.00f,1.00f)),
        };

        public static readonly CraneSkin[] CraneSkins =
        {
            new CraneSkin("crane_default", "Hemp",  0,   "",     C(0.45f,0.33f,0.20f), C(0.24f,0.24f,0.27f)),
            new CraneSkin("crane_steel",   "Steel", 200, "",     C(0.62f,0.66f,0.70f), C(0.55f,0.58f,0.63f)),
            new CraneSkin("crane_gold",    "Gold",  400, "",     C(0.82f,0.67f,0.39f), C(0.85f,0.70f,0.30f)),
            new CraneSkin("crane_neon",    "Neon",  400, "neon", C(0.20f,0.90f,0.95f), C(0.92f,0.20f,0.70f)),
        };

        public static BlockSkin GetBlockSkin(string id)
        {
            foreach (BlockSkin s in BlockSkins) if (s.Id == id) return s;
            return BlockSkins[0];
        }

        public static CraneSkin GetCraneSkin(string id)
        {
            foreach (CraneSkin s in CraneSkins) if (s.Id == id) return s;
            return CraneSkins[0];
        }
    }
}
