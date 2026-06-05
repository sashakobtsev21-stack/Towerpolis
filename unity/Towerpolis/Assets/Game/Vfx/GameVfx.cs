using UnityEngine;
using Towerpolis.Game.Gameplay;

namespace Towerpolis.Game.Vfx
{
    /// <summary>
    /// Visual juice off the <see cref="TowerGameController"/> events, all built in code (no prefabs/wiring):
    /// a dust puff at the seam on every landed floor, a colourful confetti burst + a small camera punch on
    /// a Perfect, and a camera shake on a miss / topple. Self-bootstraps (the controller adds it). This is
    /// the temporary MVP juice; the vfx-artist replaces it with authored VFX Graph effects later.
    /// </summary>
    public sealed class GameVfx : MonoBehaviour
    {
        [SerializeField] int dustCount = 16;
        [SerializeField] int confettiCount = 50;
        [Tooltip("Height above the placed floor's base where confetti bursts (so it pops above the roof).")]
        [SerializeField] float confettiHeight = 2.0f;
        [Range(0f, 1f)] [SerializeField] float perfectShake = 0.22f;
        [Range(0f, 1f)] [SerializeField] float missShake = 0.4f;
        [Range(0f, 1f)] [SerializeField] float toppleShake = 0.8f;

        TowerGameController _controller;
        CameraRig _cameraRig;
        ParticleSystem _dust, _confetti;       // built-in placeholder effects
        ParticleSystem _comboBurst, _comboGlow; // Phase B: combo tier-up sparks + "on fire" hold glow
        GameObject _dustPrefab, _confettiPrefab; // optional downloaded effects (Resources/Vfx/<name>)
        int _lastComboLevel;                    // to detect tier up vs decay between placements

        // Combo palette warms with the tier (level 0..3); a Good-decay scatters cool blue (contrast).
        static readonly Color[] ComboTierColors =
        {
            new Color(0.97f, 0.97f, 0.95f, 1f), // 0 — off-white (unused: no burst at 0)
            new Color(1.00f, 0.84f, 0.31f, 1f), // 1 — gold
            new Color(1.00f, 0.60f, 0.14f, 1f), // 2 — orange
            new Color(1.00f, 0.42f, 0.05f, 1f), // 3 — amber-red ("on fire")
        };
        static readonly int[] ComboBurstCounts = { 0, 6, 10, 16 }; // ring sparks by tier reached
        static readonly Color ComboDecayColor = new Color(0.42f, 0.72f, 1.00f, 0.9f);

        void Awake()
        {
            // Prefer a downloaded effect prefab if you dropped one in Resources/Vfx; else use the
            // built-in procedural placeholder. See docs/ASSETS_GUIDE.md.
            _dustPrefab = Resources.Load<GameObject>("Vfx/dust");
            _confettiPrefab = Resources.Load<GameObject>("Vfx/confetti");

            var mat = MakeParticleMaterial(MakeDotTexture());
            if (_dustPrefab == null) { _dust = BuildDust(mat); _dust.Play(); }
            if (_confettiPrefab == null) { _confetti = BuildConfetti(mat); _confetti.Play(); }
            _comboBurst = BuildComboBurst(mat); _comboBurst.Play();
            _comboGlow = BuildComboGlow(mat); _comboGlow.Play(); // emits only while rateOverTime > 0 (tier 3)
            // Playing with emission rate 0 → nothing auto-spawns, but manual Emit() bursts simulate.
        }

        void OnEnable() => Bind();
        void Start() => Bind();
        void OnDisable() => Unbind();

        void Bind()
        {
            if (_controller != null) return;
            _controller = FindFirstObjectByType<TowerGameController>();
            if (_controller == null) return;
            _cameraRig = FindFirstObjectByType<CameraRig>();
            _controller.FloorPlacedAt += OnPlaced;
            _controller.StrikeAdded += OnStrike;
            _controller.RunToppled += OnToppled;
            _controller.RunStarted += OnRunStarted;
        }

        void Unbind()
        {
            if (_controller == null) return;
            _controller.FloorPlacedAt -= OnPlaced;
            _controller.StrikeAdded -= OnStrike;
            _controller.RunToppled -= OnToppled;
            _controller.RunStarted -= OnRunStarted;
            _controller = null;
        }

        void OnPlaced(Vector3 basePos, bool perfect, int residents)
        {
            EmitDust(basePos);
            if (perfect)
            {
                EmitConfetti(basePos + Vector3.up * confettiHeight); // above the roof so it's clearly visible
                Shake(perfectShake);
            }
            ComboFx(basePos);
        }

