using UnityEditor;
using UnityEngine;

namespace Procedureal_grass.Create_mesh_with_compute_shader.Editor
{
    [CustomEditor(typeof(MeshBakeSettings))]
    public class CreateMeshEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button("Create"))
            {
                Debug.Log("create");
            }
        }
    }
}