#ifndef CUSTOM_LIGHT_INCLUDE
#define CUSTOM_LIGHT_INCLUDE

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#include "Shadows.hlsl"

CBUFFER_START(_CustomLight)
	int _DirectionalLightCount;
	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
	float attenuation;
};

int GetDirectionalLightCount() {
	return _DirectionalLightCount;
}

DirectionalShadowData GetDirectionalShadowData(int lightIndex)
{
	DirectionalShadowData data;
	data.strength = _DirectionalLightShadowData[lightIndex].x;
	data.tileIndex = _DirectionalLightShadowData[lightIndex].y;
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	return data;
}

float GetDirectionalShadowAttenuation(DirectionalShadowData data, float3 position, float3 normal)
{
	if (data.strength <= 0.0) {
		return 1.0;
	}

	float3 normalBias = normal * data.normalBias;
	float3 positionSTS = mul(_DirectionalShadowMatrices[data.tileIndex], float4(position + normalBias, 1.0)).xyz;
	float shadow = SampleDirectionalShadowAtlas(positionSTS);
	return lerp(1.0, shadow, data.strength);
}

float GetDirectionalShadowAttenuation(int index, float3 position, float3 normal)
{
	return GetDirectionalShadowAttenuation(GetDirectionalShadowData(index), position, normal);
}

float GetDirectionalShadowAttenuation(int index, Surface surface)
{
	return GetDirectionalShadowAttenuation(GetDirectionalShadowData(index), surface.position, surface.normal);
}


Light GetDirectionalLight(int index, Surface surfaceWS)
{
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	DirectionalShadowData shadowData = GetDirectionalShadowData(index);
	light.attenuation = GetDirectionalShadowAttenuation(shadowData, surfaceWS.position, surfaceWS.normal);
	//light.attenuation = lerp(1, pow(light.attenuation, 100), shadowData.strength); 
	light.attenuation = lerp(1, pow(abs(light.attenuation), exp(8)), shadowData.strength); 
	//light.attenuation = lerp(light.attenuation, shadowData.strength, 1-exp(.1));
	return light;
}

/*Light GetDirectionalLight(int index) {
Light light;
light.color = _DirectionalLightColors[index].rgb; //_DirectionLightColor;
light.direction = _DirectionalLightDirections[index].rgb; //_DirectionLightDirection;
//light.color = 1.;
//light.direction = float3(0., 1., 0.);
return light;
} */


#endif