using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// Swings the pending block as a PENDULUM above the tower top: it hangs from a pivot, arcs on a
    /// cable (low/fast at centre, high/slow at the edges) and tilts with the swing. The swing centre is
    /// the tower's CURRENT (swaying) top, so the player aims at where the building actually is.
    /// </summary>
    public sealed class CraneController : MonoBehaviour
    {
        Transform _block;
        TowerController _tower;
        float _craneHeight;
        float _halfArc;
        float _period;
        float _phase;
        float _cableLength;
        float _thetaMax;
        bool _swinging;

        public float Phase => _phase;
        public float CurrentX => _block != null ? _block.position.x : (_tower != null ? _tower.TopWorldX : 0f);

        public void BeginSwing(Transform block, TowerController tower, float craneHeight,
            float halfArc, float period, float phase, float cableLength)
        {
            _block = block;
            _tower = tower;
            _craneHeight = craneHeight;
            _halfArc = halfArc;
            _period = Mathf.Max(0.01f, period);
            _phase = phase;
            _cableLength = Mathf.Max(0.5f, cableLength);
            _thetaMax = Mathf.Asin(Mathf.Clamp(_halfArc / _cableLength, -1f, 1f));
            _swinging = true;
            Apply();
        }

        public Transform Release()
        {
            _swinging = false;
            Transform released = _block;
            _block = null;
            return released;
        }

        void Update()
        {
            if (!_swinging || _block == null) return;
            _phase += 2f * Mathf.PI / _period * Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            float centerX = _tower != null ? _tower.TopWorldX : 0f;
            float holdY = (_tower != null ? _tower.TopY : 0f) + _craneHeight;
            float theta = _thetaMax * Mathf.Cos(_phase);   // angle from vertical
            float pivotY = holdY + _cableLength;
            float x = centerX + _cableLength * Mathf.Sin(theta);
            float y = pivotY - _cableLength * Mathf.Cos(theta);
            _block.SetPositionAndRotation(new Vector3(x, y, 0f),
                Quaternion.Euler(0f, 0f, -theta * Mathf.Rad2Deg));
        }
    }
}
