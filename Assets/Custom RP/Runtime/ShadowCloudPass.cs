using UnityEngine;

namespace Custom_RP.Runtime
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "ShadowCloudPass", menuName = "Custom/ShadowCloudPass")]
    public class ShadowCloudPass : ScriptableObject
    {
        [SerializeField] private Shader shader = default;
        [SerializeField] private Texture texture = default;
    
        [System.NonSerialized]
        private Material _material;

        private static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");

        public Material material {
            get {
                if (_material == null && shader != null) {
                    _material = new Material(shader);
                    _material.SetTexture(NoiseTex, texture as Texture2D);
                    _material.hideFlags = HideFlags.HideAndDontSave;
                }
                return _material;
            }
        }
    }
}