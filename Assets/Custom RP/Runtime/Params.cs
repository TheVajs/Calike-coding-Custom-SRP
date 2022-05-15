using UnityEngine;
using UnityEngine.Rendering;

public class Params
{
    private const string BufferName = "_CustomParams";
    private readonly CommandBuffer _buffer = new CommandBuffer { name = BufferName };

    public const int MaxDirLightCount = 4;

    private static readonly int
        ScreenParamsId = Shader.PropertyToID("_ScreenParams"),
        ProjectionParamsId = Shader.PropertyToID("_ProjectionParams"),
        ZBufferParams = Shader.PropertyToID("_ZBufferParams");

    public void Setup(ScriptableRenderContext context, Camera camera) 
    {
        _buffer.BeginSample(BufferName);

        var projectionParams = new Vector4();
        projectionParams.x = camera.projectionMatrix.m22;
        projectionParams.y = camera.nearClipPlane;
        projectionParams.z = camera.farClipPlane;
        projectionParams.w = 1/projectionParams.z;
        _buffer.SetGlobalVector(ScreenParamsId, projectionParams);

        
        var screenParams = new Vector4();
        screenParams.x = camera.pixelWidth;
        screenParams.y = camera.pixelHeight;
        screenParams.z = 1 + 1.0f/camera.pixelWidth;
        screenParams.w = 1 + 1.0f/camera.pixelHeight;
        _buffer.SetGlobalVector(ProjectionParamsId, screenParams);

        var zbufferParams = new Vector4();
        var far = camera.farClipPlane;
        var near = camera.nearClipPlane;
        zbufferParams.x = 1 - far / near;
        zbufferParams.y = far / near;
        zbufferParams.z = zbufferParams.x / far;
        zbufferParams.w = zbufferParams.y / far;
        _buffer.SetGlobalVector(ZBufferParams, screenParams);

        _buffer.EndSample(BufferName);
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
}
