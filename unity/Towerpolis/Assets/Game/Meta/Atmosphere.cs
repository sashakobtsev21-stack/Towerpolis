using UnityEngine;
using Towerpolis.Game.Gameplay;

namespace Towerpolis.Game.Meta
{
    /// <summary>
    /// Drives the atmospheric ascent (GDD §4.9): as the tower climbs, the sky lerps toward space (via
    /// <see cref="DistrictSky"/>) and the key light dims. Updates per placed floor and resets on a new
    /// run. Self-bootstraps off the controller — no scene wiring.
    /// </summary>
    public sealed class Atmosphere : MonoBehaviour
    {
        TowerGameController _controller;
        Light _key;
        float _keyBaseIntensity = 1.15f;

        void Start()
        {
            _controller = FindFirstObjectByType<TowerGameController>();
            CacheKeyLight();
            if (_controller != null)
            {
                _controller.FloorAdded += OnFloor;
                _controller.RunStarted += OnRunStarted;
            }
            Apply(_controller != null ? _controller.Floors : 0);
        }

        void OnDestroy()
        {
            if (_controller == null) return;
            _controller.FloorAdded -= OnFloor;
            _controller.RunStarted -= OnRunStarted;
        }

        void OnRunStarted() => Apply(0);
        void OnFloor(int floors) => Apply(floors);

        void Apply(int floors)
        {
            DistrictSky.UpdateAltitude(floors);
            if (_key != null) _key.intensity = _keyBaseIntensity * (1f - 0.5f * DistrictSky.Altitude(floors));
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
