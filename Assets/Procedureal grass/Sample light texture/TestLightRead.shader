Shader "Custom RP/Test light read"
{
    Properties
    {
    }
    CustomEditor "CustomShaderGUI"
    
    SubShader
    {
        // Defines 1 way to render something.
        Pass
        {
            ZWrite True

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
            
            #include "../../Custom RP/ShaderLibrary/Common.hlsl"
            #include "../../Custom RP/ShaderLibrary/Surface.hlsl"
            #include "../../Custom RP/ShaderLibrary/Shadows.hlsl"
            #include "../../Custom RP/ShaderLibrary/Light.hlsl"
#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

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
                

                output.positionWS = TransformObjectToWorld(input.positionOS);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 LitPassFragment(Varyings input) : SV_TARGET {
                UNITY_SETUP_INSTANCE_ID(input);
                
                float3 position = input.positionWS;
                float3 normal = normalize(input.normalWS);
	            float strength = _DirectionalLightShadowData[0].x;
	            float3 normalBias = normal * _DirectionalLightShadowData[0].z;
                float4x4 mat = _DirectionalShadowMatrices[_DirectionalLightShadowData[0].y];
	            float3 positionSTS = mul(mat, float4(position + normalBias, 1.0)).xyz;
                
                float shadow = SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS);
	            float atten = lerp(1.0, shadow, strength);

                float4 color = float4(1,1,1,1);
                if (atten < 0.001 )
                    color = float4(0,0,0,1);

                return color;
            }
#endif

            ENDHLSL
        }
    }
}
