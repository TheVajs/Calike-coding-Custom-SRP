Shader "hidden/onlyNormal"
{
    Properties { }
    SubShader
    {
        Pass
        {
            ZWrite True
            ColorMask 0
            
            Tags 
            {
                "LightMode" = "CustomLit"
            }

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            
            #include "../ShaderLibrary/Common.hlsl"

            struct Attributes {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
            };

            Varyings Vert(Attributes input) {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            float4 Frag(Varyings input) : SV_TARGET {
                return float4((input.normalWS + 1)*.5, 1);
            }
            ENDHLSL
        }
    }
}
