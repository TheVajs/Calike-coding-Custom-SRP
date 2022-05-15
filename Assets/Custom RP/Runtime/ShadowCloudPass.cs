using UnityEngine;

namespace Custom_RP.Runtime
{
    [System.Serializable]
    [CreateAssetMenu(fileName = "ShadowCloudPass", menuName = "Custom/ShadowCloudPass")]
    public class ShadowCloudPass : ScriptableObject
    {
        [SerializeField] private Material mat;

        //private static readonly int NoiseTex = Shader.PropertyToID("_NoiseTex");

        public Material material => mat;
            /*if (mat == null) {
                    mat = new Material(shader);
                    mat.SetTexture(NoiseTex, texture as Texture2D);
                    mat.hideFlags = HideFlags.HideAndDontSave;
                }
                */
            
    }
}