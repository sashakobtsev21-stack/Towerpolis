using System;
using System.Collections.Generic;
using UnityEngine;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// Creates block GameObjects whose ROOT transform is at the block's bottom-center (the authored
    /// pivot — clean stacking). The visible house is the imported Blender FBX (loaded from
    /// <c>Resources/Blocks</c>), wrapped in a <c>"Mesh"</c> child that carries a clean, code-built
    /// <see cref="BoxCollider"/> sized to the gameplay grid. Materials are <b>recoloured at runtime</b>
    /// from the authored palette (Unity doesn't reliably import the FBX diffuse), so houses are always
    /// coloured; unmatched slots fall back to the solid body colour. If a model isn't imported yet, a
    /// flat-coloured primitive cube stands in so the loop still runs. The authored bodies are exactly
    /// 2.0 × 1.5 × 2.0 (bottom-center), matching <see cref="CoreConfig.InitialBlockWidth"/> /
    /// <c>floorHeight</c>, so they drop in 1:1; the height auto-fit corrects any FBX import-scale quirk.
    /// </summary>
    public sealed class BlockSpawner : MonoBehaviour
    {
        [SerializeField] float depth = 2.0f;       // footprint Z (X width comes in per-block as blockWidth)
        [SerializeField] float floorHeight = 1.5f; // stacking step / body height (== GameTuning.floorHeight)
        [Tooltip("Yaw so the decorated front (balconies/entrance) faces the camera. The FBX exports the " +
                 "front along -Z (axis_forward=-Z) and the camera sits on -Z, so 0 faces the player.")]
        [SerializeField] float modelFacingYaw = 0f;

        const string ResourceDir = "Blocks/"; // Assets/Art/Resources/Blocks/<Name>.fbx

        static readonly string[] ModelNames =
        {
            "Floor_Standard", "Floor_Balcony", "Floor_Balcony_2",
            "Floor_Premium", "Base_Ground", "Base_Ground_2",
        };

        // Authored palette — a muted, warm, harmonious scheme (premium feel, not primary-bright).
        // name → (colour, smoothness, metallic).
        static readonly (string name, Color color, float smooth, float metal)[] PaletteSpec =
        {
            ("TP_Green",    new Color(0.49f, 0.66f, 0.48f), 0.25f, 0f), // sage (Standard body)
            ("TP_Yellow",   new Color(0.90f, 0.73f, 0.38f), 0.25f, 0f), // warm ochre (Balcony body)
            ("TP_Orange",   new Color(0.84f, 0.52f, 0.35f), 0.25f, 0f), // muted terracotta (Balcony_2)
            ("TP_Blue",     new Color(0.44f, 0.57f, 0.70f), 0.25f, 0f), // dusty slate blue (Premium body)
            ("TP_White",    new Color(0.94f, 0.93f, 0.88f), 0.30f, 0f), // warm cream (frames)
            ("TP_Glass",    new Color(0.28f, 0.56f, 0.95f), 0.96f, 0.40f), // bright glossy blue glass (windows)
            ("TP_Wood",     new Color(0.51f, 0.37f, 0.26f), 0.20f, 0f), // walnut (balconies)
            ("TP_Marble",   new Color(0.88f, 0.87f, 0.83f), 0.55f, 0f), // warm off-white
            ("TP_CanopyLB", new Color(0.71f, 0.57f, 0.42f), 0.20f, 0f), // tan canopies
            ("TP_Brick",    new Color(0.71f, 0.46f, 0.39f), 0.15f, 0f), // soft clay (Base body)
            ("TP_BrickLine",new Color(0.56f, 0.35f, 0.30f), 0.15f, 0f),
            ("TP_Brick2",   new Color(0.81f, 0.71f, 0.56f), 0.15f, 0f), // sandstone (Base_2)
            ("TP_Brick2L",  new Color(0.67f, 0.57f, 0.44f), 0.15f, 0f),
            ("TP_DarkBrown",new Color(0.33f, 0.24f, 0.18f), 0.20f, 0f), // espresso door
            ("TP_Steps",    new Color(0.62f, 0.60f, 0.57f), 0.30f, 0f), // warm stone
            ("TP_Gold",     new Color(0.82f, 0.67f, 0.39f), 0.70f, 1f), // brass handles
            ("TP_Ground",   new Color(0.90f, 0.90f, 0.88f), 0.30f, 0f),
        };

        // Body-colour fallback per type (used when a model slot's name doesn't match the palette).
        static readonly Color ColStandard = new Color(0.49f, 0.66f, 0.48f);
        static readonly Color ColBalcony = new Color(0.90f, 0.73f, 0.38f);
        static readonly Color ColPremium = new Color(0.44f, 0.57f, 0.70f);
        static readonly Color ColBrick = new Color(0.71f, 0.46f, 0.39f);

        // Three muted body-colour variants per floor TYPE, so the city isn't monotone. Purely cosmetic —
        // Core stays three-way; the variant is picked per floor for variety (see VariantIndex).
        static readonly Color[] StandardVariants =
        {
            new Color(0.56f, 0.75f, 0.54f), // sage green
            new Color(0.88f, 0.61f, 0.50f), // terracotta
            new Color(0.58f, 0.70f, 0.83f), // soft blue
        };
        static readonly Color[] BalconyVariants =
        {
            new Color(0.95f, 0.79f, 0.46f), // warm ochre
            new Color(0.88f, 0.63f, 0.64f), // rose
            new Color(0.50f, 0.74f, 0.68f), // teal
        };
        static readonly Color[] PremiumVariants =
        {
            new Color(0.50f, 0.65f, 0.81f), // slate blue
            new Color(0.66f, 0.57f, 0.74f), // mauve
            new Color(0.82f, 0.78f, 0.69f), // warm stone
        };
        // The model material slots that are the BODY (wall) — these get the per-block variant colour;
        // frames/glass/trim keep the palette. (Base brick is excluded — bases pass no override.)
        static readonly HashSet<string> BodyNames =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TP_Green", "TP_Yellow", "TP_Orange", "TP_Blue" };

        readonly Dictionary<string, GameObject> _models = new Dictionary<string, GameObject>();
        readonly Dictionary<string, Material> _palette = new Dictionary<string, Material>(StringComparer.OrdinalIgnoreCase);
        Shader _lit;
        Material _matStandard, _matBalcony, _matPremium, _matBrick;
        Material[] _standardBodies, _balconyBodies, _premiumBodies;

        void Awake()
        {
            _lit = Shader.Find("Universal Render Pipeline/Lit");
            if (_lit == null) _lit = Shader.Find("Standard");

            foreach (var p in PaletteSpec)
            {
                float emis = string.Equals(p.name, "TP_Glass", StringComparison.OrdinalIgnoreCase) ? 0.5f : 0f;
                _palette[p.name] = MakeMaterial(p.color, p.smooth, p.metal, emis);
            }

            _matStandard = MakeMaterial(ColStandard);
            _matBalcony = MakeMaterial(ColBalcony);
            _matPremium = MakeMaterial(ColPremium);
            _matBrick = MakeMaterial(ColBrick);

            _standardBodies = MakeBodies(StandardVariants);
            _balconyBodies = MakeBodies(BalconyVariants);
            _premiumBodies = MakeBodies(PremiumVariants);

            foreach (string n in ModelNames)
            {
                var go = Resources.Load<GameObject>(ResourceDir + n);
                if (go != null) _models[n] = go;
            }
        }

        public Transform CreateBlock(FloorType type, float blockWidth, string label)
        {
            Material body = BodyVariant(type, label); // one of 3 colours for this type → varied city
            return Build(label, ModelName(type, label), blockWidth, body, colliderOn: false, body);
        }

        public Transform CreateBase(float blockWidth)
            => Build("Base", "Base_Ground", blockWidth, _matBrick, colliderOn: true, bodyOverride: null);

        Transform Build(string label, string modelName, float blockWidth, Material bodyFallback, bool colliderOn,
            Material bodyOverride)
        {
            var root = new GameObject(label);
            var mesh = new GameObject("Mesh");
            mesh.transform.SetParent(root.transform, false);

            // The real collider: a clean box on the gameplay grid, independent of the art mesh. Off during
            // the crane/controlled-fall phase (landing uses the scripted swept check, not physics);
            // switched ON once the block is welded (a solid obstacle), tumbling (a miss), or base.
            var col = mesh.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, floorHeight * 0.5f, 0f);
            col.size = new Vector3(blockWidth, floorHeight, depth);
            col.enabled = colliderOn;

            if (_models.TryGetValue(modelName, out GameObject prefab) && prefab != null)
            {
                var model = Instantiate(prefab, mesh.transform);
                model.name = "Model";
                model.transform.localRotation = Quaternion.Euler(0f, modelFacingYaw, 0f);
                model.transform.localPosition = Vector3.zero;
                FitToGrid(model, floorHeight, mesh.transform);
                Recolor(model, bodyFallback, bodyOverride);
            }
            else
            {
                // Pre-import fallback: a flat-coloured cube on the gameplay grid.
                var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cube.name = "Fallback";
                var stray = cube.GetComponent<Collider>();
                if (stray != null) Destroy(stray); // the BoxCollider above is the only collider we keep
                cube.transform.SetParent(mesh.transform, false);
                cube.transform.localScale = new Vector3(blockWidth, floorHeight, depth);
                cube.transform.localPosition = new Vector3(0f, floorHeight * 0.5f, 0f);
                cube.GetComponent<MeshRenderer>().sharedMaterial = bodyFallback;
            }
            return root.transform;
        }

        /// <summary>Repaint each material slot from the authored palette (matched by the imported slot
        /// name, e.g. <c>TP_Green</c>). Unmatched slots get the solid body colour, so a house is never
        /// left white even if the FBX imported with a single default material.</summary>
        void Recolor(GameObject model, Material bodyFallback, Material bodyOverride)
        {
            foreach (var r in model.GetComponentsInChildren<MeshRenderer>())
            {
                var src = r.sharedMaterials;
                var dst = new Material[src.Length];
                for (int i = 0; i < src.Length; i++)
                {
                    string name = src[i] != null ? src[i].name : null;
                    dst[i] = bodyOverride != null && name != null && BodyNames.Contains(name)
                        ? bodyOverride                                   // the wall → this block's variant colour
                        : PaletteFor(name) ?? bodyFallback;              // frames/glass/trim → palette
                }
                r.sharedMaterials = dst;
            }
        }

        Material PaletteFor(string slotName)
        {
            if (string.IsNullOrEmpty(slotName)) return null;
            if (_palette.TryGetValue(slotName, out Material exact)) return exact;
            // Imported names can carry suffixes — fall back to the longest palette key the name contains
            // ("TP_Brick2L" wins over "TP_Brick").
            Material best = null; int bestLen = 0;
            foreach (var p in PaletteSpec)
                if (slotName.IndexOf(p.name, StringComparison.OrdinalIgnoreCase) >= 0 && p.name.Length > bestLen)
                {
                    best = _palette[p.name];
                    bestLen = p.name.Length;
                }
            return best;
        }

        /// <summary>Uniform-scale the instantiated model so its body height == <paramref name="targetHeight"/>
        /// (keeps proportions; corrects any FBX import-scale quirk) and drop it so its bottom rests exactly
        /// on the floor below. The decorative balconies/canopies keep overhanging — that is the look.</summary>
        static void FitToGrid(GameObject model, float targetHeight, Transform meshParent)
        {
            if (!TryBounds(model, out Bounds b)) return;
            float h = b.size.y;
            if (h > 1e-4f) model.transform.localScale *= targetHeight / h;

            if (!TryBounds(model, out Bounds b2)) return;
            float bottomLocalY = meshParent.InverseTransformPoint(new Vector3(0f, b2.min.y, 0f)).y;
            model.transform.localPosition -= new Vector3(0f, bottomLocalY, 0f);
        }

        static bool TryBounds(GameObject model, out Bounds bounds)
        {
            var rends = model.GetComponentsInChildren<Renderer>();
            if (rends.Length == 0) { bounds = default; return false; }
            bounds = rends[0].bounds;
            for (int i = 1; i < rends.Length; i++) bounds.Encapsulate(rends[i].bounds);
            return true;
        }

        /// <summary>Enable/disable a block's collider (off during the scripted fall; on once it is a
        /// welded obstacle or a tumbling miss).</summary>
        public void SetColliderEnabled(Transform blockRoot, bool on)
        {
            var mesh = blockRoot.Find("Mesh");
            if (mesh == null) return;
            var col = mesh.GetComponent<Collider>();
            if (col != null) col.enabled = on;
        }

        Material MakeMaterial(Color color, float smoothness = 0.25f, float metallic = 0f, float emission = 0f)
        {
            var mat = new Material(_lit) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
            if (mat.HasProperty("_Glossiness")) mat.SetFloat("_Glossiness", smoothness);
            if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
            if (emission > 0f)
            {
                mat.EnableKeyword("_EMISSION"); // glass glows a touch so it reads bright and catches bloom
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", color * emission);
            }
            return mat;
        }

        Material[] MakeBodies(Color[] colors)
        {
            var mats = new Material[colors.Length];
            for (int i = 0; i < colors.Length; i++) mats[i] = MakeMaterial(colors[i]);
            return mats;
        }

        // The variant body material for this floor (one of 3 per type), picked deterministically from the
        // floor number so the city looks varied but is reproducible.
        Material BodyVariant(FloorType type, string label)
        {
            Material[] set = type switch
            {
                FloorType.Balcony => _balconyBodies,
                FloorType.Premium => _premiumBodies,
                _ => _standardBodies,
            };
            return set[VariantIndex(label, set.Length)];
        }

        static int VariantIndex(string label, int count)
        {
            int n = FloorNumber(label);
            int h = (n * 2654435) ^ (n << 3) ^ 0x5bd1e995; // cheap hash → looks shuffled, not a 1-2-3 cycle
            return ((h % count) + count) % count;
        }

        // Cosmetic mesh variant chosen in the Unity layer (Core stays three-way — spec §1.5). Balcony floors
        // alternate yellow/orange by parity for variety; the choice is purely visual.
        static string ModelName(FloorType type, string label) => type switch
        {
            FloorType.Standard => "Floor_Standard",
            FloorType.Balcony => FloorIsEven(label) ? "Floor_Balcony_2" : "Floor_Balcony",
            FloorType.Premium => "Floor_Premium",
            _ => "Floor_Standard",
        };

        static bool FloorIsEven(string label) => (FloorNumber(label) & 1) == 0;

        static int FloorNumber(string label)
        {
            int us = label.LastIndexOf('_');
            return us >= 0 && int.TryParse(label.Substring(us + 1), out int n) ? n : 0;
        }

        Material MaterialFor(FloorType type) => type switch
        {
            FloorType.Balcony => _matBalcony,
            FloorType.Premium => _matPremium,
            _ => _matStandard,
        };
    }
}
