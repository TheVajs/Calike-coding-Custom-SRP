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
    [SerializeField] private bool useDynamicBatching = true; // 
    [SerializeField] private bool useGPUInstancing = true;    // 
    [SerializeField] private bool useSRPBatcher = true;       // Process of combining draw calls, reducing the time spent communicating between CPU ang GPU.

    [SerializeField] private ShadowSettings shadows = default;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows);
    }

}
