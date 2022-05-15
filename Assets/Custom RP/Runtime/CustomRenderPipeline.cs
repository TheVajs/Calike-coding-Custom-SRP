using UnityEngine.Rendering;
using UnityEngine;

public class CustomRenderPipeline : RenderPipeline
{
    private readonly CameraRenderer _renderer = new CameraRenderer();

    private readonly bool _useDynamicBatching, _useGPUInstancing;
    
    private readonly ShadowSettings _shadowSettings;

    public CustomRenderPipeline(bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher, ShadowSettings shadowSettings)
    {
        _useDynamicBatching = useDynamicBatching;
        _useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
        _shadowSettings = shadowSettings;
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var cam in cameras)
        {
            _renderer.Render(context, cam, _useDynamicBatching, _useGPUInstancing, _shadowSettings);
        }
    }
}
