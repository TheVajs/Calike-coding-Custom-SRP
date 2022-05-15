Shader "Custom RP/Lit outline"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _HighLightColor ("Highlight color", Color) = (.5, .5, .5, 1.0)
        _MidToneColor ("Color midtone", Color) = (.5, .5, .5, 1.0)
        _ShadowColor ("Color shadow", Color) = (.5, .5, .5, 1.0)
        _MidRange ("Midtone range", Range(0,1)) = .85
        _ShadowRange ("Shadow range", Range(0,1)) = .15
        _OutlineColor ("Outline color", Color) = (1,1,1,1)
        _IncludeNT ("Incule NT", Float) = 0.0
        _DepthThreshold ("Depth threshold", Range(0,.5)) = .08
        _NormalThreshold ("Normal threshold", Range(0, .5)) = .1
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.0
    }

    SubShader
    {
        Pass
        {
            Name "Toon with outline" 
            Tags { 
                "LightMode" = "CustomLit"
            }            
            
            ZWrite True
            Cull Back
            
            HLSLPROGRAM
            #pragma multi_compile_instancing 
            #pragma vertex vert
            #pragma fragment frag

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 5.0

            #include "Assets/Custom RP/ShaderLibrary/Outlines.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Name "Normal pass" 
            Tags  { 
                "LightMode" = "NormalOnly" 
            }

            //ZWrite On

            HLSLPROGRAM
            #pragma target 3.5 
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Assets/Custom RP/ShaderLibrary/NormalOnly.hlsl"
            ENDHLSL
        }
        
        Pass
        {
            Name "DepthOnly pass" 
            Tags  { 
                "LightMode" = "DepthOnly" 
            }

            ZWrite True

            HLSLPROGRAM
            #pragma target 3.5 
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Assets/Custom RP/ShaderLibrary/DepthOnly.hlsl"
            ENDHLSL
        }
        
        Pass {
            Name "Shadow pass" 
			Tags 
            {
				"LightMode" = "ShadowCaster"
			}

			ColorMask 0

			HLSLPROGRAM
			#pragma target 3.5
			#pragma shader_feature _CLIPPING
			#pragma multi_compile_instancing
			#pragma vertex ShadowCasterPassVertex
			#pragma fragment ShadowCasterPassFragment
			#include "../ShaderLibrary/ShadowCasterPass.hlsl"
			ENDHLSL
		} /**/
    }
}