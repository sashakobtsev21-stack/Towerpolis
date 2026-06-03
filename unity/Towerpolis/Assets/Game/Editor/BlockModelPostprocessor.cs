using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Towerpolis.Game.Editor
{
    /// <summary>
    /// Imports the gameplay block FBX under <c>Resources/Blocks</c> so the houses arrive correctly:
    /// <list type="bullet">
    /// <item>materials are created (one per Blender slot) and <b>recoloured by name</b> from the authored
    /// palette (<c>scripts/blender_build_blocks.py</c>) — Unity doesn't reliably read the FBX diffuse, so
    /// we set the colours ourselves and the result is deterministic regardless of import quirks;</item>
    /// <item>unit scale kept, and auto-collider/camera/light import skipped — the
    /// <see cref="Towerpolis.Game.Gameplay.BlockSpawner"/> builds a clean BoxCollider in code.</item>
    /// </list>
    /// After editing this file, right-click <c>Assets/Art/Resources/Blocks</c> → <b>Reimport</b> to re-run it.
    /// </summary>
    public sealed class BlockModelPostprocessor : AssetPostprocessor
    {
        // Authored palette (linear RGB from the Blender recipe). Body colours, frames, glass, trims.
        static readonly Dictionary<string, Color> Palette = new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase)
        {
            { "TP_Green",    new Color(0.32f, 0.78f, 0.38f) },
            { "TP_Yellow",   new Color(1.00f, 0.82f, 0.15f) },
            { "TP_Orange",   new Color(1.00f, 0.52f, 0.12f) },
            { "TP_Blue",     new Color(0.18f, 0.58f, 1.00f) },
            { "TP_White",    new Color(0.99f, 0.99f, 0.97f) },
            { "TP_Glass",    new Color(0.10f, 0.40f, 1.00f) },
            { "TP_Wood",     new Color(0.58f, 0.40f, 0.22f) },
            { "TP_Marble",   new Color(0.92f, 0.92f, 0.95f) },
            { "TP_CanopyLB", new Color(0.74f, 0.56f, 0.36f) },
            { "TP_Brick",    new Color(0.78f, 0.40f, 0.32f) },
            { "TP_BrickLine",new Color(0.62f, 0.30f, 0.23f) },
            { "TP_Brick2",   new Color(0.86f, 0.74f, 0.54f) },
            { "TP_Brick2L",  new Color(0.72f, 0.60f, 0.42f) },
            { "TP_DarkBrown",new Color(0.34f, 0.22f, 0.13f) },
            { "TP_Steps",    new Color(0.60f, 0.62f, 0.65f) },
            { "TP_Gold",     new Color(1.00f, 0.82f, 0.30f) },
            { "TP_Ground",   new Color(0.93f, 0.95f, 0.98f) },
        };

        bool IsBlock => assetPath.Replace('\\', '/')
            .IndexOf("/Resources/Blocks/", StringComparison.OrdinalIgnoreCase) >= 0;

        void OnPreprocessModel()
        {
            if (!IsBlock) return;
            var importer = (ModelImporter)assetImporter;
            importer.useFileScale = true;
            importer.globalScale = 1f;
            importer.addCollider = false;
            importer.importCameras = false;
            importer.importLights = false;
            importer.importBlendShapes = false;
            importer.isReadable = false;
            importer.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;
            importer.materialLocation = ModelImporterMaterialLocation.InPrefab;
        }

        // Runs once per material the model importer creates. We override the colour from the authored
        // palette so the houses are never left white/magenta.
        void OnPostprocessMaterial(Material material)
        {
            if (!IsBlock) return;
            if (!TryMatch(material.name, out Color c)) return;

            bool glass = material.name.IndexOf("Glass", StringComparison.OrdinalIgnoreCase) >= 0;
            bool gold = material.name.IndexOf("Gold", StringComparison.OrdinalIgnoreCase) >= 0;
            bool marble = material.name.IndexOf("Marble", StringComparison.OrdinalIgnoreCase) >= 0;

            // URP/Lit uses _BaseColor; legacy uses _Color. Set both so it works whichever shader resolved.
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", c);
            if (material.HasProperty("_Color")) material.SetColor("_Color", c);

            float smoothness = glass ? 0.9f : marble ? 0.6f : gold ? 0.8f : 0.25f;
            float metallic = gold ? 1.0f : 0.0f;
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        }

        // Exact name first; then longest-key contains (so "TP_Brick2L" wins over "TP_Brick").
        static bool TryMatch(string name, out Color color)
        {
            if (Palette.TryGetValue(name, out color)) return true;
            string best = null;
            foreach (var kv in Palette)
                if (name.IndexOf(kv.Key, StringComparison.OrdinalIgnoreCase) >= 0
                    && (best == null || kv.Key.Length > best.Length))
                    best = kv.Key;
            if (best != null) { color = Palette[best]; return true; }
            color = default;
            return false;
        }
    }
}
