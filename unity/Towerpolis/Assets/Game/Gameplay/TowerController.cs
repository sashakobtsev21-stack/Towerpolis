using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// The welded tower (Tower-Bloxx model). Blocks stack FLUSH in the tower's LOCAL space (so the
    /// wobble rotation can never open gaps or make them overlap — the whole stack rotates as one). Runs
    /// a PERPETUAL scripted sway whose amplitude comes only from accumulated overhang (lean): a centred
    /// tower doesn't sway, an off-centre one sways toward the lean and never decays with idle time, and
    /// a Perfect/magnet drop shrinks the lean to calm it. No Rigidbodies — sway is always faked
    /// (ADR-0002; real stacks explode past ~10–12 floors).
    /// </summary>
    public sealed class TowerController : MonoBehaviour
    {
        public float TopWidth { get; private set; }

        /// <summary>Current swaying world X of the top centre — grading and the crane aim here.</summary>
        public float TopWorldX => _topBlock != null ? _topBlock.position.x : 0f;
        /// <summary>Current world Y of the top face — contact threshold / crane height.</summary>
        public float TopY => _topBlock != null ? _topBlock.position.y + _tuning.floorHeight : _baseTopFaceY;
        /// <summary>Structural top X/Y WITHOUT the sway — the camera follows these so the sway stays visible
        /// while the walking tower stays on-screen.</summary>
        public float TopStructuralX => _topLocalX;
        public float TopStructuralY => _tuning != null ? _tuning.floorHeight * (_topFloorIndex + 1) : 0f;

        GameTuning _tuning;
        Transform _topBlock;
        float _topLocalX;
        int _topFloorIndex;
        float _baseTopFaceY;
        int _floorCount;
        float _lean;
        float _time;
        float _initialWidth;
        bool _active;

        public void Init(GameTuning tuning, Transform baseBlock, float baseTopY, float topWidth)
        {
            _tuning = tuning;
            // Unit-scaled, at origin, upright: children inherit this; a zero/odd host scale would shrink
            // the whole tower to nothing (invisible).
            transform.localScale = Vector3.one;
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            if (baseBlock != null)
            {
                baseBlock.SetParent(transform, true);
                baseBlock.localPosition = Vector3.zero;       // base root at local origin → top face at floorHeight
                baseBlock.localRotation = Quaternion.identity;
            }
            _topBlock = baseBlock;
            _topLocalX = 0f;
            _topFloorIndex = 0;
            _baseTopFaceY = baseTopY;
            TopWidth = topWidth;
            _initialWidth = topWidth;
            _floorCount = 0;
            _lean = 0f;
            _time = 0f;
            _active = true;
        }

        /// <summary>Weld a placed block FLUSH onto the stack in local space, offset horizontally by
        /// <paramref name="offsetApplied"/> from the floor below (0 for a Perfect/magnet snap).</summary>
        public void WeldPlaced(Transform block, float offsetApplied, float newTopWidth, int floorCount, float lean)
        {
            _topFloorIndex += 1;
            _topLocalX += offsetApplied;
            if (block != null)
            {
                block.SetParent(transform, true);
                block.localPosition = new Vector3(_topLocalX, _tuning.floorHeight * _topFloorIndex, 0f);
                // localRotation keeps the fall tilt; SettleUpright rights it.
            }
            _topBlock = block;
            TopWidth = newTopWidth;
            _floorCount = floorCount;
            _lean = lean;
        }

        void Update()
        {
            if (!_active || _tuning == null) return;
            _time += Time.deltaTime; // continuous phase — perpetual sway, never reset
            float angle = WobbleAngle(_time);
            transform.localRotation = Quaternion.Euler(0f, 0f, -angle); // +lean → top leans toward +X
        }

        float WobbleAngle(float t)
        {
            // Amplitude comes ONLY from accumulated overhang (lean): a centred tower does not sway, an
            // off-centre one sways toward the lean side, and a Perfect/magnet drop shrinks the lean which
            // calms it. Perpetual — it never decays with idle time, only via clean play.
            float amplitude = Mathf.Min(_tuning.wobbleLeanBias * Mathf.Abs(_lean), _tuning.wobbleAmpMax);

            float period = Mathf.Clamp(
                _tuning.wobblePeriodBase + _tuning.wobblePeriodPerFloor * _floorCount,
                _tuning.wobblePeriodMin, _tuning.wobblePeriodMax);

            float equilibrium = (_initialWidth > 0f ? _lean / _initialWidth : 0f) * _tuning.maxLeanBiasAngle;
            return equilibrium + amplitude * Mathf.Sin(2f * Mathf.PI * t / Mathf.Max(0.01f, period));
        }
    }
}