        // Spark feedback for the combo state, read after Core has updated it (Phase B).
        void ComboFx(Vector3 basePos)
        {
            if (_controller == null) return;
            int level = _controller.ComboLevel; // 0..3
            Vector3 top = basePos + Vector3.up * confettiHeight;

            if (level > _lastComboLevel)        // tier up → a ring of warm sparks (bigger at higher tiers)
                BurstColored(_comboBurst, top, ComboBurstCounts[Mathf.Clamp(level, 0, 3)], ComboTierColors[Mathf.Clamp(level, 0, 3)]);
            else if (level < _lastComboLevel)   // decay on a Good → a small cool-blue step-down scatter
                BurstColored(_comboBurst, top, 5, ComboDecayColor);

            SetGlow(level == 3, top); // persistent "on fire" glow only at max
            _lastComboLevel = level;
        }

        void OnStrike(int strikes)
        {
            Shake(missShake);
            SetGlow(false, Vector3.zero); // a strike breaks the combo → kill the glow (Core already zeroed it)
            _lastComboLevel = 0;
        }

        void OnToppled() { Shake(toppleShake); SetGlow(false, Vector3.zero); _lastComboLevel = 0; }
        void OnRunStarted() { SetGlow(false, Vector3.zero); _lastComboLevel = 0; }

        void SetGlow(bool on, Vector3 pos)
        {
            if (_comboGlow == null) return;
            if (on) _comboGlow.transform.position = pos;
            var emission = _comboGlow.emission;
            emission.rateOverTime = on ? 2f : 0f; // subtle ambient stream while held
        }

        void Shake(float magnitude)
        {
            if (_cameraRig != null) _cameraRig.Shake(magnitude);
        }

        void EmitDust(Vector3 pos)
        {
            if (_dustPrefab != null) PlayPrefab(_dustPrefab, pos);
            else Burst(_dust, pos, dustCount);
        }

        void EmitConfetti(Vector3 pos)
        {
            if (_confettiPrefab != null) PlayPrefab(_confettiPrefab, pos);
            else Burst(_confetti, pos, confettiCount);
        }

        // Spawn a downloaded effect prefab, play all its particle systems, and clean it up.
        static void PlayPrefab(GameObject prefab, Vector3 pos)
        {
            var go = Instantiate(prefab, pos, prefab.transform.rotation);
            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>()) ps.Play();
            Destroy(go, 6f);
        }

        static readonly Quaternion EmitUp = Quaternion.Euler(-90f, 0f, 0f); // hemisphere/cone fire toward +Y world

        static void Burst(ParticleSystem ps, Vector3 worldPos, int count)
        {
            if (ps == null) return;
            ps.transform.SetPositionAndRotation(worldPos, EmitUp); // fix orientation no matter the parent
            ps.Emit(count);
        }

        // Burst at a per-emit colour (combo tier sparks). Identity rotation → the Circle ring faces the camera.
        static void BurstColored(ParticleSystem ps, Vector3 worldPos, int count, Color color)
        {
            if (ps == null || count <= 0) return;
            ps.transform.SetPositionAndRotation(worldPos, Quaternion.identity);
            var ep = new ParticleSystem.EmitParams { startColor = color };
            ps.Emit(ep, count);
        }

        // --- procedural particle systems ---

        ParticleSystem BuildDust(Material mat)
        {
            var go = new GameObject("Dust");
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // hemisphere opens up (+Y world)
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.45f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.16f, 0.38f);
            main.startColor = new Color(0.95f, 0.93f, 0.87f, 0.75f);
            main.gravityModifier = 0.25f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 300;

            var emission = ps.emission; emission.enabled = true; emission.rateOverTime = 0f; // burst via Emit()
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Hemisphere;
            shape.radius = 0.7f;

            var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeOut(new Color(0.95f, 0.93f, 0.87f), 0.0f);
            var sol = ps.sizeOverLifetime; sol.enabled = true; sol.size = new ParticleSystem.MinMaxCurve(1f, Grow());

