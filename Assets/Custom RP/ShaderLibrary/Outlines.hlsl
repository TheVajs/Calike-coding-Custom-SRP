#ifndef OUTLINES_INCLUDE
#define OUTLINES_INCLUDE

#include "Common.hlsl"
#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Light.hlsl"

struct Attributes
{
   float4 positionOS : POSITION;
   float3 normalOS : NORMAL;
   float3 color : COLOR;
   float2 uv : TEXCOORD0;
   UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
   float3 positionWS : TEXCOORD4; 
   float4 positionCS : SV_POSITION;
   float4 screenPosition : TEXCOORD3; 
   float3 normalWS : TEXCOORD2;
   float2 uv : TEXCOORD0;
   UNITY_VERTEX_INPUT_INSTANCE_ID
};

TEXTURE2D(_CameraDepthTexture);
SAMPLER(sampler_CameraDepthTexture);

TEXTURE2D(_CameraNormalsTexture);
SAMPLER(sampler_CameraNormalsTexture);

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
float4 _BaseMap_ST;

TEXTURE2D(_DirectionalNoiseShadowAtlas);
SAMPLER(sampler_DirectionalNoiseShadowAtlas);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
   UNITY_DEFINE_INSTANCED_PROP(float4, _MidToneColor)
   UNITY_DEFINE_INSTANCED_PROP(float4, _ShadowColor)
   UNITY_DEFINE_INSTANCED_PROP(float4, _HighLightColor)
   UNITY_DEFINE_INSTANCED_PROP(float, _MidRange)
   UNITY_DEFINE_INSTANCED_PROP(float, _ShadowRange)
   UNITY_DEFINE_INSTANCED_PROP(float, _DepthThreshold)
   UNITY_DEFINE_INSTANCED_PROP(float, _NormalThreshold)
   UNITY_DEFINE_INSTANCED_PROP(float4, _OutlineColor)
   UNITY_DEFINE_INSTANCED_PROP(float, _IncludeNT)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

float sampleDepth(float2 screenUV)
{
   float depth = SAMPLE_DEPTH_TEXTURE (_CameraDepthTexture, sampler_CameraDepthTexture, screenUV);
   return UNITY_MATRIX_P[3][3] == 0 ? Linear01Depth(depth, _ZBufferParams) : 1 - depth;
}

float getDepth(float2 uv, int x = 0, int y = 0)
{
   float2 offset = float2(x * 1.0/_ScreenParams.x, y * 1.0/_ScreenParams.y);
   return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv + offset); //SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv + offset);
}

float3 getNormal(float2 uv, int x = 0, int y = 0) {
   float2 offset = float2(x * 1.0/_ScreenParams.x, y * 1.0/_ScreenParams.y);
   return SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv + offset).rgb * 2.0 - 1.0; //SAMPLE_TEXTURE2D(_CameraNormalsTexture, sampler_CameraNormalsTexture, uv + offset).rgb * 2.0 - 1.0;
}

float depthEdgeIndicator(float2 uv) {
   float depth = getDepth(uv, 0, 0);
   float diff = 0.0;
   diff += getDepth(uv, 1, 0) - depth;
   diff += getDepth(uv, -1, 0) - depth;
   diff += getDepth(uv, 0, 1) - depth;
   diff += getDepth(uv, 0, -1) - depth;

   return floor(smoothstep(0.0001, 0.001, diff) * 10) / 10.;
}

float neighborNormalEdgeIndicator(float2 uv, int x, int y, float depth, float3 normal) {
   float depthDiff = getDepth(uv, x, y) - depth;
   float3 neighborNormal = getNormal(uv, x, y);

   // Edge pixels should yield to faces who's normals are closer to the bias normal.
   float3 normalEdgeBias = float3(1., 1., 1.);
   float normalDiff = dot(normal - neighborNormal, normalEdgeBias);
   float normalIndicator = clamp(smoothstep(-.01, .01, normalDiff), 0.0, 1.0);

   // Only the shallower pixel should detect the normal edge.
   float depthIndicator = clamp(sign(depthDiff * .25 + .0025), 0.0, 1);
   return (1.0 - dot(normal, neighborNormal)) * depthIndicator * normalIndicator;
}

