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
        int _floorCount;

        public void Init(GameTuning tuning, TowerController tower)
        {
            _tuning = tuning;
            _tower = tower;
            if (cam == null) cam = Camera.main;
            _lookY = tower != null ? tower.TopY : 0f;
        }

        public void SetFloorCount(int floors) => _floorCount = floors;

        void LateUpdate()
        {
            if (_tuning == null || _tower == null || cam == null) return;

            float targetLookY = _tower.TopY + _tuning.cameraTargetOffsetY;
            _lookY = Mathf.SmoothDamp(_lookY, targetLookY, ref _velY, _tuning.cameraFollowSmoothTime);

            float dist = Mathf.Min(_tuning.maxCameraDistance,
                _tuning.cameraDistance + _tuning.cameraDistancePerFloor * Mathf.Max(0, _floorCount - 20));
            float rad = _tuning.cameraAngleX * Mathf.Deg2Rad;

            Vector3 look = new Vector3(0f, _lookY, 0f);
            Vector3 offset = new Vector3(0f, dist * Mathf.Sin(rad), -dist * Mathf.Cos(rad));
            cam.transform.position = look + offset;
            cam.transform.rotation = Quaternion.LookRotation(look - cam.transform.position, Vector3.up);
        }
    }
}
