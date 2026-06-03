using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// The welded tower (Tower-Bloxx model). Tracks the top surface and runs a PERPETUAL scripted sway
    /// (spec §5, pivoted): the root pivots at the base bottom-center and tilts around Z. The sway never
    /// rings out over time — its amplitude is driven by the accumulated lean (overhang), so a clean
    /// (centred) tower barely sways and a top-heavy/overhanging one sways hard toward the lean side.
    /// Perfect drops shrink the lean (calming the sway). No Rigidbodies — the sway is always faked
    /// (ADR-0002; real stacks explode past ~10–12 floors).
    ///
    /// KNOWN LIMITATION (physics review): TopX/TopY are tracked on the unrotated axis while this root
    /// rotates for the sway — negligible at low lean. The full drop-onto-the-swaying-tower model + the
    /// Phase-3 deterministic sway clock are a game-designer respec item (see the Tower-Bloxx pivot note).
    /// </summary>
    public sealed class TowerController : MonoBehaviour
    {
        public float TopY { get; private set; }
        public float TopX { get; private set; }
        public float TopWidth { get; private set; }

        GameTuning _tuning;
        int _floorCount;
        float _lean;
        float _time;
        float _initialWidth;
        bool _active;

        public void Init(GameTuning tuning, Transform baseBlock, float baseTopY, float topWidth)
        {
            _tuning = tuning;
            // The tower root must be unit-scaled at origin: children (base + welded floors) inherit this,
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
            _time = 0f;
            _active = true;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>Weld a placed block where it landed (whole, overhanging) and advance the top.</summary>
        public void WeldPlaced(Transform block, float newTopX, float newTopWidth, int floorCount, float lean)
        {
            if (block != null) block.SetParent(transform, true);
            TopX = newTopX;
            TopWidth = newTopWidth;
            TopY += _tuning.floorHeight;
            _floorCount = floorCount;
            _lean = lean; // drives the sway amplitude; Perfect drops shrink it
        }

        void Update()
        {
            if (!_active || _tuning == null) return;
            _time += Time.deltaTime; // continuous phase — the sway is perpetual, never reset
            float angle = WobbleAngle(_time);
            transform.localRotation = Quaternion.Euler(0f, 0f, -angle); // +lean → top leans toward +X
        }

        float WobbleAngle(float t)
        {
            float baseAmp = Mathf.Clamp(
                _tuning.wobbleAmpBase + _tuning.wobbleAmpPerFloor * _floorCount,
                _tuning.wobbleAmpMin, _tuning.wobbleAmpMax);
            float amplitude = Mathf.Min(baseAmp + _tuning.wobbleLeanBias * Mathf.Abs(_lean),
                _tuning.wobbleAmpMax);

            float period = Mathf.Clamp(
                _tuning.wobblePeriodBase + _tuning.wobblePeriodPerFloor * _floorCount,
                _tuning.wobblePeriodMin, _tuning.wobblePeriodMax);

            // Centre the sway around the lean direction so it visibly leans the overhang way.
            float equilibrium = (_initialWidth > 0f ? _lean / _initialWidth : 0f) * _tuning.maxLeanBiasAngle;
            return equilibrium + amplitude * Mathf.Sin(2f * Mathf.PI * t / Mathf.Max(0.01f, period));
        }
    }
}
