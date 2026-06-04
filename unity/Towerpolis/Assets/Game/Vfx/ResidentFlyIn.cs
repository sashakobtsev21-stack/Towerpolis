using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Towerpolis.Game.Gameplay;

namespace Towerpolis.Game.Vfx
{
    /// <summary>
    /// When a floor lands, little umbrella residents — as many as the floor's <c>residentsAdded</c> — drift in
    /// from the side and slightly above and parachute INTO the new block (the building gaining its tenants).
    /// Drop your Blender residents into <c>Resources/Residents/Resident_Umbrella_1..3</c> to replace the
    /// procedural placeholder figures (drop-in, no wiring). Self-bootstraps off the controller. Cosmetic only
    /// — uses UnityEngine.Random, never the deterministic Core.
    /// </summary>
    public sealed class ResidentFlyIn : MonoBehaviour
    {
        const float FloorHeight = 1.5f;
        const int MaxPerPlacement = 8; // safety cap on simultaneous figures

        TowerGameController _controller;
        TowerController _tower;
        GameObject[] _models;

        void Awake()
        {
            var list = new List<GameObject>();
            for (int i = 1; i <= 3; i++)
            {
                GameObject m = Resources.Load<GameObject>("Residents/Resident_Umbrella_" + i);
                if (m != null) list.Add(m);
            }
            if (list.Count == 0) list.Add(BuildProcedural());
            _models = list.ToArray();
        }

        void OnEnable() => Bind();
        void Start() => Bind();

        void OnDisable()
        {
            if (_controller != null) _controller.FloorPlacedAt -= OnPlaced;
            _controller = null;
        }

        void Bind()
        {
            if (_tower == null) _tower = FindFirstObjectByType<TowerController>(); // residents ride the tower
            if (_controller != null) return;
            _controller = FindFirstObjectByType<TowerGameController>();
            if (_controller != null) _controller.FloorPlacedAt += OnPlaced;
        }

        void OnPlaced(Vector3 basePos, bool perfect, int residents)
        {
            int n = Mathf.Clamp(residents, 0, MaxPerPlacement);
            for (int i = 0; i < n; i++) StartCoroutine(FlyOne(basePos, i));
        }

        IEnumerator FlyOne(Vector3 basePos, int index)
        {
            yield return new WaitForSeconds(index * 0.10f); // staggered arrivals

            GameObject template = _models[Random.Range(0, _models.Length)];
            GameObject go = Instantiate(template);
            go.SetActive(true);
            foreach (Collider c in go.GetComponentsInChildren<Collider>()) Destroy(c); // cosmetic — never collide
            Transform t = go.transform;

            // Ride the tower: parent to it + animate in LOCAL space, so the resident rises and sways WITH the
            // building (no camera lag / floating below) and is auto-cleaned when the tower clears on restart.
            Transform parent = _tower != null ? _tower.transform : null;
            Vector3 localBase = parent != null ? parent.InverseTransformPoint(basePos) : basePos;
            if (parent != null) t.SetParent(parent, false);
            Vector3 baseScale = t.localScale;
            TintUmbrella(go, index);

            // From the SIDE at ~roof height + in front; glide in mostly HORIZONTALLY and vanish passing
            // THROUGH the side wall (end on the approach side, ~half block width).
            float dir = (index % 2 == 0) ? 1f : -1f;
            Vector3 start = localBase + new Vector3(dir * (2.2f + 0.2f * index + Random.Range(0f, 0.5f)),
                FloorHeight * 0.85f + Random.Range(0f, 0.35f), -1.2f + Random.Range(-0.2f, 0.2f));
            Vector3 end = localBase + new Vector3(dir * 0.92f, FloorHeight * 0.55f + Random.Range(-0.1f, 0.2f), -0.55f + Random.Range(-0.15f, 0.15f));

            float dur = 0.95f + Random.Range(0f, 0.35f);
            Destroy(go, dur + 0.4f); // hard safety — a resident never lingers, even if this coroutine is cut
            float swayPhase = Random.Range(0f, 6.283f);
            float e = 0f;
            while (e < dur)
            {
                if (t == null) yield break;
                e += Time.deltaTime;
                float p = Mathf.Clamp01(e / dur);
                float ease = 1f - (1f - p) * (1f - p);                               // ease-out glide
                Vector3 pos = Vector3.Lerp(start, end, ease);
                pos.y += Mathf.Sin(Time.time * 2.3f + swayPhase) * 0.10f * (1f - p);  // gentle umbrella bob
                if (parent != null) t.localPosition = pos; else t.position = pos;
                t.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(Time.time * 2.3f + swayPhase) * 9f * (1f - p));
                if (p > 0.5f) t.localScale = baseScale * Mathf.Clamp01(1f - (p - 0.5f) / 0.5f); // shrink through the 2nd half
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        static readonly Color[] UmbrellaColors =
        {
            new Color(0.95f, 0.30f, 0.34f), new Color(1f, 0.72f, 0.20f), new Color(0.30f, 0.78f, 0.45f),
            new Color(0.28f, 0.62f, 1f),    new Color(0.78f, 0.42f, 0.95f), new Color(0.20f, 0.82f, 0.80f),
        };

        void TintUmbrella(GameObject go, int index)
        {
            Transform u = go.transform.Find("Umbrella");
            if (u == null) return; // a drop-in model keeps its own colours
            var r = u.GetComponent<MeshRenderer>();
            if (r != null) r.material.color = UmbrellaColors[index % UmbrellaColors.Length];
        }

        // --- procedural placeholder figure (little person under an umbrella) ---

        GameObject BuildProcedural()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) lit = Shader.Find("Standard");

            var root = new GameObject("Resident");
            root.SetActive(false);                 // hidden template; clones are activated per spawn
            root.transform.localScale = Vector3.one * 0.45f;

            Part(root.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.55f, 0f), new Vector3(0.42f, 0.45f, 0.42f), new Color(0.30f, 0.45f, 0.72f), lit);
            Part(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.12f, 0f), Vector3.one * 0.42f, new Color(0.98f, 0.82f, 0.66f), lit);
            Part(root.transform, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 1.55f, 0f), new Vector3(0.04f, 0.45f, 0.04f), new Color(0.40f, 0.32f, 0.22f), lit);
            Part(root.transform, "Umbrella", PrimitiveType.Sphere, new Vector3(0f, 2.0f, 0f), new Vector3(1.15f, 0.5f, 1.15f), new Color(0.95f, 0.30f, 0.34f), lit);
            return root;
        }

        static void Part(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 scale, Color color, Shader lit)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            Transform t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = pos;
            t.localScale = scale;
            var mat = new Material(lit) { color = color };
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }
    }
}
