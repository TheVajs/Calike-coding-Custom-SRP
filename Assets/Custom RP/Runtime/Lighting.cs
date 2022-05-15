using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    private const string BufferName = "_CustomLight";
    private readonly CommandBuffer _buffer = new CommandBuffer { name = BufferName };
    private CullingResults _cullingResults;
    private readonly Shadows _shadows = new Shadows();

    private const int MaxDirLightCount = 4;

    private static readonly int DirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static readonly int DiriLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static readonly int DirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    private static readonly int DirLIghtShadowDataId = Shader.PropertyToID("_DirectionalLightShadowData");

    private static readonly Vector4[] DirLightColors = new Vector4[MaxDirLightCount];
    private static readonly Vector4[] DirLightDirections = new Vector4[MaxDirLightCount];
    public static readonly Vector4[] DirLightShadowData = new Vector4[MaxDirLightCount];
    
    public void Setup(ScriptableRenderContext context, CullingResults cullingResults, ShadowSettings settings) 
    {
        _cullingResults = cullingResults;
        _buffer.BeginSample(BufferName);
        _shadows.Setup(context, cullingResults, settings);
        SetupLights();
        
        // Render shadows
        _shadows.Render();
        
        _buffer.EndSample(BufferName);
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }

    private void SetupLights()
    {
        // A more efficient array. Provides a connection to a native memory buffer. Efficiently share data between native Unity engine code(c++) and C#.
        var visibleLights = _cullingResults.visibleLights;
        var dirLightCount = 0;
        foreach (var t in visibleLights.Where(visibleLight => visibleLight.lightType == LightType.Directional))
        {
            var visibleLight = t;
            
            SetupDirectionalLighting(dirLightCount++, ref visibleLight);
            if (dirLightCount >= MaxDirLightCount)
            {
                break;
            }
        }

        _buffer.SetGlobalInt(DirLightCountId, dirLightCount);
        _buffer.SetGlobalVectorArray(DiriLightColorsId, DirLightColors);
        _buffer.SetGlobalVectorArray(DirLightDirectionsId, DirLightDirections);
        _buffer.SetGlobalVectorArray(DirLIghtShadowDataId, DirLightShadowData);
    }

    private void SetupDirectionalLighting(int index, ref VisibleLight visibleLight)
    {
        DirLightColors[index] = visibleLight.finalColor;
        DirLightDirections[index] = -visibleLight.light.transform.forward;
        DirLightShadowData[index] = _shadows.ReserveDirectionalShadows(visibleLight.light, index);
        
        //Light light = RenderSettings.sun;
        //commandBuffer.SetGlobalVector(dirLightColor, light.color.linear * light.intensity);
        //commandBuffer.SetGlobalVector(dirLightDirection, -light.transform.forward);
    }

    public void Cleanup()
    {
        _shadows.Cleanup();
    }
}
