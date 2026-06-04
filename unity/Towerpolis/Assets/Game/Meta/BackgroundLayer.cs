using UnityEngine;
using Towerpolis.Game.Gameplay;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// A simple animated backdrop behind the tower (part of the atmospheric ascent, GDD §4.9): drifting
    /// clouds that peak mid-climb and twinkling stars that fade in toward space. Lives as a flat layer
    /// parented to the camera, behind the tower (occluded by it) and over the skybox. Procedural by
    /// default; drop your own art into <c>Resources/Background/cloud</c> and <c>Resources/Background/star</c>
    /// (Texture2D) to replace the placeholders. Self-bootstraps off the controller.
    /// </summary>
    public sealed class BackgroundLayer : MonoBehaviour
    {
        const int CloudCount = 9;
        const int StarCount = 48;
        const float Distance = 45f;     // backdrop distance in front of the camera
        const float SpanX = 90f;        // horizontal extent (wrap width for clouds)
        const float SpanY = 110f;       // vertical extent

        TowerGameController _controller;
        Transform _root;
        Material _cloudMat, _starMat;
        Color _cloudBase = new Color(1f, 1f, 1f, 1f);
        Color _starBase = new Color(1f, 1f, 0.96f, 1f);

        Transform[] _clouds;
        float[] _cloudSpeed;
        Transform[] _stars;
        float[] _starPhase, _starSize;

        void Start()
        {
            _controller = FindFirstObjectByType<TowerGameController>();
            Camera cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam == null) { enabled = false; return; }

            _root = new GameObject("BackgroundLayer").transform;
            _root.SetParent(cam.transform, false);
            _root.localPosition = new Vector3(0f, 0f, Distance);
            _root.localRotation = Quaternion.identity;

            _cloudMat = MakeMaterial("Background/cloud", SoftBlob(72, 0.9f), _cloudBase);
            _starMat = MakeMaterial("Background/star", SoftBlob(32, 3.0f), _starBase);

            BuildClouds();
            BuildStars();
        }

        void Update()
        {
            float t = _controller != null ? Altitude(_controller.Floors) : 0f;
            float cloudAlpha = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * 0.55f; // peaks mid-climb
            float starAlpha = Mathf.Clamp01((t - 0.45f) * 2.2f);               // fade in toward space

            if (_cloudMat != null) _cloudMat.color = new Color(1f, 1f, 1f, cloudAlpha);
            if (_starMat != null) _starMat.color = new Color(_starBase.r, _starBase.g, _starBase.b, starAlpha);

            // Drift clouds horizontally (wrap).
            if (_clouds != null)
                for (int i = 0; i < _clouds.Length; i++)
                {
                    if (_clouds[i] == null) continue;
                    Vector3 p = _clouds[i].localPosition;
                    p.x += _cloudSpeed[i] * Time.deltaTime;
                    if (p.x > SpanX * 0.5f) p.x -= SpanX;
                    else if (p.x < -SpanX * 0.5f) p.x += SpanX;
                    _clouds[i].localPosition = p;
                }

            // Twinkle stars (pulse size).
            if (_stars != null)
                for (int i = 0; i < _stars.Length; i++)
                {
                    if (_stars[i] == null) continue;
                    float s = _starSize[i] * (0.7f + 0.3f * Mathf.Sin(Time.time * 2.2f + _starPhase[i]));
                    _stars[i].localScale = new Vector3(s, s, s);
                }
        }

        void BuildClouds()
        {
            _clouds = new Transform[CloudCount];
            _cloudSpeed = new float[CloudCount];
            for (int i = 0; i < CloudCount; i++)
            {
                float fx = (i / (float)CloudCount - 0.5f) * SpanX;
                float fy = (Hash(i * 7 + 1) - 0.5f) * SpanY * 0.5f + SpanY * 0.05f;
                float size = 14f + Hash(i * 13 + 3) * 16f;
                _clouds[i] = MakeQuad(_cloudMat, new Vector3(fx, fy, 0.5f), size);
                _cloudSpeed[i] = (1.0f + Hash(i * 5 + 2) * 1.8f) * (Hash(i) > 0.5f ? 1f : -1f);
            }
        }

        void BuildStars()
        {
            _stars = new Transform[StarCount];
            _starPhase = new float[StarCount];
            _starSize = new float[StarCount];
            for (int i = 0; i < StarCount; i++)
            {
                float fx = (Hash(i * 3 + 11) - 0.5f) * SpanX;
                float fy = (Hash(i * 9 + 17) - 0.5f) * SpanY;
                _starSize[i] = 0.6f + Hash(i * 2 + 4) * 1.0f;
                _stars[i] = MakeQuad(_starMat, new Vector3(fx, fy, 1.0f), _starSize[i]);
                _starPhase[i] = Hash(i * 19 + 5) * 6.28f;
            }
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

        static Material MakeMaterial(string resourcePath, Texture2D fallback, Color color)
        {
            Texture2D tex = Resources.Load<Texture2D>(resourcePath);
            if (tex == null) tex = fallback;
            Shader sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Unlit");
            return new Material(sh) { mainTexture = tex, color = color };
        }

        // A soft radial blob (alpha falls off from centre); higher exponent → sharper (stars), lower → fluffy.
        static Texture2D SoftBlob(int size, float exponent)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
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
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        // Deterministic 0..1 hash (no Random — keeps the layout stable per build).
        static float Hash(int n)
        {
            n = (n << 13) ^ n;
            int h = n * (n * n * 15731 + 789221) + 1376312589;
            return ((h & 0x7fffffff) % 1000) / 1000f;
        }

        static float Altitude(int floors) => Mathf.Clamp01(floors / 30f); // matches DistrictSky test value
    }
}
