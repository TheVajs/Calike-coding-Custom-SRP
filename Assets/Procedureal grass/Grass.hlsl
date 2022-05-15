#ifndef GRASS_BLADES_INCLUDE
#define GRASS_BLADES_INCLUDE

#include "../Custom RP/ShaderLibrary/Common.hlsl"
#include "../Custom RP/ShaderLibrary/Surface.hlsl"
#include "../Custom RP/ShaderLibrary/Light.hlsl"

struct DrawVertex {
	float3 positionWS;
	float2 uv;
};

struct DrawTriangle {
    uint color;
    float3 positionWS;
    float3 normalWS; 
    DrawVertex vertices[3];
};

// A structure buffer is just meant for reading
StructuredBuffer<DrawTriangle> _DrawTriangles;

struct VertexOutput
{
    float3 anchorPositionWS : TEXCOORD4;
    float4 anchorPositionCS :  TEXCOORD7;
    float4 anchorPositionScreen : TEXCOORD6;
    float3 positionWS : TEXCOORD0; 
    float4 positionCS : SV_POSITION;
    float4 screenPosition : TEXCOORD05;
    float3 normalWS : TEXCOORD2;
    float2 uv : TEXCOORD1; 
    uint color : TEXCOORD3; 
};

float4x4 _ObjectToWorld, _WorldToObject;
float4 _HighLightColor, _MidtoneColor, _ShadowColor;
float _SelfShadow, _Shadow, _minDepthDistance;
float _MidRange, _ShadowRange;

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_ST;

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

void snap(in out float4 position) {
    float zoom = 1/(UNITY_MATRIX_P[1][1]);
	float ppu = _ScreenParams.y / zoom / 2;

    float2 uv = (position.xy + 1) * .5;
    uv *= _ScreenParams.xy;
    uv = round(uv);
    uv /= _ScreenParams.xy;
    position.xy = (uv * 2) - 1;
}

VertexOutput Vertex(uint vertexID : SV_VertexID) {
    DrawTriangle tri = _DrawTriangles[vertexID / 3];
    DrawVertex input = tri.vertices[vertexID % 3];

    VertexOutput output;
    output.anchorPositionWS = tri.positionWS;
    output.anchorPositionCS = TransformWorldToHClip(tri.positionWS);
    output.anchorPositionScreen = ComputeScreenPos(output.anchorPositionCS);
    output.positionWS = input.positionWS; 
    output.normalWS = tri.normalWS;
    output.positionCS = TransformWorldToHClip(output.positionWS);
    output.screenPosition = ComputeScreenPos(output.positionCS);
    output.uv = input.uv; //TRANSFORM_TEX(input.uv, _MainTex);
    output.color = tri.color;
    
    return output;
}

// The SV_Target semantic tells the compiler that this function outputs the pixel color
float4 Fragment(VertexOutput input) : SV_Target {
    float3 normal = normalize(input.normalWS);
    float NdotL = dot(normalize(_DirectionalLightDirections[0].xyz), normal);

    float4 color;
    float atten = 1;
    if (_SelfShadow)
    {
        atten *= GetDirectionalShadowAttenuation(0, input.positionWS, normal);
    }
    if (_Shadow)
    {
        float3 offset = float3(0, 0, -.25);
        atten *= GetDirectionalShadowAttenuation(0, input.anchorPositionWS + offset, normal);
    }
    
    if (atten < 0.001 || NdotL < _ShadowRange)
    {
        color = _ShadowColor;
    }
    else if (NdotL < _MidRange)
    {
        color = _MidtoneColor;
    }
    else
    {
        color = _HighLightColor;
    }

    // Randomise some grass sprites
    if (input.color != 0)
    {
        color = color == _HighLightColor ? _MidtoneColor :
            color == _MidtoneColor ? _HighLightColor :
            color;
    }
    
    color = lerp(color, _ShadowColor, 1 - atten);

    float2 uv = input.uv;
    float2 id = floor(uv * 4);

    float speed = random(id) * .5 + min(.5, random(id));
    int step = floor(id.x + _Time.y * speed) % 4;
    uv.x = step * .25 + uv.x;
    
    if (SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a == 0) discard;

    float2 screenUV = input.anchorPositionScreen.xy / input.anchorPositionScreen.w;
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, screenUV);
    float currentDepth = input.positionCS.z;

    if (currentDepth < depth) 
        discard; 
    return color;
}
#endif