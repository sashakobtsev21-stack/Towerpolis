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

        GameTuning _tuning;
        TowerController _tower;
        float _lookY;
        float _velY;

        public void Init(GameTuning tuning, TowerController tower)
        {
            _tuning = tuning;
            _tower = tower;
            if (cam == null) cam = Camera.main;
            _lookY = tower != null ? tower.TopY : 0f;
        }

        void LateUpdate()
        {
            if (_tuning == null || _tower == null || cam == null) return;

            // Frame the whole tower: look at its vertical centre and zoom out to fit base..top plus
            // headroom for the swinging block, so the base never scrolls off as the tower grows.
            float topY = _tower.TopY;
            float fitHeight = Mathf.Max(6f, topY + _tuning.cameraTargetOffsetY + 3f);
            float targetLookY = topY * 0.5f + 1.0f;
            _lookY = Mathf.SmoothDamp(_lookY, targetLookY, ref _velY, _tuning.cameraFollowSmoothTime);

            float vFov = cam.fieldOfView * Mathf.Deg2Rad;
            float dist = (fitHeight * 0.5f) / Mathf.Tan(vFov * 0.5f);
            dist = Mathf.Clamp(dist, 8f, _tuning.maxCameraDistance);

            float rad = _tuning.cameraAngleX * Mathf.Deg2Rad;
            Vector3 look = new Vector3(0f, _lookY, 0f);
            Vector3 offset = new Vector3(0f, dist * Mathf.Sin(rad), -dist * Mathf.Cos(rad));
            cam.transform.position = look + offset;
            cam.transform.rotation = Quaternion.LookRotation(look - cam.transform.position, Vector3.up);
        }
    }
}
