using UnityEngine;
using UnityEngine.Rendering;

public class Params
{
    private const string BufferName = "_CustomParams";
    private readonly CommandBuffer _buffer = new CommandBuffer { name = BufferName };

    private static readonly int
        ScreenParamsId = Shader.PropertyToID("_ScreenParams"),
        ProjectionParamsId = Shader.PropertyToID("_ProjectionParams"),
        ZBufferParams = Shader.PropertyToID("_ZBufferParams"),
        TimeId = Shader.PropertyToID("_Time"),
        SinTimeId = Shader.PropertyToID("_SinTime"),
        CosTimeId = Shader.PropertyToID("_CosTime"),
        TimeParametersId = Shader.PropertyToID("_TimeParameters");

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

        var t = Time.time;
        
        // (t/20, t, t*2, t*3)		
        var time = new Vector4
        {
            x = t / 20,
            y = t,
            z = t * 2,
            w = t* 2
        };	   
        _buffer.SetGlobalVector(TimeId, time);
        
        // sin(t/8), sin(t/4), sin(t/2), sin(t)
        var sinTime = new Vector4
        {
            x = Mathf.Sin(t/8),
            y = Mathf.Sin(t/4),
            z = Mathf.Sin(t/2),
            w = Mathf.Sin(t)
        };
        _buffer.SetGlobalVector(SinTimeId, sinTime);
        
        // cos(t/8), cos(t/4), cos(t/2), cos(t)
        var cosTime = new Vector4
        {
            x = Mathf.Cos(t/8),
            y = Mathf.Cos(t/4),
            z = Mathf.Cos(t/2),
            w = Mathf.Cos(t)
        };
        _buffer.SetGlobalVector(CosTimeId, cosTime);
        
        // t, sin(t), cos(t)
        var timeParameters = new Vector4
        {
            x = t,
            y = Mathf.Sin(t),
            z = Mathf.Cos(t)
        };     
        _buffer.SetGlobalVector(TimeParametersId, timeParameters);
        

        _buffer.EndSample(BufferName);
        context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
}
