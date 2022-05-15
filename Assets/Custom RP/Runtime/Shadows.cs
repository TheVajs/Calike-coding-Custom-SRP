using Custom_RP.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

public class Shadows
{
    public static readonly int DirShadowAtlasId = Shader.PropertyToID("_DirectionalShadowAtlas");
    private static readonly int DirShadowMatricesId = Shader.PropertyToID("_DirectionalShadowMatrices");
    //private static readonly int CascadeDataId = Shader.PropertyToID("_CascadeData");
    
    public static readonly int DirShadowNoiseId = Shader.PropertyToID("_DirectionalNoiseShadowAtlas");

    private const int MaxShadowedDirectionalLightCount = 4;
    private int _shadowDirectionalLightCount;

    public static readonly Matrix4x4[] DirShadowMatrices = new Matrix4x4[MaxShadowedDirectionalLightCount];
    
    //private static readonly Vector4[] cascadeData = new Vector4[maxCas]

    public struct ShadowedDirectionalLight
    {
        public int LightIndex;
        public float NearPlaneOffset;
        public float SlopeScaleBias;
    }

    public readonly ShadowedDirectionalLight[] ShadowedDirectionalLights = new ShadowedDirectionalLight[MaxShadowedDirectionalLightCount];

    private const string BufferName = "_Shadows";
    private readonly CommandBuffer _buffer = new CommandBuffer {
        name = BufferName
    };
    
    private ScriptableRenderContext _context;
    private CullingResults _cullingResults;
    private ShadowSettings _settings;

