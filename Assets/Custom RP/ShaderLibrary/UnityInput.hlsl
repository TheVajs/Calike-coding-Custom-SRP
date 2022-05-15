#ifndef CUSTOM_UNITY_INPUT_INCLUDED
#define CUSTOM_UNITY_INPUT_INCLUDED

// Uniform values. Ramain constant one per draw call. 
CBUFFER_START(UnityPerDraw)
	float4x4 unity_ObjectToWorld;
	float4x4 unity_WorldToObject;
	float4 unity_LODFade;
	real4 unity_WorldTransformParams; // contains some transform (real is float or half depending on the platform)
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection;
float3 _WorldSpaceCameraPos; // Unity makes camera position available here.

float4 _Time;			// (t/20, t, t*2, t*3)
float4 _SinTime;		// sin(t/8), sin(t/4), sin(t/2), sin(t)
float4 _CosTime;		// cos(t/8), cos(t/4), cos(t/2), cos(t)
float4 _TimeParameters; // t, sin(t), cos(t)

CBUFFER_START(_CustomParams)
	// x = 1 or -1 (-1 if projection is flipped)
	// y = near plane
	// z = far plane
	// w = 1/far plane
	float4 _ProjectionParams;

	// x = width
	// y = height
	// z = 1 + 1.0/width
	// w = 1 + 1.0/height
	float4 _ScreenParams;

	// x = 1 - far / near
	// y = far / near
	// z = x / far
	// w = y / far
	float4 _ZBufferParams;
CBUFFER_END

inline float4 ComputeScreenPos (float4 pos) {
	float4 o = pos * 0.5f;
	#if defined(UNITY_HALF_TEXEL_OFFSET)
	o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w * _ScreenParams.zw;
	#else
	o.xy = float2(o.x, o.y*_ProjectionParams.x) + o.w;
	#endif
 
	o.zw = pos.zw;
	return o;
}

inline float LinearEyeDepth( float z )
{
	return 1.0 / (_ZBufferParams.z * z + _ZBufferParams.w);
}

// https://stackoverflow.com/questions/5149544/can-i-generate-a-random-number-inside-a-pixel-shader
float random(float2 p){ return frac((cos(dot(p,float2(23.14069263277926,2.665144142690225)))*12345.6789)); }

#endif
