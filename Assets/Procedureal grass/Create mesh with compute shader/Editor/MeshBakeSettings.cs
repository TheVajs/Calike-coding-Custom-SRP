using UnityEngine;

namespace Procedureal_grass.Create_mesh_with_compute_shader.Editor
{
    [CreateAssetMenu(fileName = "CreateMeshSettings", menuName = "Custom/CreateMenuSettings")]
    public class MeshBakeSettings : ScriptableObject
    {
        [Tooltip("The source mesh to build off")]
        public Mesh sourceMesh;

        public int sourceSubMeshIndex;
        public Vector3 scale;
        public Vector3 rotation;
        public float height;
    }
}