    private ShadowCloudPass _cloudPass;

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults,  ShadowSettings settings)
    {
        _context = context;
        _cullingResults = cullingResults;
        _settings = settings;
        _shadowDirectionalLightCount = 0;
        _cloudPass = settings.clouPassSettings;
    }

    public void Render()
    {
        if (_shadowDirectionalLightCount > 0)
        {
            RenderDirectionalShadows();
        }
        else
        {
            _buffer.GetTemporaryRT(DirShadowAtlasId, 1, 1,
                32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap
            );
        }
    }

    private void RenderDirectionalShadows()
    {
        var atlasSize = (int)_settings.directional.atlasSize;
        
        // Claims a square render texture, but is ARGB by default.
        _buffer.GetTemporaryRT(DirShadowAtlasId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.Shadowmap);
        _buffer.SetRenderTarget(DirShadowAtlasId,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        
        _buffer.ClearRenderTarget(true, false, Color.clear);
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();
        
        var split = _shadowDirectionalLightCount <= 1 ? 1 : 2;
        var tileSize = atlasSize / split;
        
        for (var i = 0; i < _shadowDirectionalLightCount; i++) {
            RenderDirectionalShadows(i, split, tileSize);
        }

        _buffer.SetGlobalMatrixArray(DirShadowMatricesId, DirShadowMatrices);
        ExecuteBuffer();

        

        //_buffer.SetGlobalTexture("_TestTexture", DirShadowAtlasId);
        //_buffer.SetRenderTarget(DirShadowNoiseId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        //_buffer.Blit(DirShadowAtlasId, DirShadowNoiseId);
        ExecuteBuffer();

        //_buffer.SetShadowSamplingMode(DirShadowAtlasId, ShadowSamplingMode.RawDepth);

        //_buffer.SetShadowSamplingMode(DirShadowAtlasId, ShadowSamplingMode.RawDepth);
        //_buffer.Blit(DirShadowAtlasId, DirShadowNoiseId);
        //_buffer.SetGlobalTexture("_DirectionalNoiseShadowAtlas", DirShadowNoiseId);
        
        //_buffer.Blit(DirShadowAtlasId, _temporaryBufferId,  _cloudPass.material, ShaderData.Pass.Copy);
        //Draw(DirShadowNoiseId, DirShadowAtlasId, Pass.Copy);
        
        _buffer.GetTemporaryRT(DirShadowNoiseId, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.RFloat);
        _buffer.SetRenderTarget(DirShadowNoiseId,
           RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        int temp = Shader.PropertyToID("_test");
        _buffer.GetTemporaryRT(temp, atlasSize, atlasSize,
            32, FilterMode.Bilinear, RenderTextureFormat.RFloat);
        _buffer.SetRenderTarget(temp,
            RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        _buffer.CopyTexture(DirShadowAtlasId, temp);
        _buffer.SetGlobalTexture("_MainTex", DirShadowAtlasId);
        //_buffer.ClearRenderTarget(true, false, Color.clear);
        ExecuteBuffer();
        
        //_buffer.SetShadowSamplingMode(DirShadowAtlasId, ShadowSamplingMode.RawDepth);
        //_buffer.CopyTexture(DirShadowAtlasId, DirShadowNoiseId);
        _buffer.Blit(temp, DirShadowNoiseId, _cloudPass.material);

        //_buffer.DrawProcedural( Matrix4x4.identity, _cloudPass.material, 0, MeshTopology.Triangles, 3);
        _buffer.SetGlobalTexture("_DirectionalNoiseShadowAtlas", DirShadowNoiseId);
        _buffer.EndSample(BufferName);        
        ExecuteBuffer();
        
        _buffer.ReleaseTemporaryRT(temp);
        ExecuteBuffer();
    }
    
    private  Matrix4x4 ConvertToAtlasMatrix (Matrix4x4 m, Vector2 offset, int split) {
        if (SystemInfo.usesReversedZBuffer) {
            m.m20 = -m.m20;
            m.m21 = -m.m21;
            m.m22 = -m.m22;
            m.m23 = -m.m23;
        }
        
        var scale = 1f / split;
        m.m00 = (0.5f * (m.m00 + m.m30) + offset.x * m.m30) * scale;
        m.m01 = (0.5f * (m.m01 + m.m31) + offset.x * m.m31) * scale;
        m.m02 = (0.5f * (m.m02 + m.m32) + offset.x * m.m32) * scale;
        m.m03 = (0.5f * (m.m03 + m.m33) + offset.x * m.m33) * scale;
        m.m10 = (0.5f * (m.m10 + m.m30) + offset.y * m.m30) * scale;
        m.m11 = (0.5f * (m.m11 + m.m31) + offset.y * m.m31) * scale;
        m.m12 = (0.5f * (m.m12 + m.m32) + offset.y * m.m32) * scale;
        m.m13 = (0.5f * (m.m13 + m.m33) + offset.y * m.m33) * scale;
        m.m20 = 0.5f * (m.m20 + m.m30);
        m.m21 = 0.5f * (m.m21 + m.m31);
        m.m22 = 0.5f * (m.m22 + m.m32);
        m.m23 = 0.5f * (m.m23 + m.m33);
        return m;
    }

    private void ExecuteBuffer()
    {
        _context.ExecuteCommandBuffer(_buffer);
        _buffer.Clear();
    }
    
    private Vector2 SetTileViewport (int index, int split, float tileSize) {
        Vector2 offset = new Vector2(index % split, index / split);
        _buffer.SetViewport(new Rect(
            offset.x * tileSize, offset.y * tileSize, tileSize, tileSize
        ));
        return offset;
    }

    private void RenderDirectionalShadows(int index, int split, int tileSize)
    {
        var light = ShadowedDirectionalLights[index];
        var shadowSettings = new ShadowDrawingSettings(_cullingResults, light.LightIndex);
        
        _cullingResults.ComputeDirectionalShadowMatricesAndCullingPrimitives(
            light.LightIndex, 0, 1, Vector3.zero, tileSize, light.NearPlaneOffset,
            out Matrix4x4 viewMatrix, out Matrix4x4 projectionMatrix, out ShadowSplitData splitData);
        shadowSettings.splitData = splitData;
        
        DirShadowMatrices[index] = ConvertToAtlasMatrix(projectionMatrix * viewMatrix,  
            SetTileViewport(index, split, tileSize), split);
        
        _buffer.SetViewProjectionMatrices(viewMatrix, projectionMatrix);
        _buffer.SetGlobalDepthBias(0f, light.SlopeScaleBias);
        ExecuteBuffer();
        
        _context.DrawShadows(ref shadowSettings);
        _buffer.SetGlobalDepthBias(0f, 0f);
    }

    public Vector3 ReserveDirectionalShadows(Light light, int visibleLightIndex)
    {
        if (_shadowDirectionalLightCount < MaxShadowedDirectionalLightCount &&
            light.shadows != LightShadows.None && light.shadowStrength > 0f &&
            _cullingResults.GetShadowCasterBounds(visibleLightIndex, out Bounds b))
        {
            ShadowedDirectionalLights[_shadowDirectionalLightCount] = new ShadowedDirectionalLight
            {
                LightIndex = visibleLightIndex,
                SlopeScaleBias = light.shadowBias,
                NearPlaneOffset = light.shadowNearPlane
            };
            return new Vector3(light.shadowStrength, _shadowDirectionalLightCount++,
                light.shadowNormalBias);
        }
        return Vector3.zero;
    }

    public void Cleanup()
    {
        _buffer.ReleaseTemporaryRT(DirShadowAtlasId);
        _buffer.ReleaseTemporaryRT(DirShadowNoiseId);
        ExecuteBuffer();
    }
}