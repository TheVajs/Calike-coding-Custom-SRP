Shader "Custom RP/Test"
{
    Properties
    {
        _NoiseTex ("Texdture", 2D) = "white" {}
        _MainTex ("Texsture", 2D) = "white" {}
    	_NoiseBottom ("Bottom", Range(0.0, 1.0)) = .1
    	_NoiseTop ("Top", Range(0.0, 1.0)) = .55
		[ShowAsVector2] _AnimationSpeed ("Animation Speed", Vector) = (0, 0, 0, 0)
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
            
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            float4 _NoiseTex_ST;

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
				float2 noiseUV : TEXCOORD2;
            };

            float _NoiseBottom, _NoiseTop;
            float2 _AnimationSpeed;

            Varyings DefaultPassVertex (Attributes input) {
	            Varyings output;
            	output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
				output.positionCS = TransformWorldToHClip(output.positionWS);
				output.screenPosition = ComputeScreenPos(output.positionCS);
            	output.normalWS = float3(0, 1, 0);
	            output.screenUV = TRANSFORM_TEX(input.uv, _MainTex);
	            output.noiseUV = TRANSFORM_TEX(input.uv, _NoiseTex);
	            return output;
            }

            float LitPassFragment(Varyings input) : SV_TARGET  {
                float2 screenUV = input.screenPosition.xy/input.screenPosition.w;
				float shadow = SAMPLE_DEPTH_TEXTURE(_MainTex, sampler_MainTex, screenUV);

            	float2 tilling = _Time.xy * _AnimationSpeed;

            	float cloudNoise = SAMPLE_DEPTH_TEXTURE(_NoiseTex, sampler_NoiseTex, input.noiseUV + tilling);

            	float shadowStep = 0; //lerp(shadow, .8, 1 - cloudNoise);
            	shadowStep = smoothstep(shadow, cloudNoise, .6) + shadow*.3;

                return max(shadow ,smoothstep(_NoiseBottom, _NoiseTop, cloudNoise + screenUV.y*.15));
            }

            ENDHLSL
        }
        
    }
}
