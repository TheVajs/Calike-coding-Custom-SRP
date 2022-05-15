// Tut: https://roystan.net/articles/toon-shader.html (7.9.2021)
Shader "Custom/Grass pixels"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    HLSLINCLUDE
	#pragma multi_compile_fragment __ _ALPHATEST_ON
	ENDHLSL

    SubShader
    {
        // Compatible with universal pipeline
        Tags { "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline"}

        // Forward lit pass, the main pass that renders color.
        Pass
        {
            // Blend SrcAlpha OneMinusSrcAlpha
            Cull Off
            ZWrite On

            HLSLPROGRAM
            // Tell unity withc delpendencies we require
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 3.0

            // Register functions
            #pragma vertex Vertex
            #pragma fragment Fragment
            
            #ifndef TOON_INCLUDE
            #define TOON_INCLUDE

            #include "../Custom RP/ShaderLibrary/Common.hlsl"
            #include "../Custom RP/ShaderLibrary/Surface.hlsl"
            #include "../Custom RP/ShaderLibrary/Light.hlsl"    
            
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_ST;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
            };

            struct VertexOutput
            {
                float3 positionWS : TEXCOORD0; 
                float3 normalWS : TEXCOORD2; 
                float3 viewDir : TEXCOORD1;
                float2 uv : TEXCOORD3; 
                float2 screenPos : TEXCOORD4;
                float4 positionCS : SV_POSITION; 
            };

            float4x4 _ObjectToWorld, _WorldToObject, _View;

            void snap(in out float4 position) {
                float zoom = 1/(UNITY_MATRIX_P[1][1]);
	            float ppu = _ScreenParams.y / zoom / 2;

                float2 uv = (position.xy + 1) * .5;
                uv *= _ScreenParams.xy;
                uv = round(uv);
                uv /= _ScreenParams.xy;
                position.xy = (uv * 2) - 1;
            }

            // Structs
            struct VertexPositionInputs
            {
                float3 positionWS; // World space position
                float3 positionVS; // View space position
                float4 positionCS; // Homogeneous clip space position
                float4 positionNDC;// Homogeneous normalized device coordinates
            };

            struct VertexNormalInputs
            {
                real3 tangentWS;
                real3 bitangentWS;
                float3 normalWS;
            };

            VertexPositionInputs GetVertexPositionInputs(float3 positionOS)
            {
                VertexPositionInputs input;
                input.positionWS = TransformObjectToWorld(positionOS);
                input.positionVS = TransformWorldToView(input.positionWS);
                input.positionCS = TransformWorldToHClip(input.positionWS);

                float4 ndc = input.positionCS * 0.5f;
                input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
                input.positionNDC.zw = input.positionCS.zw;

                return input;
            }

            VertexNormalInputs GetVertexNormalInputs(float3 normalOS)
            {
                VertexNormalInputs tbn;
                tbn.tangentWS = real3(1.0, 0.0, 0.0);
                tbn.bitangentWS = real3(0.0, 1.0, 0.0);
                tbn.normalWS = TransformObjectToWorldNormal(normalOS);
                return tbn;
            }

            VertexOutput Vertex(Attributes input) {
                VertexOutput output = (VertexOutput)0;
    
                input.positionOS.xyz = mul((float3x3) Inverse(UNITY_MATRIX_V), input.positionOS.xyz);

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionWS = vertexInput.positionWS;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS);
                output.normalWS = normalInput.normalWS;
    
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                output.positionCS = TransformWorldToHClip(output.positionWS);
                // snap(output.positionCS);
                return output;
            }

            float4 Fragment(VertexOutput input) : SV_Target {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                if (color.a == 0)
                    discard;
                return color;
            }
#endif
            
            ENDHLSL
        }
    }
}
