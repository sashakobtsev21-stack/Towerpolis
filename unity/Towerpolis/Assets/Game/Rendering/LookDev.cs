using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Towerpolis.Game.Rendering
{
    /// <summary>
    /// A code-driven look-dev pass that lifts the flat prototype look without any downloaded assets:
    /// a gradient skybox + matching trilight ambient, a warm key light with soft shadows + a cool fill,
    /// and a URP post-processing volume (tonemapping, gentle colour grading, bloom, vignette) with
    /// post-processing enabled on the camera. Self-bootstraps (the controller adds it). The
    /// rendering-engineer/technical-artist refine this (and add SSAO as a renderer feature) in Phase 6.
    /// Applies at runtime (Play); SSAO is the one bit that lives on the URP Renderer asset — see
    /// docs/ASSETS_GUIDE.md.
    /// </summary>
    public sealed class LookDev : MonoBehaviour
    {
        [Header("Sky / ambient")]
        [SerializeField] Color skyTop = new Color(0.20f, 0.46f, 0.83f);
        [SerializeField] Color skyHorizon = new Color(0.68f, 0.85f, 0.97f);
        [SerializeField] Color skyBottom = new Color(0.52f, 0.68f, 0.86f);
        [SerializeField] Color ambientSky = new Color(0.56f, 0.66f, 0.82f);
        [SerializeField] Color ambientEquator = new Color(0.70f, 0.71f, 0.72f);
        [SerializeField] Color ambientGround = new Color(0.42f, 0.40f, 0.37f);

        [Header("Lights")]
        [SerializeField] Color keyColor = new Color(1.00f, 0.96f, 0.86f);
        [SerializeField] float keyIntensity = 1.15f;
        [SerializeField] Vector3 keyAngles = new Vector3(50f, -35f, 0f);
        [SerializeField] Color fillColor = new Color(0.70f, 0.80f, 1.00f);
        [SerializeField] float fillIntensity = 0.35f;
        [SerializeField] Vector3 fillAngles = new Vector3(-20f, 150f, 0f);

        [Header("Post")]
        [SerializeField] float bloom = 0.5f;
        [SerializeField] float vignette = 0.28f;
        [SerializeField] float contrast = 12f;
        [SerializeField] float saturation = 8f;
        [SerializeField] float warmth = 8f;

        void Awake()
        {
            // If the look-dev was baked into the scene (Tools ▸ Towerpolis ▸ Bake Look-Dev), those
            // persistent objects already provide everything — don't duplicate them at runtime.
            if (GameObject.Find("LookDev Volume") != null) return;
            SetupSky();
            SetupLights();
            SetupPost();
            SetupCamera();
        }

        void SetupSky()
        {
            Shader sh = Shader.Find("Towerpolis/GradientSkybox");
            if (sh != null)
            {
                var sky = new Material(sh);
                sky.SetColor("_TopColor", skyTop);
                sky.SetColor("_HorizonColor", skyHorizon);
                sky.SetColor("_BottomColor", skyBottom);
                RenderSettings.skybox = sky;
            }
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky;
            RenderSettings.ambientEquatorColor = ambientEquator;
            RenderSettings.ambientGroundColor = ambientGround;
            DynamicGI.UpdateEnvironment();
        }

        void SetupLights()
        {
            Light key = null;
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { key = l; break; }
            if (key == null)
            {
                var g = new GameObject("Key Light");
                key = g.AddComponent<Light>();
                key.type = LightType.Directional;
            }
            key.color = keyColor;
            key.intensity = keyIntensity;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.55f;
            key.transform.rotation = Quaternion.Euler(keyAngles);

            var fillGo = new GameObject("LookDev Fill Light");
            var fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = fillColor;
            fill.intensity = fillIntensity;
            fill.shadows = LightShadows.None;
            fill.transform.rotation = Quaternion.Euler(fillAngles);
        }

        void SetupPost()
        {
            var go = new GameObject("LookDev Volume");
            var vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 100f;

            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            vol.profile = profile;

            var tone = profile.Add<Tonemapping>();
            tone.mode.Override(TonemappingMode.Neutral);

            var color = profile.Add<ColorAdjustments>();
            color.postExposure.Override(0.04f);
            color.contrast.Override(contrast);
            color.saturation.Override(saturation);

            var wb = profile.Add<WhiteBalance>();
            wb.temperature.Override(warmth);

            var bl = profile.Add<Bloom>();
            bl.intensity.Override(bloom);
            bl.threshold.Override(1.0f);
            bl.scatter.Override(0.6f);

            var vig = profile.Add<Vignette>();
            vig.intensity.Override(vignette);
            vig.smoothness.Override(0.4f);
        }

        void SetupCamera()
        {
            var cam = Camera.main != null ? Camera.main : FindFirstObjectByType<Camera>();
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.Skybox; // so the gradient sky shows (not a flat fill)
            var data = cam.GetUniversalAdditionalCameraData();
            if (data != null)
            {
                data.renderPostProcessing = true;
                data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            }
        }
    }
}
