using UnityEngine;
using UnityEngine.Rendering;

// https://catlikecoding.com/unity/tutorials/custom-srp/custom-render-pipeline/
public partial class CameraRenderer
{
	private static readonly ShaderTagId UnlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
	private static readonly ShaderTagId LitShaderId = new ShaderTagId("CustomLit");
	private static readonly ShaderTagId NormalOnlyShaderId = new ShaderTagId("NormalOnly");
	private static readonly ShaderTagId DepthOnlyShaderId = new ShaderTagId("DepthOnly");

	private ScriptableRenderContext _context;
	private Camera _camera;

	private const string BufferName = "Render Camera";
	private readonly CommandBuffer _buffer = new CommandBuffer { // initializer syntax, same as buffer.name = bufferName;
		name = BufferName
	};

	private CullingResults _cullingResults;

	private readonly Lighting _lighting = new Lighting();
	private readonly Params _params = new Params();

	private readonly int _normalTextureShaderId = Shader.PropertyToID("_CameraNormalsTexture");
	private const string BufferNormalName = "Normal Only Pass";
	private readonly CommandBuffer _normalBuffer = new CommandBuffer {
		name = BufferNormalName
	};
	
	private readonly int _depthPrpasTextureIdentifier;

	private readonly Material _materialDepthOnly = new Material(Shader.Find("hidden/DepthOnly"))
	{
		hideFlags = HideFlags.HideAndDontSave
	};
	
	private readonly int _depthTextureShaderId = Shader.PropertyToID("_CameraDepthTexture");
	private const string BufferDepthName = "Depth Only Pass";
	private readonly CommandBuffer _depthBuffer = new CommandBuffer {
		name = BufferDepthName
	};
	
	private  void ExecuteDepthOnly()
	{
		_normalBuffer.GetTemporaryRT(
			_normalTextureShaderId, _camera.pixelWidth, _camera.pixelHeight,
			32, FilterMode.Point, RenderTextureFormat.ARGB32
		);
		_normalBuffer.SetRenderTarget(
			_normalTextureShaderId,
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare, 
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		_normalBuffer.ClearRenderTarget(true, true, Color.clear);
		_normalBuffer.BeginSample(BufferNormalName);
		_context.SetupCameraProperties(_camera);
		ExecuteBuffer(_normalBuffer);

		var sortingSettings = new SortingSettings(_camera) { criteria = SortingCriteria.CommonOpaque };
		var drawingSettings = new DrawingSettings(NormalOnlyShaderId, sortingSettings)
		{
			perObjectData = PerObjectData.None
		};

		var filterSettings = new FilteringSettings(RenderQueueRange.opaque);
		_context.DrawRenderers(_cullingResults, ref drawingSettings, ref filterSettings);

		_normalBuffer.SetGlobalTexture("_CameraNormalsTexture", _normalTextureShaderId);
		_normalBuffer.EndSample(BufferNormalName);
		ExecuteBuffer(_normalBuffer);
		
		// ===============
		
		_depthBuffer.GetTemporaryRT(
			_depthTextureShaderId, _camera.pixelWidth, _camera.pixelHeight,
			32, FilterMode.Point, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear
		);
		_depthBuffer.SetRenderTarget(
			_depthTextureShaderId,
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare, 
			RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store
		);
		_depthBuffer.ClearRenderTarget(true, false, Color.clear);
		_depthBuffer.BeginSample(BufferDepthName);
		ExecuteBuffer(_depthBuffer);
		
		drawingSettings = new DrawingSettings(DepthOnlyShaderId, sortingSettings)
		{
			perObjectData = PerObjectData.None
		};

		filterSettings = new FilteringSettings(RenderQueueRange.opaque);
		_context.DrawRenderers(_cullingResults, ref drawingSettings, ref filterSettings);
		
		_depthBuffer.SetGlobalTexture("_CameraDepthTexture", _depthTextureShaderId);
		_depthBuffer.EndSample(BufferDepthName);
		ExecuteBuffer(_depthBuffer);
	}
	
	// https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html context
	public void Render(ScriptableRenderContext context, Camera camera,
		bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
	{
		_context = context;
		_camera = camera;
		
		PrepareBuffer();
		PrepareForSceneWindow();

		if (!Cull(shadowSettings.maxDistance))
		{
			return;
		}
		
		// : Pre passes
		ExecuteDepthOnly();
		// :
		
		_lighting.Setup(context, _cullingResults, shadowSettings);		
		_params.Setup(context, camera);
		
		Setup();
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		DrawUnsupportedShaders();
		DrawGizmos();
		
		// : Post processing

		// :

		Cleanup();
		Submit();
	}
	
	void Cleanup () {
		_lighting.Cleanup();
		_normalBuffer.ReleaseTemporaryRT(_normalTextureShaderId);
		_depthBuffer.ReleaseTemporaryRT(_depthTextureShaderId);
	}

	private void Setup()
	{
		_buffer.SetRenderTarget(_camera.targetTexture);
		_context.SetupCameraProperties(_camera);
		var flags = _camera.clearFlags; // defines 4 values from 1-4 -> skybox, solidColor, depth, nothing
		// cloear depth, color, clear color 
		_buffer.ClearRenderTarget(
			flags <= CameraClearFlags.Depth, // clear depth if flag is set
			flags == CameraClearFlags.Color, // clear color if flag is set
			flags == CameraClearFlags.Color ? _camera.backgroundColor.linear : Color.clear); 
		_buffer.BeginSample(SampleName);
		ExecuteBuffer(_buffer);
	}

	private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
	{
		var sortingSettings = new SortingSettings(_camera)
		{
			criteria = SortingCriteria.CommonOpaque	
		};

		var drawingSettings = new DrawingSettings(UnlitShaderTagId, sortingSettings)
		{
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
		};
		drawingSettings.SetShaderPassName(1, LitShaderId);
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		_context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);

		if (_camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null) 
		{
			_context.DrawSkybox(_camera);
		}
		
		sortingSettings.criteria = SortingCriteria.CommonTransparent;
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		_context.DrawRenderers(_cullingResults, ref drawingSettings, ref filteringSettings);
	}
	
	private void Submit()
	{
		_buffer.EndSample(SampleName);
		ExecuteBuffer(_buffer);
		_context.Submit();
	}

	private void ExecuteBuffer(CommandBuffer cmd)
	{
		_context.ExecuteCommandBuffer(cmd);
		cmd.Clear();
	}

	private bool Cull(float maxShadowDistance)
	{
		if (!_camera.TryGetCullingParameters(out var p)) return false;
		
		p.shadowDistance = Mathf.Min(maxShadowDistance, _camera.farClipPlane);
		_cullingResults = _context.Cull(ref p);
		return true;
	}
}