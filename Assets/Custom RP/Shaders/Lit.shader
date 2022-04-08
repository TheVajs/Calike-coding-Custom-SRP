Shader "Custom RP/Lit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (.5, .5, .5, 1.0)
        _Metallic("Metalic", Range(0.0, 1.0)) = 0.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.0
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
		[Toggle(_PREMULTIPLY_ALPHA)] _PremultiplyAlpha ("Premultiply Alpha", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
    }
    CustomEditor "CustomShaderGUI"
    SubShader
    {
        // Defines 1 way to render something.
        Pass
        {
            // We want to use the shader properties, which we can access here by putting them inside square brackets. 
            // This is old syntax from the days before programmable shaders.
            Blend[_SrcBlend][_DstBlend]
            ZWrite[_ZWrite]

            Tags { // Indicate that we are using a custom ligth mode
                "LightMode" = "CustomLit"
            }

            HLSLPROGRAM
            #pragma target 3.5 // turn off WebGL 1.0 and OpenGL ES 2.0 support

            // Pragma means action in greek
            // GPU instancing and works by issuing a single draw call for multiple objects with the same mesh at once.
            // Unity generates two instances of this shader, one with and one without GPU instancing support. This is added in inspector.
            #pragma multi_compile_instancing 
            #pragma shader_feature _CLIPPING // Compiling different shader based on features.
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            
            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/Surface.hlsl"
            #include "../ShaderLibrary/Light.hlsl"
            #include "../ShaderLibrary/BRDF.hlsl"
            #include "../ShaderLibrary/Lighting.hlsl"
            // #include "UnlitPass.hlsl" can include from outside file

#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

            // Constant memory buffer, although it remains accessible at the global level.
            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST) // Tilling and offset for base texture.
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff) // A material usually uses either transparency blending or alpha clipping, not both at the same time. A typical clip material is fully opaque except for the discarded fragments and does write to the depth buffer. It uses the AlphaTest render queue, which means that it gets rendered after all fully opaque objects. This is done because discarding fragments makes some GPU optimizations impossible, as triangles can no longer be assumed to entirely cover what's behind them. By drawing fully opaque objects first they might end up covering part of the alpha-clipped objects, which then don't need to process their hidden fragments.
                UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
                UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
            UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

            struct Attributes {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 baseUV : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID // GPU instancing
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 positionWS : VAR_POSITION;
                float3 normalWS : VAR_NORMAL;
                float2 baseUV : VAR_BASE_UV;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings LitPassVertex(Attributes input) {
                Varyings output;
                // This extracts the index from the input and stores it in a global static variable that the other instancing macros rely on.
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
                output.baseUV = input.baseUV * baseST.xy + baseST.zw;

                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 LitPassFragment(Varyings input) : SV_TARGET {
                UNITY_SETUP_INSTANCE_ID(input);

                float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);  // copy material property.
            
                // Light calculation
                float4 base = baseMap * baseColor;

                Surface surface;
                surface.normal = normalize(input.normalWS);
                surface.viewDirection = normalize(_WorldSpaceCameraPos - input.positionWS);
                surface.color = base.rgb;
                surface.alpha = base.a;
                surface.metallic = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Metallic);
                surface.smoothness = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Smoothness);
                #if defined(_PREMULTIPLY_ALPHA)
                    BRDF brdf = GetBRDF(surface, true);
                #else
                    BRDF brdf = GetBRDF(surface);
                #endif
                float3 color = CalculateLighting(surface, brdf);

                #if defined(_CLIPPING) // It will abort and discard the fragment if the value we pass it is zero or less.
                    clip(base.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
                #endif
                return float4(color, surface.alpha);
            }
#endif

            ENDHLSL
        }
    }
}
