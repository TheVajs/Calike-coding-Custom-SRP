Shader "Custom RP/UnlitNoInstancing"
{
    Properties
    {
        _BaseColor("Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }

    SubShader
    {
        // Defines 1 way to render something.
        Pass
        {
            HLSLPROGRAM
            // Pragma means action in greek
            // GPU instancing and works by issuing a single draw call for multiple objects with the same mesh at once.
            // Unity generates two instances of this shader, one with and one without GPU instancing support. This is added in inspector.
            #pragma multi_compile_instancing 
            #pragma vertex UnlitPassVertex
			#pragma fragment UnlitPassFragment
            #include "../ShaderLibrary/Common.hlsl"
            // #include "UnlitPass.hlsl" can include from outside file

#ifndef CUSTOM_UNLIT_PASS_INCLUDED
#define CUSTOM_UNLIT_PASS_INCLUDED

            // Constant memory buffer, although it remains accessible at the global level.
            /* CBUFFER_START(UnityPerMaterial) // cbuffer - for some platforms it doesn't exsist, so use macros and let unity handle.
                float4 _BaseColor;
            CBUFFER_END */
            /*CBUFFER_START(UnityPerDraw) 
                float4x4 unity_ObjectToWorld;
                float4x4 unity_WorldToObject;
                float4 unity_LODFade;
                real4 unity_WroldTransformParams; //
            CBUFFER_END */ // CBUFFER_START switched with UNITY_INSTANCING_BUFFER_START

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial) // Per instance different colors.
	            UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            struct Attributes {
	            float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID // GPU instancing
            };

            float4 UnlitPassVertex (Attributes input) : SV_POSITION {
                // This extracts the index from the input and stores it in a global static variable that the other instancing macros rely on.
              	UNITY_SETUP_INSTANCE_ID(input);
	            float3 positionWS = TransformObjectToWorld(input.positionOS);
	            return TransformWorldToHClip(positionWS);
            }
            
            float4 UnlitPassFragment () : SV_TARGET {
	            return _BaseColor;
            }
#endif

            ENDHLSL
        }
    }
}
