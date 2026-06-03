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
        Vector3 _look;
        Vector3 _lookVel;

        public void Init(GameTuning tuning, TowerController tower)
        {
            _tuning = tuning;
            _tower = tower;
            if (cam == null) cam = Camera.main;
            _look = tower != null ? new Vector3(tower.TopX, tower.TopY, 0f) : Vector3.zero;
        }

        void LateUpdate()
        {
            if (_tuning == null || _tower == null || cam == null) return;

            // Aim between the tower top and the swinging crane block (so the player sees where to drop),
            // and follow the tower's X drift as it leans/walks.
            float aimY = _tower.TopY + _tuning.craneHeight * 0.5f;
            Vector3 targetLook = new Vector3(_tower.TopX, aimY, 0f);
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
        }
    }
}
