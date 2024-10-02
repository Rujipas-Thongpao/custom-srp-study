#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

float3 IncomingLight (Surface surface, Light light) {
	return
		saturate(dot(surface.normal, light.direction) * light.attenuation) *
		light.color;
}

float3 GetLighting (Surface surface, BRDF brdf, Light light) {
	return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting (Surface surfaceWS, BRDF brdf, GI gi) {
	ShadowData shadowData = GetShadowData(surfaceWS);
	shadowData.shadowMask = gi.shadowMask;
	float3 reflection = gi.specular * brdf.specular;
	reflection /= (brdf.roughness  * brdf.roughness + 1);
	float3 color = (gi.diffuse * brdf.diffuse + reflection) * surfaceWS.occlusion;

	for (int i = 0; i < GetDirectionalLightCount(); i++) {
		Light light = GetDirectionalLight(i, surfaceWS, shadowData);
		color += GetLighting(surfaceWS, brdf, light);
	}

	for (int i = 0; i < GetOtherLightCount(); i++) {
		Light Otherlight = GetOtherLight(i, surfaceWS, shadowData);
		color += GetLighting(surfaceWS, brdf, Otherlight);
	}
	return color;
}

#endif
