using UnityEngine;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// Drives the sky/ambient for the active district AND the atmospheric ascent (GDD §4.9): the sky
    /// starts at the district's ground gradient and lerps toward space as the tower climbs. Clones the
    /// skybox material once and swaps it in, so the baked GradientSky.mat asset is never edited.
    /// </summary>
    public static class DistrictSky
    {
        const float FullAscentFloors = 90f; // height at which the sky reaches full "space"

        static readonly Color SpaceTop = new Color(0.012f, 0.012f, 0.05f);
        static readonly Color SpaceHorizon = new Color(0.06f, 0.05f, 0.16f);
        static readonly Color SpaceBottom = new Color(0.02f, 0.02f, 0.08f);

        static Material _runtime;
        static Color _gTop, _gHorizon, _gBottom; // the active district's ground sky

        /// <summary>Set the active district's ground sky (call on run start). Resets the ascent to 0.</summary>
        public static void SetDistrict(in DistrictTheme t)
        {
            EnsureRuntime();
            _gTop = t.SkyTop;
            _gHorizon = t.SkyHorizon;
            _gBottom = t.SkyBottom;
            UpdateAltitude(0);
        }

        /// <summary>Blend the sky/ambient toward space for the current floor count.</summary>
        public static void UpdateAltitude(int floors)
        {
            if (_runtime == null) EnsureRuntime();
            if (_runtime == null) return;

            float t = Mathf.Clamp01(floors / FullAscentFloors);
            Color top = Color.Lerp(_gTop, SpaceTop, t);
            Color horizon = Color.Lerp(_gHorizon, SpaceHorizon, t);
            Color bottom = Color.Lerp(_gBottom, SpaceBottom, t);

            SetCol("_TopColor", top);
            SetCol("_HorizonColor", horizon);
            SetCol("_BottomColor", bottom);

            // Ambient follows the sky so the scene lighting darkens as you climb (no DynamicGI per call).
            RenderSettings.ambientSkyColor = horizon;
            RenderSettings.ambientEquatorColor = Color.Lerp(horizon, bottom, 0.5f) * 0.85f;
            RenderSettings.ambientGroundColor = bottom * 0.7f;
        }

        /// <summary>0 (ground) → 1 (space) for the given floor count — used to dim the key light too.</summary>
        public static float Altitude(int floors) => Mathf.Clamp01(floors / FullAscentFloors);

        static void EnsureRuntime()
        {
            if (_runtime != null && RenderSettings.skybox == _runtime) return;

            // Own a gradient skybox material we fully control (the scene's skybox may be a different
            // shader without _TopColor — then tinting would silently do nothing).
            Shader sh = Shader.Find("Towerpolis/GradientSkybox");
            if (sh != null)
            {
                _runtime = new Material(sh);
            }
            else
            {
                Material cur = RenderSettings.skybox;
                if (cur == null) return;
                _runtime = new Material(cur);
            }
            RenderSettings.skybox = _runtime;

            // Make sure the camera actually draws the skybox (not a solid colour).
            Camera cam = Camera.main;
            if (cam != null) cam.clearFlags = CameraClearFlags.Skybox;
        }

        static void SetCol(string prop, Color c)
        {
            if (_runtime.HasProperty(prop)) _runtime.SetColor(prop, c);
        }
    }
}
