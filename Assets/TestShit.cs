using UnityEngine;
using UnityEngine.Rendering;

public class TestShit : MonoBehaviour
{
    private CommandBuffer cb;
    private RenderTexture m_ShadowmapCopy;
    void Start()
    {
        /*
         * 
        cb = new CommandBuffer();
        RenderTargetIdentifier shadowmap = BuiltinRenderTextureType.CurrentActive;
        m_ShadowmapCopy = new RenderTexture(4024, 4024, 32, RenderTextureFormat.Shadowmap);
        m_ShadowmapCopy.filterMode = FilterMode.Point;
        
        cb.SetShadowSamplingMode(shadowmap, ShadowSamplingMode.RawDepth);
        
        var id = new RenderTargetIdentifier(m_ShadowmapCopy);
        
        cb.Blit(shadowmap, id);
        cb.SetGlobalTexture("m_ShadowmapCopy", id);
        var m_Light = this.GetComponent<Light>();
        m_Light.AddCommandBuffer(LightEvent.AfterShadowMap, cb);
         */
    }
}