float normalEdgeIndicator(float2 uv, float depth, float3 normal) {
   float indicator = 0.0;
   indicator += neighborNormalEdgeIndicator( uv, 0, -1, depth, normal);
   indicator += neighborNormalEdgeIndicator(uv, 0, 1, depth, normal);
   indicator += neighborNormalEdgeIndicator(uv, -1, 0, depth, normal);
   indicator += neighborNormalEdgeIndicator(uv, 1, 0, depth, normal);
   return indicator;
}

Varyings vert(Attributes input)
{
   Varyings output;
   // This extracts the index from the input and stores it in a global static variable that the other instancing macros rely on.
   UNITY_SETUP_INSTANCE_ID(input);
   UNITY_TRANSFER_INSTANCE_ID(input, output);

   output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
   output.positionCS = TransformWorldToHClip(output.positionWS);
   output.screenPosition = ComputeScreenPos(output.positionCS);
   output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
   output.normalWS = TransformObjectToWorldNormal(input.normalOS);
   return output;
}

half4 frag(Varyings input) : SV_TARGET
{
   float3 normal = normalize(input.normalWS);
   float NdotL = dot(normalize(_DirectionalLightDirections[0].xyz), normal);

   float2 screenUV = input.screenPosition.xy/input.screenPosition.w;
   //float depthBorder = depthEdgeIndicator(screenUV) > 0.0 ? 1.0 : 0.0;
   //float u = SAMPLE_DEPTH_TEXTURE(_DirectionalNoiseShadowAtlas, sampler_DirectionalNoiseShadowAtlas, screenUV);

   float3 shadowColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ShadowColor).rgb;
   float3 midtone = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _MidToneColor).rgb;
   float3 higlight = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _HighLightColor).rgb;

   float3 color;
   if (NdotL < UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _ShadowRange))
   {
       color = shadowColor;
   }
   else if (NdotL < UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial,_MidRange))
   {
      float include =  UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _IncludeNT) == 1;
      color = include ? lerp(shadowColor, midtone, saturate(NdotL)) : midtone;
      //float range = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
      //color = lerp(color, shadowColor, range);
   }
   else
   {
      //float range = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
      //color = lerp(higlight, midtone, range);
      color = higlight;
   }

   float attenuation = GetDirectionalShadowAttenuation(0, input.positionWS, normal);
   color = lerp(color, shadowColor, 1 - attenuation);
   
   // : DEPTH OUTLINE
   float threshold = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _DepthThreshold);
   float F = 10, D = getDepth(screenUV, 0, 0);
   float depthIndicator = (D - getDepth(screenUV, -1, 0)) * F > threshold ? 1 : 0;
   depthIndicator += (D - getDepth(screenUV, 1, 0)) * F > threshold ? 1 : 0;
   depthIndicator += (D - getDepth(screenUV, 0, 1)) * F > threshold ? 1 : 0;
   depthIndicator += (D - getDepth(screenUV, 0, -1)) * F > threshold ? 1 : 0;
   depthIndicator = depthIndicator > threshold ? 1 : 0;
   // :

   float t = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _NormalThreshold);
   float normalndicator = normalEdgeIndicator(screenUV, getDepth(screenUV), normal) > t ? 1 : 0;
   float indicator = max(depthIndicator, normalndicator);
   
   //color = getNormal(screenUV, 0, 0);
   //color = lerp(color * .02, float3(0,0,1), min(.5, normalndicator));
   //color = lerp(color, float3(1,0,0), min(.5, depthIndicator));

   float4 outline = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _OutlineColor);
   
   float3 surfaceNormal = normalize(normal + float3(0, 1, 0));
   float bavel = dot(_DirectionalLightDirections[0].xyz, surfaceNormal);
   color = lerp(color, lerp(color * 2.2 * max(.6, bavel), outline.rgb, outline.a), saturate(indicator - .1));
   return float4(color, 1) ; //  + (diff4.r > 0).rrr
}

#endif