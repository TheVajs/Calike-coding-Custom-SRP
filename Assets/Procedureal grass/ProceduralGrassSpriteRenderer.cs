using System;
using System.Collections.Generic;
using System.Linq;
using Procedureal_grass;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Quaternion = UnityEngine.Quaternion;
using Random = UnityEngine.Random;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;
using Vector4 = UnityEngine.Vector4;

// [ExecuteAlways]
public class ProceduralGrassSpriteRenderer : MonoBehaviour
{
    [Tooltip("A mesh to extrude the pyramids from")]
    [SerializeField] private Mesh sourceMesh = default;
    [Tooltip("The compute shader")]
    [SerializeField] private ComputeShader computeShader = default;
    [Tooltip("The material to render the pyramid mesh")]
    [SerializeField] private Material material = default;

    // This structure to send to the compute shader
    // This layout kind assures that the data is laid out sequentially
    [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
    private struct SourceVertex
    {
        public Vector3 position;
        public Vector3 normal;
        public Vector2 offset;
        public uint color;
    }

    private bool _initialized;

    // Just pointers to the buffers in the GPU.
    private ComputeBuffer _sourceVertBuffer;
    private ComputeBuffer _drawBuffer;
    private ComputeBuffer _argsBuffer;

    private int _idKernel;

    // The id of the kernel in the tri to vert count compute shader
    private int _dispatchSize;
    private Bounds _localBounds;

    // The data in the args buffer corresponds to:
    //      0: vertx cound per draw instance. We will only use one instance
    //      1: instance count. One
    //      2: start vertex location if using a Graphics Buffer
    //      3: and start instnce location if using a Graphics Buffer
    private readonly int[] _argsBufferReset = { 0, 1, 0, 0 };
    private static readonly int LocalToWorld = Shader.PropertyToID("_LocalToWorld");
    private static readonly int WorldToLocal = Shader.PropertyToID("_WorldToLocal");
    private static readonly int DrawTriangles = Shader.PropertyToID("_DrawTriangles");
    //private static readonly int CameraDepthTexture = Shader.PropertyToID("_CameraDepthTexture");

    // The size of one entry into the various compute buffers
    private const int SourceVertStride = sizeof(float) * (3 * 2 + 1 + 2);
    private const int DrawStride = sizeof(float) * (3 + 3 + (3 + 2) * 3 + 1);
    private const int ArgsStride = sizeof(int) * 4;

    private void OnEnable()
    {
        Setup();
    }

    private void Setup()
    {
        Debug.Assert(computeShader != null, "This grass compute shader is null", gameObject);
        Debug.Assert(material != null, "This grass material is null", gameObject);

        if (_initialized)
        {
            OnDisable();
        }
        _initialized = true;

        var positions = sourceMesh.vertices;
        var normals = sourceMesh.normals;
        var tris = sourceMesh.triangles;

        var vertices = new List<SourceVertex>(); //SourceVertex[] vertices = new SourceVertex[positions.Length];
        for (var i = 0; i < positions.Length; i++)
        {
            if (Random.Range(0.0f, 1.0f) < .05) continue;
            if (Vector3.Dot(normals[i], Vector3.forward) < 0.01) continue;
            var vert = new SourceVertex()
            {
                position = positions[i],
                normal = normals[i],
                color = Random.Range(0f, 1f) < .005f ? (uint)Random.Range(1, 4) : 0,
                offset = new Vector2(
                    Mathf.Floor(Random.Range(0f, 1f) * 4)/4, 
                    Mathf.Floor(Random.Range(0f, 1f) * 4)/4
                )
            };
            vertices.Add(vert);
        } 
        var numTriangles = tris.Length / 3;
        
        // Create compute buffers.
        _sourceVertBuffer = new ComputeBuffer(vertices.Count, SourceVertStride, ComputeBufferType.Structured, ComputeBufferMode.Immutable);
        _sourceVertBuffer.SetData(vertices);
        _drawBuffer = new ComputeBuffer(numTriangles * 3 * 2, DrawStride, ComputeBufferType.Append);
        _drawBuffer.SetCounterValue(0);
        _argsBuffer = new ComputeBuffer(1, ArgsStride, ComputeBufferType.IndirectArguments);

        _idKernel = computeShader.FindKernel("Main");

        // Set data on the shader
        computeShader.SetBuffer(_idKernel, "_SourceVertices", _sourceVertBuffer);
        computeShader.SetBuffer(_idKernel, "_DrawTriangles", _drawBuffer);
        computeShader.SetBuffer(_idKernel, "_IndirectArgsBuffer", _argsBuffer);
        computeShader.SetInt("_NumSourceVertices", numTriangles * 3 * 2);

        material.SetBuffer(DrawTriangles, _drawBuffer);

        // Calculate the number of threads to use. Get the thread size from the kernel
        // Then divide the number of triangles by that size
        computeShader.GetKernelThreadGroupSizes(_idKernel, out var threadGroupSize, out _, out _);
        _dispatchSize = Mathf.CeilToInt((float) numTriangles / threadGroupSize);

        // Get the bounds of the source mesh and then expand by the maximum blade witdh and size
        _localBounds = sourceMesh.bounds;
        _localBounds.Expand(1);
    }

    private void OnDisable()
    {
        if (_initialized)
        {
            _sourceVertBuffer.Release();
            _drawBuffer.Release();
            _argsBuffer.Release();
        }
        _initialized = false;
    }

    // https://answers.unity.com/questions/361275/cant-convert-bounds-from-world-coordinates-to-loca.html (5.9.2021)
    private Bounds TransformBounds(Bounds boundsOS)
    {
        var center = transform.TransformPoint(boundsOS.center);

        // transform the local extents' axes
        var extents = boundsOS.extents;
        var axisX = transform.TransformVector(extents.x, 0, 0);
        var axisY = transform.TransformVector(0, extents.y, 0);
        var axisZ = transform.TransformVector(0, 0, extents.z);

        // sum their absolute value to get the world extents
        extents.x = Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x);
        extents.y = Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y);
        extents.z = Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z);

        return new Bounds { center = center, extents = extents };
    }

    private int setData = 0;

    private void LateUpdate()
    {
        if (setData++ < 2)
        {
            OnDisable();
            Setup();
            
        }

        _drawBuffer.SetCounterValue(0);
        _argsBuffer.SetData(_argsBufferReset);

        var bounds = TransformBounds(_localBounds);

        var transform1 = transform;
        var local = transform1.localToWorldMatrix;
        var world = transform1.worldToLocalMatrix;
        material.SetMatrix(LocalToWorld, local);
        material.SetMatrix(WorldToLocal, world);

        var cam = Camera.main;

        var textureDepth = Shader.GetGlobalTexture(Shadows.DirShadowAtlasId);
        if (textureDepth != null)
        {
            computeShader.SetTexture(_idKernel, "_DirectionalShadowAtlas", textureDepth);
        }
        
        computeShader.SetVectorArray("_DirectionalLightShadowData", Lighting.DirLightShadowData);
        computeShader.SetMatrixArray("_DirectionalShadowMatrices", Shadows.DirShadowMatrices);

        var far = cam.farClipPlane;
        var near = cam.nearClipPlane;
        float x = 1 - far / near, y = far / near;
        computeShader.SetVector("_ZBufferParams", new Vector4(0, 0, x/far, y/far));
        
        computeShader.SetMatrix("_LocalToWorld", local);
        computeShader.SetMatrix("_WorldToLocal", world);
        computeShader.SetMatrix("_LocalToWorldNormal", local.inverse.transpose);
        computeShader.SetMatrix("_CameraViewMatrix", cam.transform.localToWorldMatrix);
        computeShader.SetMatrix("_CameraVPMatrix", cam.previousViewProjectionMatrix);
        computeShader.SetMatrix("_CameraYMatrix", Matrix4x4.Rotate(Quaternion.Euler(0,-45, 0)));
        computeShader.SetVector("_CameraDirection", cam.transform.forward);
        computeShader.SetVector("_WorldSpaceCameraPos", cam.transform.position);
        computeShader.SetMatrix("_CameraProjection", cam.projectionMatrix);
        computeShader.SetVector("_ScreenParams", new Vector4(cam.pixelWidth, cam.pixelHeight, 1f + 1f/cam.pixelWidth, 1f + 1f /cam.pixelHeight));
        
        computeShader.Dispatch(_idKernel, _dispatchSize, 1, 1);

        Graphics.DrawProceduralIndirect(material, bounds, MeshTopology.Triangles, _argsBuffer, 0, null, null,
            UnityEngine.Rendering.ShadowCastingMode.Off, true, gameObject.layer);
    }
}
