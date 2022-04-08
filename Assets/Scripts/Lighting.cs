using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class Lighting
{
    const string bufferName = "_CustomLight";

    CommandBuffer commandBuffer = new CommandBuffer { name = bufferName };

    CullingResults cullingResults;

    //static int dirLightColor = Shader.PropertyToID("_DirectionLightColor");
    //static int dirLightDirection = Shader.PropertyToID("_DirectionLightDirection");

    public static int numDirLightCount = 4;

    static readonly int
        dirLightCountId = Shader.PropertyToID("_DirectionalLightCount"),
        diriLightColorsId = Shader.PropertyToID("_DirectionalLightColors"),
        dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

    static readonly Vector4[]
        dirLightColors = new Vector4[numDirLightCount],
        dirLightDirections = new Vector4[numDirLightCount];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults) 
    {
        this.cullingResults = cullingResults;
        commandBuffer.BeginSample(bufferName);
        //SetupDirectionalLighting();
        SetupLights();
        commandBuffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(commandBuffer);
        commandBuffer.Clear();
    }

    private void SetupLights()
    {
        // A more efficient array. Provides a connection to a native memory buffer. Efficiently share data between native Unity engine code(c++) and C#.
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;
        for(int i = 0; i < visibleLights.Length; i++)
        {
            var visibleLight = visibleLights[i];
            
            if (visibleLight.lightType != LightType.Directional)
                continue;

            SetupDirectionalLighting(dirLightCount++, ref visibleLight);
            if (dirLightCount >= numDirLightCount)
                break;
        }

        commandBuffer.SetGlobalInt(dirLightCountId, dirLightCount);
        commandBuffer.SetGlobalVectorArray(diriLightColorsId, dirLightColors);
        commandBuffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
    }

    private void SetupDirectionalLighting(int index, ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.light.transform.forward;

        //Light light = RenderSettings.sun;
        //commandBuffer.SetGlobalVector(dirLightColor, light.color.linear * light.intensity);
        //commandBuffer.SetGlobalVector(dirLightDirection, -light.transform.forward);
    }
}
