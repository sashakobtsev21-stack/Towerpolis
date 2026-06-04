using UnityEngine;
using Towerpolis.Game.Gameplay;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// Drives the atmospheric ascent (GDD §4.9): as the tower climbs, the sky lerps toward space (via
    /// <see cref="DistrictSky"/>) and the key light dims. The altitude is EASED every frame toward the
    /// floor-based target, so the sky/lights glide continuously and never stall for a beat when a floor
    /// lands (the per-floor stepping read as a "freeze"). Space starts showing around floor ~75 and is
    /// full near <see cref="AscentFloors"/>. <see cref="Altitude01"/> is the shared smoothed value the
    /// background layers read. Self-bootstraps off the controller — no scene wiring.
    /// </summary>
    public sealed class Atmosphere : MonoBehaviour
    {
        // Floors to reach full space. At 150, floor 75 is the half-way point — space "по немногу" appears.
        const float AscentFloors = 150f;
        const float EaseRate = 1.6f; // how fast the displayed altitude catches its target (per second-ish)

        /// <summary>Smoothed 0 (ground) → 1 (space) altitude, eased toward the current height each frame.</summary>
        public static float Altitude01 { get; private set; }

        TowerGameController _controller;
        Light _key;
        float _keyBaseIntensity = 1.15f;
        float _alt;

        void Start()
        {
            _controller = FindFirstObjectByType<TowerGameController>();
            CacheKeyLight();
            if (_controller != null) _controller.RunStarted += OnRunStarted;
            _alt = Altitude01 = 0f;
            DistrictSky.UpdateAltitude(0f);
        }

        void OnDestroy()
        {
            if (_controller != null) _controller.RunStarted -= OnRunStarted;
        }

        // Snap straight back to the ground on a fresh run (no slow descent animation).
        void OnRunStarted()
        {
            _alt = Altitude01 = 0f;
            DistrictSky.UpdateAltitude(0f);
            if (_key != null) _key.intensity = _keyBaseIntensity;
        }

        void Update()
        {
            int floors = _controller != null ? _controller.Floors : 0;
            float target = Mathf.Clamp01(floors / AscentFloors);

            // Critically-damped-ish ease so the altitude flows smoothly between floors instead of snapping
            // on each landing. dt-clamped so a frame hitch can't make it jump.
            float k = 1f - Mathf.Exp(-EaseRate * Mathf.Min(Time.deltaTime, 0.1f));
            _alt = Mathf.Lerp(_alt, target, k);
            Altitude01 = _alt;

            DistrictSky.UpdateAltitude(_alt);
            if (_key != null) _key.intensity = _keyBaseIntensity * (1f - 0.5f * _alt);
        }

        void CacheKeyLight()
        {
            foreach (Light l in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (l.type != LightType.Directional) continue;
                _key = l;
                _keyBaseIntensity = l.intensity;
                return;
            }
        }
    }
}
