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
        bool _swinging;

        public float Phase => _phase;
        public float CurrentX => _block != null ? _block.position.x : _centerX;

        public void BeginSwing(Transform block, float centerX, float holdY, float halfArc, float period, float phase)
        {
            _block = block;
            _centerX = centerX;
            _holdY = holdY;
            _halfArc = halfArc;
            _period = Mathf.Max(0.01f, period);
            _phase = phase;
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
            float x = _centerX + _halfArc * Mathf.Cos(_phase);
            _block.position = new Vector3(x, _holdY, 0f);
        }
    }
}
