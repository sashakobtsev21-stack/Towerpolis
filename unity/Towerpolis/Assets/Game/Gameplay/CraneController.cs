using UnityEngine;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// Swings the pending block as a PENDULUM above the tower top: it hangs from a pivot, arcs on a
    /// cable (low/fast at centre, high/slow at the edges) and tilts with the swing. The swing centre is
    /// the tower's CURRENT (swaying) top, so the player aims at where the building actually is. A visible
    /// rope (LineRenderer) runs from the crane pivot down to a small hook clamp on the block top, and
    /// follows the swing; both hide when the block is released.
    /// </summary>
    public sealed class CraneController : MonoBehaviour
    {
        [Header("Hook & rope (visual)")]
        [SerializeField] float ropeWidth = 0.06f;
        [SerializeField] Color ropeColor = new Color(0.16f, 0.16f, 0.18f);
        [SerializeField] Color hookColor = new Color(0.24f, 0.24f, 0.27f);

        Transform _block;
        TowerController _tower;
        float _craneHeight;
        float _halfArc;
        float _period;
        float _phase;
        float _cableLength;
        float _thetaMax;
        float _tiltFactor;
        float _attachHeight;
        bool _swinging;

        LineRenderer _rope;
        Transform _hook;

        public float Phase => _phase;
        public float CurrentX => _block != null ? _block.position.x : (_tower != null ? _tower.TopWorldX : 0f);

        public void BeginSwing(Transform block, TowerController tower, float craneHeight,
            float halfArc, float period, float phase, float cableLength, float tiltFactor, float blockHeight)
        {
            _block = block;
            _tower = tower;
            _craneHeight = craneHeight;
            _halfArc = halfArc;
            _period = Mathf.Max(0.01f, period);
            _phase = phase;
            _cableLength = Mathf.Max(0.5f, cableLength);
            _thetaMax = Mathf.Asin(Mathf.Clamp(_halfArc / _cableLength, -1f, 1f));
            _tiltFactor = tiltFactor;
            _attachHeight = blockHeight;
            _swinging = true;
            EnsureVisuals();
            SetVisible(true);
            Apply();
        }

        public Transform Release()
        {
            _swinging = false;
            Transform released = _block;
            _block = null;
            SetVisible(false); // the rope lets go as the block drops
            return released;
        }

        void Update()
        {
            if (!_swinging || _block == null) return;
            _phase += 2f * Mathf.PI / _period * Time.deltaTime;
            Apply();
        }

        void Apply()
        {
            float centerX = _tower != null ? _tower.TopWorldX : 0f;
            float holdY = (_tower != null ? _tower.TopY : 0f) + _craneHeight;
            float theta = _thetaMax * Mathf.Cos(_phase);   // angle from vertical
            float pivotY = holdY + _cableLength;
            float x = centerX + _cableLength * Mathf.Sin(theta);
            float y = pivotY - _cableLength * Mathf.Cos(theta);
            // Position follows the full arc; the visible block tilt is scaled down by the tilt factor.
            _block.SetPositionAndRotation(new Vector3(x, y, 0f),
                Quaternion.Euler(0f, 0f, -theta * Mathf.Rad2Deg * _tiltFactor));

            UpdateRope(new Vector3(centerX, pivotY, 0f));
        }

        void UpdateRope(Vector3 pivot)
        {
            if (_rope == null || _block == null) return;
            Vector3 attach = _block.position + _block.up * _attachHeight; // top-centre of the swinging block
            _rope.SetPosition(0, pivot);
            _rope.SetPosition(1, attach);
            if (_hook != null)
            {
                _hook.position = attach;
                Vector3 dir = pivot - attach;
                if (dir.sqrMagnitude > 1e-6f) _hook.up = dir.normalized; // hook shank points up along the rope
            }
        }

        void EnsureVisuals()
        {
            if (_rope == null)
            {
                var ropeGo = new GameObject("Rope");
                ropeGo.transform.SetParent(transform, false);
                _rope = ropeGo.AddComponent<LineRenderer>();
                _rope.useWorldSpace = true;
                _rope.positionCount = 2;
                _rope.numCapVertices = 2;
                _rope.startWidth = _rope.endWidth = ropeWidth;
                _rope.textureMode = LineTextureMode.Stretch;
                _rope.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _rope.receiveShadows = false;
                _rope.material = UnlitMaterial(ropeColor);
                _rope.startColor = _rope.endColor = ropeColor;
            }
            if (_hook == null)
            {
                var hookGo = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                hookGo.name = "Hook";
                var col = hookGo.GetComponent<Collider>();
                if (col != null) Destroy(col);
                hookGo.transform.SetParent(transform, false);
                hookGo.transform.localScale = new Vector3(0.16f, 0.16f, 0.16f); // small clamp on the block top
                hookGo.GetComponent<MeshRenderer>().sharedMaterial = LitMetal(hookColor);
                _hook = hookGo.transform;
            }
        }

        void SetVisible(bool on)
        {
            if (_rope != null) _rope.enabled = on;
            if (_hook != null) _hook.gameObject.SetActive(on);
        }

        static Material UnlitMaterial(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            var m = new Material(sh) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            return m;
        }

        static Material LitMetal(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Lit");
            if (sh == null) sh = Shader.Find("Standard");
            var m = new Material(sh) { color = c };
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Metallic")) m.SetFloat("_Metallic", 0.8f);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", 0.5f);
            return m;
        }
    }
}
