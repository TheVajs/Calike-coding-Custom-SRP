Shader "Custom RP/Test"
{
    Properties
    {
        _TestTexture ("Textvbure", 2D) = "white" {}
        _NoiseTex ("Texdture", 2D) = "white" {}
        _MainTex ("Texsture", 2D) = "white" {}
    }
    
    SubShader
    {
        // Defines 1 way to render something.
        Pass
        {
            Cull Off
		    ZTest Always
		    ZWrite On
            
            HLSLPROGRAM
            #pragma target 3.5 // turn off WebGL 1.0 and OpenGL ES 2.0 support
            #pragma vertex DefaultPassVertex
            #pragma fragment LitPassFragment
            
            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/Shadows.hlsl"
            #include "../ShaderLibrary/Surface.hlsl"
            #include "../ShaderLibrary/Light.hlsl"

            TEXTURE2D(_TestTexture);
            SAMPLER(sampler_TestTexture);
            
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

			TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
			float4 _MainTex_ST;
            
            struct Attributes
	        {
	            float4 positionOS : POSITION;
            	float3 normalOS : NORMAL;
	            float2 uv : TEXCOORD0;
	        };

            struct Varyings {
                float4 positionCS : SV_POSITION;
            	float3 normalWS :  TEXCOORD1;
                float3 positionWS : TEXCOORD0;
				float4 screenPosition : TEXCOORD3; 
				float2 screenUV : VAR_SCREEN_UV;
            };

            Varyings DefaultPassVertex (Attributes input) {
	            Varyings output;
            	output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionCS = TransformWorldToHClip(output.positionWS);
				output.screenPosition = ComputeScreenPos(output.positionCS);
            	output.normalWS = float3(0, 1, 0);
	            output.screenUV = TRANSFORM_TEX(input.uv, _MainTex);
	            return output;
            }

            float LitPassFragment(Varyings input) : SV_TARGET  {
                float2 screenUV = input.screenPosition.xy/input.screenPosition.w;
				float r = SAMPLE_DEPTH_TEXTURE(_MainTex, sampler_MainTex, screenUV);
            	
            	//float4x4 mat = _DirectionalShadowMatrices[_DirectionalLightShadowData[0].y];
            	//float3 positionSTS = mul(mat, float3(input.screenPosition.x * 100, 0, input.screenPosition.z * 100)).xyz; //float4(input.positionWS+ .1, 1.0)).xyz;
                //float r = SAMPLE_TEXTURE2D_SHADOW(_DirectionalShadowAtlas, SHADOW_SAMPLER, positionSTS); // SAMPLE_DEPTH_TEXTURE(_MainTex, sampler_MainTex, input.screenUV);

            	float s =  SAMPLE_DEPTH_TEXTURE(_NoiseTex, sampler_NoiseTex, input.screenUV);
            	//float s = GetDirectionalShadowAttenuation(0, input.positionWS, input.normalWS);
                return lerp(r, 1, step((1 - s), 0.7) ); //smoothstep(0, .1, ); //r ; //(r * s.r * input.screenUV.y).rrrr * 10; // r * 10; //* s.r * .1;
            }

            ENDHLSL
        }
        
    }
}
