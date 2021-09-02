using UnityEngine;
using UnityEngine.Rendering;

public class CameraRenderer
{
	static ShaderTagId unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit");

	ScriptableRenderContext context;
	Camera camera;
	
	const string bufferName = "Render Camera";
	CommandBuffer buffer = new CommandBuffer
	{ // nitializer syntax, same as buffer.name = bufferName;
		name = bufferName
	};

	CullingResults cullingResults;

	public void Render(ScriptableRenderContext context, Camera camera)
	{
		this.context = context;
		this.camera = camera;

		if (!Cull())
			return;

		Setup();
		DrawVisibleGeometry();
		Submit();
	}

	void Setup()
    {
		context.SetupCameraProperties(camera);
		buffer.ClearRenderTarget(true, true, Color.clear); // cloear depth, color, clear color 
		buffer.BeginSample(bufferName);
		ExecuteBuffer();
    }

	void DrawVisibleGeometry()
    {
		var sortingSettings = new SortingSettings(camera);
		sortingSettings.criteria = SortingCriteria.CommonOpaque; // (front-to-back)

		var drawingSettings = new DrawingSettings(unlitShaderTagId, sortingSettings);
		var filteringSettings = new FilteringSettings(RenderQueueRange.opaque); 

		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings); // 1. draw only opque objects

		context.DrawSkybox(camera); // 2. draw skybox

		sortingSettings.criteria = SortingCriteria.CommonTransparent; // (back-to-front)
		drawingSettings.sortingSettings = sortingSettings;
		filteringSettings.renderQueueRange = RenderQueueRange.transparent;

		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings); // 3. draw only transparent objects 
	}

	void Submit()
    {
		buffer.EndSample(bufferName);
		ExecuteBuffer();
		context.Submit();
    }

	void ExecuteBuffer()
	{
		context.ExecuteCommandBuffer(buffer);
		buffer.Clear();
	}

	bool Cull()
	{
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p)) // out - reference to the original struct, the method is responsible for setting the variable.
        {
			cullingResults = context.Cull(ref p); // ref - use for optimization, to not make copy of struct.
			return true;
        }
        return false;
	}
}