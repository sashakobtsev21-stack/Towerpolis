using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// Swings the pending block sinusoidally above the tower top (spec §1.1): position
    /// x = centerX + halfArc·cos(phase). The phase is continuous across floors (the controller carries
    /// it over). The deterministic initial phase comes from the swing RNG stream.
    /// </summary>
    public sealed class CraneController : MonoBehaviour
    {
        Transform _block;
        float _halfArc;
        float _period;
        float _phase;
        float _centerX;
        float _holdY;
        float _cableLength;
        float _thetaMax;
        bool _swinging;

        public float Phase => _phase;
        public float CurrentX => _block != null ? _block.position.x : _centerX;

        public void BeginSwing(Transform block, float centerX, float holdY, float halfArc, float period, float phase, float cableLength)
        {
            _block = block;
            _centerX = centerX;
            _holdY = holdY;
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
            // Pendulum: the block hangs from a pivot and swings on an arc — low & fast at centre, high &
            // slow at the edges — and tilts with the swing. Far more natural than a horizontal slide.
            float theta = _thetaMax * Mathf.Cos(_phase);   // angle from vertical
            float pivotY = _holdY + _cableLength;
            float x = _centerX + _cableLength * Mathf.Sin(theta);
            float y = pivotY - _cableLength * Mathf.Cos(theta);
            _block.SetPositionAndRotation(new Vector3(x, y, 0f),
                Quaternion.Euler(0f, 0f, -theta * Mathf.Rad2Deg));
        }
    }
}
