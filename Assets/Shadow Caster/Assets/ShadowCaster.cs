using UnityEngine;
// https://forum.unity.com/threads/how-do-i-render-my-own-shadow-map.471293/
[ExecuteInEditMode]
public class ShadowCaster : MonoBehaviour
{
    public int targetSize = 512;
    public float shadowBias = 0.005f;

    private Camera _camera;
    private RenderTexture _depthTarget;
    
    private void OnEnable() {
        UpdateResources();
    }
    
    private void OnValidate() {
        UpdateResources();
    }

    private void UpdateResources()
    {
        if(_camera == null) {
            _camera = GetComponent<Camera>();
            _camera.depth = -1000;
        }

        if(_depthTarget == null || _depthTarget.width != targetSize)
        {
            var sz = Mathf.Max(targetSize, 16);
            _depthTarget = new RenderTexture(sz, sz, 16, RenderTextureFormat.Depth, RenderTextureReadWrite.Linear)
            {
                wrapMode = TextureWrapMode.Clamp,
                filterMode = FilterMode.Bilinear,
                autoGenerateMips = false,
                useMipMap = false
            };
            _camera.targetTexture = _depthTarget;
        }
    }

    private void Update()
    {
        var bias = new Matrix4x4() {
            m00 = 0.5f, m01 = 0,    m02 = 0,    m03 = 0.5f,
            m10 = 0,    m11 = 0.5f, m12 = 0,    m13 = 0.5f,
            m20 = 0,    m21 = 0,    m22 = 0.5f, m23 = 0.5f,
            m30 = 0,    m31 = 0,    m32 = 0,    m33 = 1,
        };
        
        var view = _camera.worldToCameraMatrix;
        var proj = _camera.projectionMatrix;
        var mtx = bias * proj * view;
        
        Shader.SetGlobalMatrix("_ShadowMatrix", mtx);
        Shader.SetGlobalTexture("_ShadowTex", _depthTarget);
        Shader.SetGlobalFloat("_ShadowBias", shadowBias);
    }
}
