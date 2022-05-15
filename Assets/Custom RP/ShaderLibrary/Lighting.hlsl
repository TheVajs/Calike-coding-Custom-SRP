#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

float3 IncomingLight(Surface surface, Light light) {
	//float at = .96 > light.attenuation ? 0 : light.attenuation;
	//at = saturate(at);
	return saturate(dot(surface.normal, light.direction) * light.attenuation) * light.color;
}

float3 CalculateLighting(Surface surface, BRDF brdf, Light light) {
	return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 CalculateLighting(Surface surfaceWS, BRDF brdf) {
	float3 color = 0.0;
	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		color += CalculateLighting(surfaceWS, brdf, GetDirectionalLight(i, surfaceWS));
	}
	return color;
}

#endif