using UnityEngine;
using Towerpolis.Core.Gameplay;

namespace Towerpolis.Game.Gameplay
{
    /// <summary>
    /// Creates block GameObjects whose ROOT transform is at the block's bottom-center (the authored
    /// pivot — clean stacking). MVP uses scaled primitive cubes so the loop is playable before the
    /// Blender art is imported; swap in the FBX prefabs later without changing the controller.
    /// </summary>
    public sealed class BlockSpawner : MonoBehaviour
    {
        [SerializeField] float depth = 2.0f;
        [SerializeField] float floorHeight = 1.5f;

        static readonly Color ColStandard = new Color(0.32f, 0.78f, 0.38f);
        static readonly Color ColBalcony = new Color(1.00f, 0.82f, 0.15f);
        static readonly Color ColPremium = new Color(0.18f, 0.58f, 1.00f);
        static readonly Color ColBase = new Color(0.82f, 0.82f, 0.86f); // light concrete — must stand out from the brown skybox ground

        Material _matStandard, _matBalcony, _matPremium, _matBase;

        void Awake()
        {
            _matStandard = MakeMaterial(ColStandard);
            _matBalcony = MakeMaterial(ColBalcony);
            _matPremium = MakeMaterial(ColPremium);
            _matBase = MakeMaterial(ColBase);
        }

        static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            var mat = new Material(shader);
            mat.color = color;
            return mat;
        }

        public Transform CreateBlock(FloorType type, float width, string label)
        {
            var root = new GameObject(label);
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = "Mesh";
            var col = cube.GetComponent<Collider>();
            if (col != null) Destroy(col); // contact is scripted, not physics
            cube.transform.SetParent(root.transform, false);
            cube.transform.localScale = new Vector3(width, floorHeight, depth);
            cube.transform.localPosition = new Vector3(0f, floorHeight * 0.5f, 0f); // root = bottom-center
            cube.GetComponent<MeshRenderer>().sharedMaterial = MaterialFor(type);
            return root.transform;
        }

        public Transform CreateBase(float width)
        {
            var root = CreateBlock(FloorType.Standard, width, "Base");
            var mesh = root.Find("Mesh");
            if (mesh != null)
            {
                mesh.GetComponent<MeshRenderer>().sharedMaterial = _matBase;
                Vector3 s = mesh.localScale; // a wider plinth so the foundation reads clearly
                s.x *= 1.2f;
                s.z *= 1.2f;
                mesh.localScale = s;
            }
            return root;
        }

        /// <summary>Resize a block's mesh width in place (the visual "slice" — Stack-style narrowing).</summary>
        public void SetWidth(Transform blockRoot, float width)
        {
            var mesh = blockRoot.Find("Mesh");
            if (mesh == null) return;
            Vector3 s = mesh.localScale;
            s.x = width;
            mesh.localScale = s;
        }

        Material MaterialFor(FloorType type) => type switch
        {
            FloorType.Standard => _matStandard,
            FloorType.Balcony => _matBalcony,
            FloorType.Premium => _matPremium,
            _ => _matStandard,
        };
    }
}
