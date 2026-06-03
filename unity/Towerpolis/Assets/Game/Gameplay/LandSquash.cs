using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// A quick squash-stretch on a freshly-landed block (spec §7.1) — gives the drop weight and impact.
    /// Self-contained, no tween library: a short animation on the "Mesh" child that removes itself when
    /// done. Scales relative to the mesh's current (already-sliced) size, so it preserves block width.
    /// </summary>
    public sealed class LandSquash : MonoBehaviour
    {
        const float Duration = 0.22f;
        const float SquashY = 0.72f;
        const float StretchXZ = 1.12f;

        Transform _mesh;
        Vector3 _baseScale;
        float _t;
        bool _playing;

        public void Play()
        {
            _mesh = transform.Find("Mesh");
            if (_mesh == null) { Destroy(this); return; }
            _baseScale = _mesh.localScale;
            _t = 0f;
            _playing = true;
        }

        void Update()
        {
            if (!_playing || _mesh == null) return;

            _t += Time.deltaTime;
            float p = _t / Duration;
            if (p >= 1f)
            {
                _mesh.localScale = _baseScale;
                _playing = false;
                Destroy(this);
                return;
            }

            float e = EaseOutBack(p);
            float y = Mathf.Lerp(SquashY, 1f, e);
            float xz = Mathf.Lerp(StretchXZ, 1f, e);
            _mesh.localScale = new Vector3(_baseScale.x * xz, _baseScale.y * y, _baseScale.z * xz);
        }

        static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float xm = x - 1f;
            return 1f + c3 * xm * xm * xm + c1 * xm * xm;
        }
    }
}
