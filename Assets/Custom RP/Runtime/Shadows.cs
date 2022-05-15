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
    
    static string[] directionalFilterKeywords = {
        "_DIRECTIONAL_PCF3",
        "_DIRECTIONAL_PCF5",
        "_DIRECTIONAL_PCF7",
    };

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
    private const int Depth = 32;
    private const FilterMode Filter = FilterMode.Point;

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
                32, FilterMode.Point, RenderTextureFormat.Shadowmap
            );
        }
    }
    
    private void RenderDirectionalShadows()
    {
        var atlasSize = (int)_settings.directional.atlasSize;
        
        //_buffer.SetShadowSamplingMode(DirShadowAtlasId, ShadowSamplingMode.RawDepth);
        _buffer.GetTemporaryRT(DirShadowAtlasId, atlasSize, atlasSize, Depth, Filter, RenderTextureFormat.Shadowmap);
        _buffer.SetRenderTarget(DirShadowAtlasId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store); 
        

        _buffer.ClearRenderTarget(true, false, Color.clear);
        _buffer.BeginSample(BufferName);
        ExecuteBuffer();
        
        var split = _shadowDirectionalLightCount <= 1 ? 1 : 2;
        var tileSize = atlasSize / split;
        
        for (var i = 0; i < _shadowDirectionalLightCount; i++) {
            RenderDirectionalShadows(i, split, tileSize);
        }

        _buffer.SetGlobalMatrixArray(DirShadowMatricesId, DirShadowMatrices);
        
        SetKeywords();
        
        _buffer.EndSample(BufferName); 
        ExecuteBuffer();

        ApplyProceduralNoise();
    }
    
    private void SetKeywords () {
        var enabledIndex = (int) _settings.directional.filter - 1;
        for (var i = 0; i < directionalFilterKeywords.Length; i++) {
            if (i == enabledIndex) {
                _buffer.EnableShaderKeyword(directionalFilterKeywords[i]);
            }
            else {
                _buffer.DisableShaderKeyword(directionalFilterKeywords[i]);
            }
        }
    }


    RenderTexture m_Shadowmap;
    private CommandBuffer m_BufGrabShadowmap = new CommandBuffer
    {
        name = "Grab shadowmap for Volumetric Fog"
    };
    
    
    private void ApplyProceduralNoise()
    {
        ExecuteBuffer();
        var atlasSize = (int)_settings.directional.atlasSize;
        
        var temp = Shader.PropertyToID("_test");
        m_BufGrabShadowmap.GetTemporaryRT(temp, atlasSize, atlasSize, Depth, Filter, RenderTextureFormat.RFloat);
        m_BufGrabShadowmap.SetRenderTarget(temp, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        m_BufGrabShadowmap.CopyTexture(DirShadowAtlasId, temp);
        _context.ExecuteCommandBuffer(m_BufGrabShadowmap);
        m_BufGrabShadowmap.Clear();
        
        
         RenderTextureFormat format = RenderTextureFormat.RFloat;
          
         ReleaseTemporary(ref m_Shadowmap);
         m_Shadowmap = RenderTexture.GetTemporary(atlasSize, atlasSize, 32, format, RenderTextureReadWrite.Linear);
         m_Shadowmap.filterMode = FilterMode.Point;
         m_Shadowmap.wrapMode = TextureWrapMode.Clamp;

         var tID = new RenderTargetIdentifier(m_Shadowmap);
         //m_BufGrabShadowmap.SetShadowSamplingMode(m_Shadowmap, ShadowSamplingMode.RawDepth);
         //m_BufGrabShadowmap.CopyTexture(DirShadowAtlasId, tID);
         //m_BufGrabShadowmap.SetShadowSamplingMode(tID, ShadowSamplingMode.RawDepth);
         _context.ExecuteCommandBuffer(m_BufGrabShadowmap);
         m_BufGrabShadowmap.Clear();
         
         //m_BufGrabShadowmap.SetShadowSamplingMode(m_Shadowmap, ShadowSamplingMode.RawDepth);
         //m_BufGrabShadowmap.SetShadowSamplingMode(DirShadowAtlasId, ShadowSamplingMode.RawDepth);
 
         //m_BufGrabShadowmap.SetGlobalTexture("_DirShadowmap", m_Shadowmap);
         m_BufGrabShadowmap.Blit(temp, tID, _cloudPass.material,  0);
         _context.ExecuteCommandBuffer(m_BufGrabShadowmap);
         m_BufGrabShadowmap.Clear();

         m_BufGrabShadowmap.CopyTexture(tID, DirShadowAtlasId);
         _buffer.ReleaseTemporaryRT(temp);
         
         _context.ExecuteCommandBuffer(m_BufGrabShadowmap);
         m_BufGrabShadowmap.Clear();
         
         
        /* 
        
        _buffer.GetTemporaryRT(DirShadowNoiseId, atlasSize, atlasSize, Depth, Filter, RenderTextureFormat.RFloat);
        //_buffer.SetRenderTarget(DirShadowNoiseId, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

        var temp = Shader.PropertyToID("_test");
        _buffer.GetTemporaryRT(temp, atlasSize, atlasSize, Depth, Filter, RenderTextureFormat.RFloat);
        _buffer.SetRenderTarget(temp, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        
        _buffer.CopyTexture(DirShadowAtlasId, temp);
        ExecuteBuffer();
        
        //_buffer.CopyTexture(DirShadowAtlasId, DirShadowNoiseId);
        _buffer.Blit(temp, DirShadowNoiseId, _cloudPass.material, -1);
        
        var targetRT = new RenderTargetIdentifier(DirShadowAtlasId);
        _buffer.Blit(DirShadowNoiseId, targetRT);
        //_buffer.Blit(DirShadowNoiseId, DirShadowAtlasId);
        
        //_buffer.SetGlobalTexture("_DirectionalShadowAtlas", DirShadowAtlasId);
        //_buffer.Blit(DirShadowNoiseId, DirShadowAtlasId, _cloudPass.material, -1);
        _buffer.EndSample(BufferName);        
        ExecuteBuffer();
        
        _buffer.ReleaseTemporaryRT(temp);
        ExecuteBuffer();*/
    }
    
    void ReleaseTemporary(ref RenderTexture rt)
    {
        if (rt == null)
            return;

        RenderTexture.ReleaseTemporary(rt);
        rt = null;
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
        m_BufGrabShadowmap?.Clear();

        _buffer.ReleaseTemporaryRT(DirShadowAtlasId);
        _buffer.ReleaseTemporaryRT(DirShadowNoiseId);
        ExecuteBuffer();
    }
}