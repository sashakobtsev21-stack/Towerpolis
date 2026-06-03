using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// Lifecycle for a missed/tumbling block: it keeps its collider for a brief window so it physically
    /// bounces off the tower near the top, then the collider is switched OFF so it free-falls cleanly
    /// without snagging on far-away floors or other debris in mid-air. Self-destroys after its lifetime.
    /// </summary>
    public sealed class TumbleDebris : MonoBehaviour
    {
        const float BounceWindow = 0.4f; // collider stays on this long (the bounce off the tower)
        const float Lifetime = 3.0f;

        float _t;
        Collider _col;

        void Start() => _col = GetComponentInChildren<Collider>();

        void Update()
        {
            _t += Time.deltaTime;
            if (_t > BounceWindow && _col != null)
            {
                _col.enabled = false; // stop catching on anything once it's past the initial bounce
                _col = null;
            }
            if (_t > Lifetime) Destroy(gameObject);
        }
    }
}
