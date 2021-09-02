using UnityEngine.Rendering;
using UnityEngine;

public class CustomRenderPipeline : RenderPipeline
{
    CameraRenderer renderer = new CameraRenderer();

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (Camera cam in cameras)
        {
            renderer.Render(context, cam);
        }
    }
}
