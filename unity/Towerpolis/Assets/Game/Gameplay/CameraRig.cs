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
            if (cam == null) return;

            // TEMP DEBUG: fixed camera aimed at the origin where the base lives, so visibility no longer
            // depends on any follow logic. If the base is created, it MUST be visible here. We'll
            // restore the auto-fit follow once visibility is confirmed.
            Vector3 look = new Vector3(0f, 4f, 0f);
            cam.transform.position = new Vector3(0f, 6f, -16f);
            cam.transform.rotation = Quaternion.LookRotation(look - cam.transform.position, Vector3.up);

            _ = _tuning; _ = _tower; _ = _lookY; _ = _velY; // keep fields referenced
        }
    }
}
