using UnityEngine;

namespace Procedureal_grass
{
    public class ComputeTesting : MonoBehaviour
    {
        public ComputeShader shader;

        void RunShader()
        {
            int kernelHandle = shader.FindKernel("CSMain");

            RenderTexture tex = new RenderTexture(256,256,24);
            tex.enableRandomWrite = true;
            tex.Create();

            shader.SetTexture(kernelHandle, "Result", tex);
            shader.Dispatch(kernelHandle, 256/8, 256/8, 1);
        }
    }
}