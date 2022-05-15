Shader "Custom/ShadowCollector" {
	Properties {
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
	}
	SubShader {
		
		Tags {
                "LightMode" = "CustomLit"
		}
		
		
		pass {
		
			HLSLPROGRAM
			#pragma target 3.0
	        
			#pragma vertex vert
			#pragma fragment frag

			#include "Assets/Custom RP/ShaderLibrary/Common.hlsl"
			
	        TEXTURE2D(_ShadowTex);
			SAMPLER(sampler_ShadowTex);
			
	        float4x4 _ShadowMatrix;
	        float _ShadowBias;

	        sampler2D _MainTex;
	        
	        struct Atributes {
				float3 positionOS : POSITION;
				float2 uv : TEXCOORD0;
	        };

			struct Varyings {
				float4 positionCS : SV_POSITION;
	            float4 shadowCoords : TEXCOORD1;
				float2 uv : TEXCOORD0;
			};

	        Varyings vert(Atributes input) {
        		Varyings output;
        		float3 worldPosition = TransformObjectToWorld(input.positionOS);
	            output.positionCS = TransformWorldToHClip(worldPosition);
	            output.shadowCoords = mul(_ShadowMatrix, float4(worldPosition , 1.0));
        		output.uv = input.uv;
        		return output;
	        }

			float4 frag(Varyings input) : SV_TARGET {
				float lightDepth = 1.0 - tex2Dproj(sampler_ShadowTex, input.shadowCoords).r;
	            //float shadow = (input.shadowCoords.z - _ShadowBias) < lightDepth ? 1.0 : 0.5;

				float4 c = tex2D(_MainTex, input.uv);

				// float depth = SAMPLE_DEPTH_TEXTURE(_ShadowTex, sampler_ShadowTex, input.uv);
				// depth *= pow(SAMPLE_DEPTH_TEXTURE(_ShadowTex, sampler_ShadowTex, input.uv + float2(.01, 0.01)), 10) > 0.01 ? 1 : 0;
				return lightDepth.rrrr; //float4(c.rgb * shadow, c.a);
			}
			ENDHLSL	
		}
	}
	FallBack "Diffuse"
}
