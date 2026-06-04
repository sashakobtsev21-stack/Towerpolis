using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Towerpolis.Game.Editor
{
    /// <summary>
    /// Bakes the look-dev (gradient sky, warm key + cool fill lights, post-processing Volume) as
    /// PERSISTENT scene objects + saved assets, so it shows in the editor WITHOUT pressing Play and is
    /// fully tweakable in the inspector. Idempotent — re-run to refresh. The runtime
    /// <see cref="Towerpolis.Game.Rendering.LookDev"/> detects the baked "LookDev Volume" and skips
    /// itself, so there's no duplication. Remember to save the scene (Ctrl+S) afterwards.
    /// </summary>
    public static class LookDevBaker
    {
        const string Dir = "Assets/Game/Rendering";
        const string SkyPath = Dir + "/GradientSky.mat";
        const string ProfilePath = Dir + "/LookDevProfile.asset";

        [MenuItem("Tools/Towerpolis/Bake Look-Dev Into Scene")]
        public static void Bake()
        {
            BakeSky();
            BakeLights();
            var profile = BakeProfile();
            BakeVolume(profile);
            BakeCamera();

            DynamicGI.UpdateEnvironment();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[Towerpolis] Look-dev baked into the scene. Save the scene (Ctrl+S) to keep it. " +
                      "Tweak the 'Key Light' / 'LookDev Fill Light' / 'LookDev Volume' objects in the Hierarchy.");
        }

        static void BakeSky()
        {
            var shader = Shader.Find("Towerpolis/GradientSkybox");
            if (shader == null) { Debug.LogWarning("[Towerpolis] GradientSkybox shader not found — sky skipped."); return; }

            var sky = AssetDatabase.LoadAssetAtPath<Material>(SkyPath);
            if (sky == null) { sky = new Material(shader); AssetDatabase.CreateAsset(sky, SkyPath); }
            else sky.shader = shader;
            sky.SetColor("_TopColor", new Color(0.27f, 0.50f, 0.85f));
            sky.SetColor("_HorizonColor", new Color(0.86f, 0.91f, 0.97f));
            sky.SetColor("_BottomColor", new Color(0.55f, 0.56f, 0.60f));
            EditorUtility.SetDirty(sky);

            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.56f, 0.66f, 0.82f);
            RenderSettings.ambientEquatorColor = new Color(0.70f, 0.71f, 0.72f);
            RenderSettings.ambientGroundColor = new Color(0.42f, 0.40f, 0.37f);
        }

        static void BakeLights()
        {
            Light key = UnityEngine.Object
                .FindObjectsByType<Light>(FindObjectsSortMode.None)
                .FirstOrDefault(l => l.type == LightType.Directional);
            if (key == null) key = new GameObject("Key Light").AddComponent<Light>();
            key.type = LightType.Directional;
            key.color = new Color(1.00f, 0.96f, 0.86f);
            key.intensity = 1.15f;
            key.shadows = LightShadows.Soft;
            key.shadowStrength = 0.55f;
            key.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

            // NB: don't use ?? with Unity objects — it bypasses Unity's overloaded == (fake-null), which
            // is exactly what produced the MissingComponent error. Use explicit checks / TryGetComponent.
            var fillGo = GameObject.Find("LookDev Fill Light");
            if (fillGo == null) fillGo = new GameObject("LookDev Fill Light");
            if (!fillGo.TryGetComponent(out Light fill)) fill = fillGo.AddComponent<Light>();
            fill.type = LightType.Directional;
            fill.color = new Color(0.70f, 0.80f, 1.00f);
            fill.intensity = 0.35f;
            fill.shadows = LightShadows.None;
            fillGo.transform.rotation = Quaternion.Euler(-20f, 150f, 0f);
        }

        static VolumeProfile BakeProfile()
        {
            AssetDatabase.DeleteAsset(ProfilePath); // start fresh so re-bakes don't pile up sub-assets
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);

            Add<Tonemapping>(profile, t => t.mode.Override(TonemappingMode.Neutral));
            Add<ColorAdjustments>(profile, c =>
            {
                c.postExposure.Override(0.04f);
                c.contrast.Override(12f);
                c.saturation.Override(8f);
            });
            Add<WhiteBalance>(profile, w => w.temperature.Override(8f));
            Add<Bloom>(profile, b =>
            {
                b.intensity.Override(0.5f);
                b.threshold.Override(1.0f);
                b.scatter.Override(0.6f);
            });
            Add<Vignette>(profile, v =>
            {
                v.intensity.Override(0.28f);
                v.smoothness.Override(0.4f);
            });

            AssetDatabase.SaveAssets();
            return profile;
        }

        static void BakeVolume(VolumeProfile profile)
        {
            var go = GameObject.Find("LookDev Volume");
            if (go == null) go = new GameObject("LookDev Volume");
            if (!go.TryGetComponent(out Volume vol)) vol = go.AddComponent<Volume>();
            vol.isGlobal = true;
            vol.priority = 100f;
            vol.sharedProfile = profile;
        }

        static void BakeCamera()
        {
            var cam = Camera.main != null ? Camera.main : UnityEngine.Object.FindFirstObjectByType<Camera>();
            if (cam == null) return;
            cam.clearFlags = CameraClearFlags.Skybox;
            var data = cam.GetUniversalAdditionalCameraData();
            if (data == null) return;
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
        }

        static void Add<T>(VolumeProfile profile, Action<T> configure) where T : VolumeComponent
        {
            var component = profile.Add<T>();
            configure(component);
            AssetDatabase.AddObjectToAsset(component, profile);
        }
    }
}