            Finish(go, mat);
            return ps;
        }

        ParticleSystem BuildConfetti(Material mat)
        {
            var go = new GameObject("Confetti");
            go.transform.SetParent(transform, false);
            go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f); // cone fires up (+Y world)
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.9f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.0f, 5.5f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.26f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 2f * Mathf.PI);
            main.gravityModifier = 1.1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 600;
            var colorGrad = new ParticleSystem.MinMaxGradient(ConfettiGradient()) { mode = ParticleSystemGradientMode.RandomColor };
            main.startColor = colorGrad;

            var emission = ps.emission; emission.enabled = true; emission.rateOverTime = 0f;
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 38f; // wider fountain so it spreads visibly
            shape.radius = 0.12f;

            var rot = ps.rotationOverLifetime; rot.enabled = true; rot.z = new ParticleSystem.MinMaxCurve(-4f, 4f); // flutter
            var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeOut(Color.white, 0.7f); // hold colour, fade tail

            Finish(go, mat);
            return ps;
        }

        // Phase B: a quick ring of sparks on a combo tier-up (colour/count set per emit, world space).
        ParticleSystem BuildComboBurst(Material mat)
        {
            var go = new GameObject("ComboBurst");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = 0.5f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.8f, 3.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.16f);
            main.startColor = Color.white; // overridden per emit by BurstColored
            main.gravityModifier = -0.2f;  // sparks drift up a touch
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 40;

            var emission = ps.emission; emission.enabled = true; emission.rateOverTime = 0f; // burst via Emit()
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.25f;
            shape.radiusThickness = 0f; // emit from the shell → a ring that expands outward

            // colour comes from the per-emit startColor; this just fades the alpha to 0 over life.
            var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeOut(Color.white, 0f);
            Finish(go, mat);
            return ps;
        }

        // Phase B: a subtle, cheap upward shimmer held while the combo is at max ("on fire").
        ParticleSystem BuildComboGlow(Material mat)
        {
            var go = new GameObject("ComboGlow");
            go.transform.SetParent(transform, false);
            var ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = true;
            main.playOnAwake = false;
            main.startLifetime = 1.2f;
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 0.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.11f);
            main.startColor = new Color(1.00f, 0.55f, 0.10f, 0.6f);
            main.gravityModifier = -0.3f; // negative gravity → drift upward
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 6;

            var emission = ps.emission; emission.enabled = true; emission.rateOverTime = 0f; // toggled on at tier 3
            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.12f;

            var col = ps.colorOverLifetime; col.enabled = true; col.color = FadeOut(new Color(1f, 0.55f, 0.10f), 0f);
            Finish(go, mat);
            return ps;
        }

        static void Finish(GameObject go, Material mat)
        {
            var rend = go.GetComponent<ParticleSystemRenderer>();
            rend.material = mat;
            rend.renderMode = ParticleSystemRenderMode.Billboard;
            rend.sortMode = ParticleSystemSortMode.None;
        }

        // --- assets built in code ---

        static Texture2D MakeDotTexture()
        {
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[s * s];
            float r = s * 0.5f;
            for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float dx = x - r + 0.5f, dy = y - r + 0.5f;
                    float d = Mathf.Sqrt(dx * dx + dy * dy) / r;
                    float a = Mathf.Clamp01(1f - d); a *= a;
                    px[y * s + x] = new Color32(255, 255, 255, (byte)(a * 255f));
                }
            tex.SetPixels32(px);
            tex.Apply();
            return tex;
        }

        static Material MakeParticleMaterial(Texture2D tex)
        {
            Shader sh = Shader.Find("Sprites/Default"); // alpha-blended, uses particle vertex colour × texture
            if (sh == null) sh = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            return new Material(sh) { mainTexture = tex };
        }

        static Gradient ConfettiGradient()
        {
            var g = new Gradient();
            g.SetKeys(new[]
            {
                new GradientColorKey(new Color(0.95f, 0.26f, 0.30f), 0.00f), // red
                new GradientColorKey(new Color(1.00f, 0.70f, 0.15f), 0.20f), // orange/yellow
                new GradientColorKey(new Color(0.30f, 0.82f, 0.40f), 0.45f), // green
                new GradientColorKey(new Color(0.20f, 0.60f, 1.00f), 0.70f), // blue
                new GradientColorKey(new Color(0.95f, 0.45f, 0.85f), 1.00f), // pink
            }, new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            return g;
        }

        static ParticleSystem.MinMaxGradient FadeOut(Color c, float holdUntil)
        {
            var g = new Gradient();
            var alpha = holdUntil > 0f
                ? new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, holdUntil), new GradientAlphaKey(0f, 1f) }
                : new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) };
            g.SetKeys(new[] { new GradientColorKey(c, 0f), new GradientColorKey(c, 1f) }, alpha);
            return new ParticleSystem.MinMaxGradient(g);
        }

        static AnimationCurve Grow() => AnimationCurve.EaseInOut(0f, 0.6f, 1f, 1.4f);
    }
}
