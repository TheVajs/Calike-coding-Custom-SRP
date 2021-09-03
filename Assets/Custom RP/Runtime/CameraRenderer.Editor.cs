using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
	partial void PrepareBuffer();
	partial void PrepareForSceneWindow(); // UI
	partial void DrawGizmos();
	partial void DrawUnsupportedShaders();

#if UNITY_EDITOR || DEVELOPMENT_BUILD // alocating memory for separeted names for cameras
	string SampleName { get; set; }
	partial void PrepareBuffer()
	{
		Profiler.BeginSample("Editor only");
		buffer.name = SampleName = camera.name;
		Profiler.EndSample();
	}
#else
	const string SampleName = bufferName;
#endif

#if UNITY_EDITOR || DEVELOPMENT_BUILD

	// For legacy shaders.
	static ShaderTagId[] legacyShaderTagIds = {
		new ShaderTagId("Always"),
		new ShaderTagId("ForwardBase"),
		new ShaderTagId("PrepassBase"),
		new ShaderTagId("Vertex"),
		new ShaderTagId("VertexLMRGBM"),
		new ShaderTagId("VertexLM")
	};

	static Material errorMaterial;

	// Add UI to world geometry.
	partial void PrepareForSceneWindow()
	{
		if (camera.cameraType == CameraType.SceneView)
			ScriptableRenderContext.EmitWorldGeometryForSceneView(camera);
	}

	partial void DrawGizmos()
	{
		if (Handles.ShouldRenderGizmos())
		{
			context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
			context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
		}
	}

	partial void DrawUnsupportedShaders()
	{
		if (errorMaterial == null)
			errorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));

		var drawingSettings = new DrawingSettings(legacyShaderTagIds[0], new SortingSettings(camera)); // for all shaders in separated pass.
		drawingSettings.overrideMaterial = errorMaterial;

		for (int i = 1; i < legacyShaderTagIds.Length; i++)
		{
			drawingSettings.SetShaderPassName(i, legacyShaderTagIds[i]);
		}

		var filteringSettings = FilteringSettings.defaultValue;
		context.DrawRenderers(cullingResults, ref drawingSettings, ref filteringSettings);
	}

#endif
}