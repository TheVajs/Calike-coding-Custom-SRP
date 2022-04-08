using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class CustomShaderGUI : ShaderGUI
{
    private MaterialEditor _editor;
    private Object[] _materials; // you can select multiple materials at once
    private MaterialProperty[] _properties;
    private bool _showPresets;
    
    public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties) {
        base.OnGUI(materialEditor, properties);
        _editor = materialEditor;
        _materials = materialEditor.targets;
        _properties = properties;

        EditorGUILayout.Space();
        _showPresets = EditorGUILayout.Foldout(_showPresets, "Presets", true);
        if (_showPresets)
        {
            OpaquePreset();
            ClipPreset();
            FadePreset();
            TransparentPreset();
        }
    }

    bool Clipping {
        set => SetProperty("_Clipping", "_CLIPPING", value);
    }
    
    bool HasPremultiplyAlpha => HasProperty("_PremultiplyAlpha");
    bool PremultiplyAlpha {
        set => SetProperty("_PremultiplyAlpha", "_PREMULTIPLY_ALPHA", value);
    }

    BlendMode SrcBlend {
        set => SetProperty("_SrcBlend", (float)value);
    }

    BlendMode DstBlend {
        set => SetProperty("_DstBlend", (float)value);
    }

    bool ZWrite {
        set => SetProperty("_ZWrite", value ? 1f : 0f);
    }

    RenderQueue RenderQueue {
        set {
            foreach (Material m in _materials) {
                m.renderQueue = (int)value;
            }
        }
    }
    
    bool PresetButton (string name) {
        if (GUILayout.Button(name)) {
            _editor.RegisterPropertyChangeUndo(name);
            return true;
        }
        return false;
    }
    
    void OpaquePreset () {
        if (PresetButton("Opaque")) {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.Geometry;
        }
    }
    
    void TransparentPreset () {
        if (HasPremultiplyAlpha && PresetButton("Transparent")) {
            Clipping = false;
            PremultiplyAlpha = true;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    
    void FadePreset () {
        if (PresetButton("Fade")) {
            Clipping = false;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.SrcAlpha;
            DstBlend = BlendMode.OneMinusSrcAlpha;
            ZWrite = false;
            RenderQueue = RenderQueue.Transparent;
        }
    }
    
    void ClipPreset () {
        if (PresetButton("Clip")) {
            Clipping = true;
            PremultiplyAlpha = false;
            SrcBlend = BlendMode.One;
            DstBlend = BlendMode.Zero;
            ZWrite = true;
            RenderQueue = RenderQueue.AlphaTest;
        }
    }

    private void SetKeyWord(string keyword, bool enabled)
    {
        if (enabled)
        {
            foreach (var o in _materials)
            {
                var m = (Material)o;
                m.EnableKeyword(keyword);
            }
        }
        else
        {
            foreach (var o in _materials)
            {
                var m = (Material)o;
                m.DisableKeyword(keyword);
            }
        }
            
    }

    private bool SetProperty(string name, float value)
    {
        //FindProperty(name, _properties).floatValue = value;
        MaterialProperty property = FindProperty(name, _properties, false);
        if (property != null) {
            property.floatValue = value;
            return true;
        }
        return false;
    }

    private void SetProperty(string name, string keyword, bool value)
    {
        if (SetProperty(name, value ? 1f : 0f))
        {
            SetKeyWord(keyword, value);
        }
    }
    
    bool HasProperty (string name) =>
        FindProperty(name, _properties, false) != null;
}