using UnityEngine;
using UnityEngine.Rendering;

// https://catlikecoding.com/unity/tutorials/custom-srp/custom-render-pipeline/
public partial class CameraRenderer
{
	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"), litShaderId = new ShaderTagId("CustomLit");

	ScriptableRenderContext context;
	Camera camera;

	const string bufferName = "Render Camera";
	CommandBuffer buffer = new CommandBuffer { // nitializer syntax, same as buffer.name = bufferName;
		name = bufferName
	};

	CullingResults cullingResults;

	Lighting lighting = new Lighting();

	// https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html context
	public void Render(ScriptableRenderContext context, Camera camera,
		bool useDynamicBatching, bool useGPUInstancing, ShadowSettings shadowSettings)
	{
		this.context = context;
		this.camera = camera;

		PrepareBuffer();
		PrepareForSceneWindow();

		if (!Cull(shadowSettings.maxDistance))
			return;

		Setup();
		lighting.Setup(context, cullingResults, shadowSettings);
		DrawVisibleGeometry(useDynamicBatching, useGPUInstancing);
		DrawUnsupportedShaders();
		DrawGizmos();
		Submit();
	}

	void Setup()
	{
		context.SetupCameraProperties(camera);
		CameraClearFlags flags = camera.clearFlags; // defines 4 values from 1-4 -> skybox, solidColor, depth, nothing
		// cloear depth, color, clear color 
		buffer.ClearRenderTarget(
			flags <= CameraClearFlags.Depth, // clear depth if flag is set
			flags == CameraClearFlags.Color, // clear color if flag is set
			flags == CameraClearFlags.Color ? camera.backgroundColor.linear : Color.clear); 
		buffer.BeginSample(SampleName);
		ExecuteBuffer();
	}

	void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
	{
		var sortingSettings = new SortingSettings(camera);
		sortingSettings.criteria = SortingCriteria.CommonOpaque; // (front-to-back)

		var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings)
		{
			enableDynamicBatching = useDynamicBatching,
			enableInstancing = useGPUInstancing
		};
		drawingSettings.SetShaderPassName(1, litShaderId);
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings); // 1. draw only opque objects

		if (camera.clearFlags == CameraClearFlags.Skybox && RenderSettings.skybox != null)
			context.DrawSkybox(camera); // 2. draw skybox

		sortingSettings.criteria = SortingCriteria.CommonTransparent; // (back-to-front)
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings); // 3. draw only transparent objects 
	}
	
	void Submit()
	{
		buffer.EndSample(SampleName);
		ExecuteBuffer();
		context.Submit();
	}

	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	bool Cull(float maxShadowDistance)
	{
		if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) // out - reference to the original struct, the method is responsible for setting the variable.
		{
			p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);;
			cullingResults = context.Cull(ref p); // ref - use for optimization, to not make copy of struct.
			return true;
		}
		return false;
	}
}