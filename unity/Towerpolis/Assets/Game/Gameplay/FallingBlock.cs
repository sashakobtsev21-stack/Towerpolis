using System;
using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// The only moment real physics is active (spec §2): the dropped block falls as a Rigidbody under
    /// scaled gravity. Contact is a SCRIPTED swept check (OQ-09), not a physics collision — it detects
    /// the frame the block's bottom crosses the tower-top Y and reports the interpolated crossing X, so
    /// a fast fall on a low frame can't tunnel through and mis-grade.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public sealed class FallingBlock : MonoBehaviour
    {
        public Action<float> OnContact; // contact-frame X (bottom-center)

        Rigidbody _rb;
        float _topY;
        float _gravityScale;
        bool _falling;
        float _prevBottomY;

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.isKinematic = true;
            _rb.constraints = RigidbodyConstraints.FreezeRotation
                            | RigidbodyConstraints.FreezePositionX
                            | RigidbodyConstraints.FreezePositionZ;
        }

        public void Release(float topY, float gravityScale)
        {
            _topY = topY;
            _gravityScale = gravityScale;

            // Guard (physics review P0): if the block somehow spawns at/below the target Y, the swept
            // crossing can never be observed — grade immediately instead of falling forever.
            if (transform.position.y <= _topY)
            {
                OnContact?.Invoke(transform.position.x);
                return;
            }

            _rb.isKinematic = false;
            _prevBottomY = transform.position.y;
            _falling = true;
        }

        void FixedUpdate()
        {
            if (!_falling) return;

            _rb.AddForce(Physics.gravity * _gravityScale, ForceMode.Acceleration);

            float bottomY = transform.position.y; // root pivot is bottom-center → this is the bottom face
            if (_prevBottomY >= _topY && bottomY <= _topY)
            {
                _falling = false;
                _rb.isKinematic = true;
                // X is frozen for the whole fall, so the contact X is exactly the release X — no
                // sub-step interpolation needed.
                OnContact?.Invoke(transform.position.x);
                return;
            }

            _prevBottomY = bottomY;
        }
    }
}
