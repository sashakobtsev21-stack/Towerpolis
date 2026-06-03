using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// Rights a freshly-landed block from its fall tilt (inherited from the pendulum swing) to upright,
    /// relative to the tower, over a short settle — so a tilted block "rights itself" as it beds in
    /// instead of snapping. Self-removing.
    /// </summary>
    public sealed class SettleUpright : MonoBehaviour
    {
        const float Duration = 0.16f;

        Quaternion _from;
        float _t;
        bool _playing;

        public void Play()
        {
            _from = transform.localRotation;
            _t = 0f;
            _playing = true;
        }

        void Update()
        {
            if (!_playing) return;
            _t += Time.deltaTime;
            float p = Mathf.Clamp01(_t / Duration);
            transform.localRotation = Quaternion.Slerp(_from, Quaternion.identity, p);
            if (p >= 1f)
            {
                transform.localRotation = Quaternion.identity;
                _playing = false;
                Destroy(this);
            }
        }
    }
}
