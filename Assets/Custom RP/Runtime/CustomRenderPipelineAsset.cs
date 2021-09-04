using UnityEngine.Rendering;
using UnityEngine;

/// <summary>
/// 1. tutorial - Custom render pipeline
///     Link: https://catlikecoding.com/unity/tutorials/custom-srp/custom-render-pipeline/
/// </summary>
[CreateAssetMenu(menuName = "Rendering/Custom render pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{
    // Unity editor will create a new RP instance when it detects that the asset is changed.
    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher);
    }

}
