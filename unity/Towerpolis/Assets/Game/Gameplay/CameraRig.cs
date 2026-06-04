using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// Follows the tower top as it grows (spec §8.1): a smoothed look-at height and a 3/4 orbit that
    /// pulls back gradually above floor 20. Miss pull-back / topple framing comes with the juice pass.
    /// </summary>
    public sealed class CameraRig : MonoBehaviour
    {
        [SerializeField] Camera cam;

        [Header("Shake")]
        [Tooltip("World-units of camera offset at shake magnitude 1.")]
        [SerializeField] float shakeMaxOffset = 0.5f;
        [Tooltip("How fast a shake decays (magnitude/second).")]
        [SerializeField] float shakeDecay = 2.5f;
        [Tooltip("Shake wobble frequency.")]
        [SerializeField] float shakeFrequency = 26f;

        GameTuning _tuning;
        TowerController _tower;
        Vector3 _look;
        Vector3 _lookVel;
        float _shake;

        /// <summary>Kick a transient camera shake. <paramref name="magnitude"/> ~0.2 = small punch,
        /// ~0.8 = a heavy collapse. Takes the strongest of any overlapping kicks.</summary>
        public void Shake(float magnitude) => _shake = Mathf.Max(_shake, magnitude);

        public void Init(GameTuning tuning, TowerController tower)
        {
            _tuning = tuning;
            _tower = tower;
            if (cam == null) cam = Camera.main;
            _look = tower != null ? new Vector3(tower.TopStructuralX, tower.TopStructuralY, 0f) : Vector3.zero;
        }

        void LateUpdate()
        {
            if (_tuning == null || _tower == null || cam == null) return;

            // Aim between the tower top and the swinging crane block (so the player sees where to drop),
            // following the STRUCTURAL top (the lean/walk, not the sway) so the wobble stays visible.
            float aimY = _tower.TopStructuralY + _tuning.craneHeight * 0.5f;
            Vector3 targetLook = new Vector3(_tower.TopStructuralX, aimY, 0f);
            _look = Vector3.SmoothDamp(_look, targetLook, ref _lookVel, _tuning.cameraFollowSmoothTime);

            // Frame a window: the crane block above + the top floor + a couple of floors below.
            float window = _tuning.craneHeight + 7f;
            float vFov = cam.fieldOfView * Mathf.Deg2Rad;
            float dist = (window * 0.5f) / Mathf.Tan(vFov * 0.5f);
            dist = Mathf.Clamp(dist, 8f, _tuning.maxCameraDistance);

            float rad = _tuning.cameraAngleX * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(0f, dist * Mathf.Sin(rad), -dist * Mathf.Cos(rad));
            cam.transform.position = _look + offset;
            cam.transform.rotation = Quaternion.LookRotation(_look - cam.transform.position, Vector3.up);

            // Transient shake applied on top of the framed position (smooth Perlin, decays to 0).
            if (_shake > 0.0001f)
            {
                float amp = _shake * shakeMaxOffset;
                float sx = (Mathf.PerlinNoise(Time.time * shakeFrequency, 0.37f) - 0.5f) * 2f;
                float sy = (Mathf.PerlinNoise(0.91f, Time.time * shakeFrequency) - 0.5f) * 2f;
                cam.transform.position += cam.transform.right * (sx * amp) + cam.transform.up * (sy * amp);
                _shake = Mathf.MoveTowards(_shake, 0f, shakeDecay * Time.deltaTime);
            }
        }
    }
}
