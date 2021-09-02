using UnityEngine.Rendering;
using UnityEngine;

/// <summary>
/// 1. tutorial - Custom render pipeline
///     Link: https://catlikecoding.com/unity/tutorials/custom-srp/custom-render-pipeline/
/// </summary>
[CreateAssetMenu(menuName = "Rendering/Custom render pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline();
    }

}
