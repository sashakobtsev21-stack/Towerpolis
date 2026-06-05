using UnityEngine;

namespace Towerpolis.Game.UI
{
    /// <summary>
    /// Insets its RectTransform to the device safe area (notches / rounded corners / home indicator) so
    /// edge-anchored HUD elements parented under it never sit under a cutout. Add to a full-stretch child of
    /// a ScreenSpaceOverlay canvas. Cheap: only re-applies when the safe area actually changes (rotation,
    /// foldables, simulator switches). Self-contained, no wiring.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaRoot : MonoBehaviour
    {
        RectTransform _rt;
        Rect _lastSafe;
        Vector2Int _lastScreen;

        void Awake() => _rt = (RectTransform)transform;

        void OnEnable() { _lastScreen = Vector2Int.zero; Apply(); } // force a re-apply after a rebuild

        void Update() => Apply();

        void Apply()
        {
            int w = Screen.width, h = Screen.height;
            if (w <= 0 || h <= 0) return;
            Rect sa = Screen.safeArea;
            if (sa == _lastSafe && w == _lastScreen.x && h == _lastScreen.y) return;
            _lastSafe = sa;
            _lastScreen = new Vector2Int(w, h);

            Vector2 min = new Vector2(sa.xMin / w, sa.yMin / h);
            Vector2 max = new Vector2(sa.xMax / w, sa.yMax / h);
            _rt.anchorMin = min;
            _rt.anchorMax = max;
            _rt.offsetMin = Vector2.zero;
            _rt.offsetMax = Vector2.zero;
        }
    }
}
