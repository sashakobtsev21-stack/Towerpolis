using UnityEngine;
using Towerpolis.Game.Gameplay;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// A multi-layer animated backdrop for the atmospheric ascent (GDD §4.9): a stack of parallax layers
    /// (city silhouette → clouds → balloons → planes → aurora → stars → moon), each fading in/out over its
    /// own altitude band, with drift/twinkle. Flat layers parented to the camera, behind the tower and over
    /// the skybox. Procedural placeholders by default; drop a Texture2D into
    /// <c>Resources/Background/&lt;name&gt;</c> (cloud, city, balloon, plane, aurora, star, moon) to replace
    /// any layer with your own art (drop-in, no wiring). Self-bootstraps off the controller.
    /// </summary>
    public sealed class BackgroundLayer : MonoBehaviour
    {
        const float Distance = 45f;
        const float AscentFloors = 30f; // matches the DistrictSky test value

        enum Kind { Blob, Star, City, Aurora }

        struct Def
        {
            public string Res;
            public Kind Kind;
            public float InA, InB, OutA, OutB;     // altitude fade band (0..1)
            public int Count;
            public float SizeMin, SizeMax;
            public float DriftMin, DriftMax;       // horizontal drift (units/s); 0 = static
            public float YMin, YMax, XSpread;      // placement
            public bool Twinkle;
            public Color Tint;                     // rgb + peak alpha
            public float Depth;                    // local z (farther = more behind)
        }

        static readonly Def[] Defs =
        {
            // name      kind          in    inB   outA  outB   n   szmin szmax dmin dmax  ymin  ymax  xspr  twk  tint(rgba)                              depth
            D("city",    Kind.City,    0f,   0f,   0.12f,0.22f, 2,  48f,  48f,  0f,  0f,  -32f, -26f, 0f,  false, new Color(0.10f,0.12f,0.18f,0.95f), 3.0f),
            D("cloud",   Kind.Blob,    0.04f,0.18f,0.42f,0.58f, 8,  11f,  20f,  1.0f,2.6f,-10f, 16f, 22f, false, new Color(1f,1f,1f,0.55f),          1.6f),
            D("balloon", Kind.Blob,    0.06f,0.18f,0.34f,0.44f, 5,  2.4f, 3.6f, 0.4f,1.0f,-6f,  16f, 20f, false, new Color(1f,0.85f,0.55f,0.85f),    1.3f),
            D("plane",   Kind.Blob,    0.40f,0.50f,0.66f,0.76f, 3,  1.8f, 2.6f, 3.2f,5.0f,-2f,  18f, 22f, false, new Color(0.85f,0.88f,0.95f,0.9f),  1.1f),
            D("aurora",  Kind.Aurora,  0.58f,0.70f,0.88f,0.96f, 2,  44f,  44f,  0.3f,0.6f, 12f, 24f, 0f,  false, new Color(0.35f,0.95f,0.6f,0.4f),   2.6f),
            D("star",    Kind.Star,    0.48f,0.62f,1.2f, 1.3f,  52, 0.5f, 1.3f, 0f,  0f,  -28f, 28f, 24f, true,  new Color(1f,1f,0.97f,1f),          3.2f),
            D("moon",    Kind.Blob,    0.80f,0.90f,1.2f, 1.3f,  1,  9f,   9f,   0f,  0f,   18f, 22f, 0f,  false, new Color(0.95f,0.95f,0.88f,0.95f), 3.1f),
        };

        static Def D(string res, Kind kind, float inA, float inB, float outA, float outB, int count,
            float szMin, float szMax, float dMin, float dMax, float yMin, float yMax, float xSpr,
            bool twk, Color tint, float depth) => new Def
        {
            Res = res, Kind = kind, InA = inA, InB = inB, OutA = outA, OutB = outB, Count = count,
            SizeMin = szMin, SizeMax = szMax, DriftMin = dMin, DriftMax = dMax, YMin = yMin, YMax = yMax,
            XSpread = xSpr, Twinkle = twk, Tint = tint, Depth = depth,
        };

        sealed class Runtime
        {
            public Def Def;
            public Material Mat;
            public Transform[] Items;
            public float[] Speed, Phase, BaseSize;
        }

        TowerGameController _controller;
        Transform _root;
        Runtime[] _layers;
        Texture2D _blob, _star, _city, _aurora;

        void Start()
        {
            _controller = FindFirstObjectByType<TowerGameController>();
            Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam == null) { enabled = false; return; }

            _root = new GameObject("BackgroundLayer").transform;
            _root.SetParent(cam.transform, false);
            _root.localPosition = new Vector3(0f, 0f, Distance);
            _root.localRotation = Quaternion.identity;

            _layers = new Runtime[Defs.Length];
            int seed = 1;
            for (int d = 0; d < Defs.Length; d++)
                _layers[d] = BuildLayer(Defs[d], ref seed);
        }

        void Update()
        {
            float t = _controller != null ? Mathf.Clamp01(_controller.Floors / AscentFloors) : 0f;
            float time = Time.time;
            float dt = Time.deltaTime;

            foreach (Runtime L in _layers)
            {
                if (L == null) continue;
                float a = Alpha(t, L.Def) * L.Def.Tint.a;
                if (L.Mat != null) L.Mat.color = new Color(L.Def.Tint.r, L.Def.Tint.g, L.Def.Tint.b, a);

                for (int i = 0; i < L.Items.Length; i++)
                {
                    Transform it = L.Items[i];
                    if (it == null) continue;
                    if (L.Speed[i] != 0f)
                    {
                        Vector3 p = it.localPosition;
                        p.x += L.Speed[i] * dt;
                        float bound = (L.Def.XSpread > 0f ? L.Def.XSpread : 24f) + L.BaseSize[i];
                        if (p.x > bound) p.x -= bound * 2f;
                        else if (p.x < -bound) p.x += bound * 2f;
                        it.localPosition = p;
                    }
                    if (L.Def.Twinkle)
                    {
                        float s = L.BaseSize[i] * (0.65f + 0.35f * Mathf.Sin(time * 2.4f + L.Phase[i]));
                        it.localScale = new Vector3(s, s, s);
                    }
                }
            }
        }

        Runtime BuildLayer(Def def, ref int seed)
        {
            var L = new Runtime
            {
                Def = def,
                Mat = MakeMaterial(def),
                Items = new Transform[def.Count],
                Speed = new float[def.Count],
                Phase = new float[def.Count],
                BaseSize = new float[def.Count],
            };
            for (int i = 0; i < def.Count; i++)
            {
                float h1 = Hash(seed++), h2 = Hash(seed++), h3 = Hash(seed++), h4 = Hash(seed++);
                float size = Mathf.Lerp(def.SizeMin, def.SizeMax, h1);
                float x = def.XSpread > 0f ? (h2 - 0.5f) * def.XSpread * 2f : (def.Count > 1 ? (i / (float)(def.Count - 1) - 0.5f) * 30f : 8f);
                float y = Mathf.Lerp(def.YMin, def.YMax, h3);
                L.BaseSize[i] = size;
                L.Phase[i] = h4 * 6.283f;
                L.Speed[i] = def.DriftMax > 0f ? Mathf.Lerp(def.DriftMin, def.DriftMax, h2) * (h3 > 0.5f ? 1f : -1f) : 0f;
                L.Items[i] = MakeQuad(L.Mat, new Vector3(x, y, def.Depth), size);
            }
            return L;
        }

        Transform MakeQuad(Material mat, Vector3 localPos, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = "BgSprite";
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var t = go.transform;
            t.SetParent(_root, false);
            t.localPosition = localPos;
            t.localScale = new Vector3(size, size, 1f);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            return t;
        }

        Material MakeMaterial(Def def)
        {
            Texture2D tex = Resources.Load<Texture2D>("Background/" + def.Res);
            if (tex == null) tex = TextureFor(def.Kind);
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            return new Material(sh) { mainTexture = tex, color = def.Tint };
        }

        Texture2D TextureFor(Kind k) => k switch
        {
            Kind.Star => _star != null ? _star : (_star = SoftBlob(24, 3.2f)),
            Kind.City => _city != null ? _city : (_city = CitySilhouette()),
            Kind.Aurora => _aurora != null ? _aurora : (_aurora = AuroraBand()),
            _ => _blob != null ? _blob : (_blob = SoftBlob(72, 1.1f)),
        };

        // --- procedural placeholder textures ---

        static Texture2D SoftBlob(int size, float exponent)
        {
            var tex = NewTex(size, size);
            var px = new Color32[size * size];
            float r = size * 0.5f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f, dy = y - r + 0.5f;
                    float d = Mathf.Clamp01(Mathf.Sqrt(dx * dx + dy * dy) / r);
                    float a = Mathf.Pow(Mathf.Clamp01(1f - d), exponent);
                    px[y * size + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static Texture2D CitySilhouette()
        {
            int w = 256, h = 64;
            var tex = NewTex(w, h);
            var px = new Color32[w * h]; // transparent
            int x = 0, b = 1;
            while (x < w)
            {
                int bw = 10 + (int)(Hash(b++) * 22f);
                int bh = 14 + (int)(Hash(b++) * 42f);
                for (int yy = 0; yy < bh && yy < h; yy++)
                    for (int xx = x; xx < x + bw && xx < w; xx++)
                        px[yy * w + xx] = new Color32(255, 255, 255, 255);
                x += bw + 3 + (int)(Hash(b++) * 5f);
            }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static Texture2D AuroraBand()
        {
            int w = 128, h = 64;
            var tex = NewTex(w, h);
            var px = new Color32[w * h];
            for (int y = 0; y < h; y++)
            {
                float vy = Mathf.Sin((y / (float)h) * Mathf.PI); // soft top/bottom fade
                for (int x = 0; x < w; x++)
                {
                    float vx = 0.6f + 0.4f * Mathf.Sin(x / (float)w * 6.283f * 1.5f);
                    byte a = (byte)(Mathf.Clamp01(vy * vx) * 255f);
                    px[y * w + x] = new Color32(255, 255, 255, a);
                }
            }
            tex.SetPixels32(px); tex.Apply();
            return tex;
        }

        static Texture2D NewTex(int w, int h) =>
            new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };

        // --- helpers ---

        static float Alpha(float t, Def d)
        {
            if (t <= d.InA || t >= d.OutB) return 0f;
            if (t < d.InB) return Mathf.InverseLerp(d.InA, d.InB, t);
            if (t < d.OutA) return 1f;
            return 1f - Mathf.InverseLerp(d.OutA, d.OutB, t);
        }

        static float Hash(int n)
        {
            n = (n << 13) ^ n;
            int h = n * (n * n * 15731 + 789221) + 1376312589;
            return ((h & 0x7fffffff) % 1000) / 1000f;
        }
    }
}
