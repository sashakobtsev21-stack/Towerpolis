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
        [SerializeField] Color cableColor = new Color(0.45f, 0.33f, 0.20f); // brown rope
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
            // Continuous: if the hook is already swinging (between floors), KEEP the current phase so the
            // new block attaches exactly where the hook is right now. Only a cold start (new run) seeds it.
            if (!_swinging) _phase = phase;
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
            Transform released = _block;
            _block = null;
            // DON'T stop: the empty hook keeps swinging (no freeze on release) and the next block attaches
            // at the hook's current position. The rope stays live for a future tear-off animation.
            return released;
        }

        /// <summary>Stop and hide the crane (call when the run ends so a lone hook doesn't swing over the
        /// toppled tower). The next <see cref="BeginSwing"/> seeds a fresh phase and shows it again.</summary>
        public void EndSwing()
        {
            _swinging = false;
            _block = null;
            SetVisible(false);
        }

        void Update()
        {
            if (!_swinging) return;
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
            // The bob is where a block CENTRE hangs; the visible tilt is scaled down by the tilt factor.
            Vector3 bob = new Vector3(x, y, 0f);
            Quaternion tilt = Quaternion.Euler(0f, 0f, -theta * Mathf.Rad2Deg * _tiltFactor);
            Vector3 up = tilt * Vector3.up;

            // Position the block ON the bob when one is attached; the hook/rope swing regardless (no freeze).
            if (_block != null) _block.SetPositionAndRotation(bob, tilt);

            Vector3 attach = bob + up * _attachHeight; // top-centre of where a block hangs from the hook
            UpdateRope(new Vector3(centerX, pivotY, 0f), attach);
        }

        const float HookTopOffset = 0.5f; // height of the hook (cable meets it at the pulley on top)

        void UpdateRope(Vector3 pivot, Vector3 attach)
        {
            if (_rope == null) return;
            Vector3 dir = pivot - attach;
            Vector3 hookUp = dir.sqrMagnitude > 1e-6f ? dir.normalized : Vector3.up;
            _rope.SetPosition(0, pivot);
            _rope.SetPosition(1, attach + hookUp * HookTopOffset); // cable ends at the pulley atop the hook
            if (_hook != null)
            {
                _hook.position = attach;
                // Full rotation (not just .up, which leaves roll free → the hook would spin around the
                // cable): hook's up follows the cable, its flat face stays toward the camera (+Z).
                _hook.rotation = Quaternion.LookRotation(Vector3.forward, hookUp);
            }
        }

        void EnsureVisuals()
        {
            if (_rope == null)
            {
                // NOT parented to the crane: the crane may be a child of the swaying tower, and the rope
                // (world-space) + hook must stay locked to each other in world space, not be dragged apart.
                var ropeGo = new GameObject("Rope");
                _rope = ropeGo.AddComponent<LineRenderer>();
                _rope.useWorldSpace = true;
                _rope.positionCount = 2;
                _rope.numCapVertices = 2;
                _rope.startWidth = _rope.endWidth = ropeWidth;
                _rope.textureMode = LineTextureMode.Stretch;
                _rope.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                _rope.receiveShadows = false;
                _rope.material = UnlitMaterial(cableColor);
                _rope.startColor = _rope.endColor = cableColor;
            }
            if (_hook == null) _hook = BuildHook(LitMetal(hookColor));
        }

        // A recognizable crane hook (local +Y points up the cable): a pulley block on top, a straight
        // shank, and a smooth curved "J" zev built from connected tube segments. The curl sits on the
        // block top; a rounded tip caps the barb.
        Transform BuildHook(Material mat)
        {
            var root = new GameObject("Hook").transform; // NOT parented (world-space, see EnsureVisuals)

            Part(root, PrimitiveType.Cube, new Vector3(0f, 0.48f, 0f), new Vector3(0.20f, 0.12f, 0.16f), mat); // pulley
            Part(root, PrimitiveType.Sphere, new Vector3(0f, 0.42f, 0f), Vector3.one * 0.10f, mat);            // eye

            var center = new Vector3(0f, 0.18f, 0f);
            const float r = 0.15f;
            Vector3 top = center + Arc(90f, r);
            Bar(root, new Vector3(0f, 0.42f, 0f), top, 0.07f, mat); // shank down to the curl

            const int n = 10;
            Vector3 prev = top;
            for (int i = 1; i < n; i++)
            {
                Vector3 cur = center + Arc(Mathf.Lerp(90f, -150f, i / (float)(n - 1)), r); // ~240° J curl
                Bar(root, prev, cur, 0.07f, mat);
                prev = cur;
            }
            Part(root, PrimitiveType.Sphere, prev, Vector3.one * 0.075f, mat); // rounded barb tip
            return root;
        }

        static Vector3 Arc(float degrees, float radius)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0f);
        }

        void Bar(Transform parent, Vector3 a, Vector3 b, float thickness, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var t = go.transform;
            t.SetParent(parent, false);
            Vector3 dir = b - a;
            float len = dir.magnitude;
            t.localPosition = (a + b) * 0.5f;
            if (len > 1e-5f) t.localRotation = Quaternion.FromToRotation(Vector3.up, dir / len);
            t.localScale = new Vector3(thickness, Mathf.Max(0.001f, len * 0.5f), thickness); // cylinder is 2 tall
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        }

        void Part(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            var t = go.transform;
            t.SetParent(parent, false);
            t.localPosition = localPos;
            t.localScale = localScale;
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;
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
