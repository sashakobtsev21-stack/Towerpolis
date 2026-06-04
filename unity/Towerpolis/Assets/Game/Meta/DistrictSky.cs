using UnityEngine;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// Tints the gradient skybox to the active district's sky (meta-spec §2). Clones the skybox material
    /// once and swaps it in, so the baked GradientSky.mat asset is never modified — switching districts
    /// just recolours the runtime clone.
    /// </summary>
    public static class DistrictSky
    {
        static Material _runtime;

        public static void Apply(in DistrictTheme t)
        {
            Material current = RenderSettings.skybox;
            if (current == null) return;

            if (_runtime == null || current != _runtime)
            {
                _runtime = new Material(current); // clone — don't edit the shared/baked asset
                RenderSettings.skybox = _runtime;
            }

            if (_runtime.HasProperty("_TopColor")) _runtime.SetColor("_TopColor", t.SkyTop);
            if (_runtime.HasProperty("_HorizonColor")) _runtime.SetColor("_HorizonColor", t.SkyHorizon);
            if (_runtime.HasProperty("_BottomColor")) _runtime.SetColor("_BottomColor", t.SkyBottom);
            DynamicGI.UpdateEnvironment();
        }
    }
}
