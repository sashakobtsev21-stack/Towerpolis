using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// The welded tower. Tracks the top surface (Y / center X / width) in the Stack model and runs the
    /// SCRIPTED wobble (spec §5) — the tower root pivots at the base bottom-center and tilts around Z;
    /// amplitude rises and damping falls with height, and the equilibrium shifts toward accumulated
    /// lean, so tall/leaning towers feel heavy and tense. No Rigidbodies — the wobble is always faked
    /// (ADR-0002; real stacks explode past ~10–12 floors).
    ///
    /// KNOWN LIMITATION (physics review): TopX/TopY are tracked on the unrotated axis, while this root
    /// rotates for the wobble. At low floors/lean the gap is sub-perceptible (fine for the first
    /// fun-gate test), but before high-floor tuning we must resolve a design question with
    /// game-designer: does the player drop onto the SWAYING top (grade vs the rotated top — needs a
    /// deterministic wobble clock for the Phase-3 daily seed) or onto a fixed logical axis (wobble is
    /// purely cosmetic)? That decision drives whether top tracking moves to tower-local space.
    /// </summary>
    public sealed class TowerController : MonoBehaviour
    {
        public float TopY { get; private set; }
        public float TopX { get; private set; }
        public float TopWidth { get; private set; }

        GameTuning _tuning;
        int _floorCount;
        float _lean;
        float _timeSinceDrop;
        float _initialWidth;
        bool _active;
        bool _resetWobblePending;

        public void Init(GameTuning tuning, Transform baseBlock, float baseTopY, float topWidth)
        {
            _tuning = tuning;
            // The tower root must be unit-scaled: children (base + welded floors) inherit this scale,
            // and a zero/odd scale on the host GameObject would shrink them to nothing (invisible).
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            if (baseBlock != null) baseBlock.SetParent(transform, true);
            TopY = baseTopY;
            TopX = 0f;
            TopWidth = topWidth;
            _initialWidth = topWidth;
            _floorCount = 0;
            _lean = 0f;
            _timeSinceDrop = 0f;
            _active = true;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Weld a placed block and advance the top (Stack model: new center = overlap center).</summary>
        public void WeldPlaced(Transform block, float newTopX, float newTopWidth, int floorCount, float lean)
        {
            if (block != null) block.SetParent(transform, true);
            TopX = newTopX;
            TopWidth = newTopWidth;
            TopY += _tuning.floorHeight;
            _floorCount = floorCount;
            _lean = lean;
            _resetWobblePending = true; // restart the ring-out aligned to the next rendered frame
        }

        void Update()
        {
            if (!_active || _tuning == null) return;
            if (_resetWobblePending) { _timeSinceDrop = 0f; _resetWobblePending = false; }
            _timeSinceDrop += Time.deltaTime;
            float angle = WobbleAngle(_timeSinceDrop);
            transform.localRotation = Quaternion.Euler(0f, 0f, -angle); // +lean → top leans toward +X
        }

        float WobbleAngle(float t)
        {
            float baseAmp = Mathf.Clamp(
                _tuning.wobbleAmpBase + _tuning.wobbleAmpPerFloor * _floorCount,
                _tuning.wobbleAmpMin, _tuning.wobbleAmpMax);
            float amplitude = baseAmp + _tuning.wobbleLeanBias * Mathf.Abs(_lean);

            float period = Mathf.Clamp(
                _tuning.wobblePeriodBase + _tuning.wobblePeriodPerFloor * _floorCount,
                _tuning.wobblePeriodMin, _tuning.wobblePeriodMax);

            float rate = Mathf.Max(_tuning.dampingRateMin,
                _tuning.dampingRate - _tuning.dampingDecayPerFloor * _floorCount);
            float envelope = _tuning.idleWobbleScale + (1f - _tuning.idleWobbleScale) * Mathf.Exp(-rate * t);

            float equilibrium = (_initialWidth > 0f ? _lean / _initialWidth : 0f) * _tuning.maxLeanBiasAngle;
            float oscillation = amplitude * Mathf.Sin(2f * Mathf.PI * t / Mathf.Max(0.01f, period)) * envelope;
            return equilibrium + oscillation;
        }
    }
}
