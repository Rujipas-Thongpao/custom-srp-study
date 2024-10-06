#ifndef CUSTOM_LIGHT_INCLUDED
#define CUSTOM_LIGHT_INCLUDED

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
#define MAX_OTHER_LIGHT_COUNT 64


CBUFFER_START(_CustomLight)
	int _DirectionalLightCount;
	int _OtherLightCount;

	float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
	float4 _DirectionalLightShadowData[MAX_DIRECTIONAL_LIGHT_COUNT];

	float4 _OtherLightColors[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightPositions[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightDirections[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightAngles[MAX_OTHER_LIGHT_COUNT];
	float4 _OtherLightShadowData[MAX_OTHER_LIGHT_COUNT];
CBUFFER_END

struct Light {
	float3 color;
	float3 direction;
	float attenuation;
};

int GetDirectionalLightCount () {
	return _DirectionalLightCount;
}
OtherShadowData GetOtherShadowData (int lightIndex) {
	OtherShadowData data;
	data.strength = _OtherLightShadowData[lightIndex].x; 
	data.tileIndex = _OtherLightShadowData[lightIndex].y;
	data.shadowMaskChannel = _OtherLightShadowData[lightIndex].w;

	// NOTE : because these 2 value is from "Light" not "Shadow"
	data.lightPositionWS = 0.0;
	data.spotDirectionWS = 0.0;
	return data;
}

DirectionalShadowData GetDirectionalShadowData (
	int lightIndex, ShadowData shadowData
) {
	DirectionalShadowData data;
	data.strength =
		_DirectionalLightShadowData[lightIndex].x;
	data.tileIndex =
		_DirectionalLightShadowData[lightIndex].y + shadowData.cascadeIndex;
	data.normalBias = _DirectionalLightShadowData[lightIndex].z;
	data.shadowMaskChannel = _DirectionalLightShadowData[lightIndex].w;
	return data;
}

Light GetDirectionalLight (int index, Surface surfaceWS, ShadowData shadowData) {
	Light light;
	light.color = _DirectionalLightColors[index].rgb;
	light.direction = _DirectionalLightDirections[index].xyz;
	DirectionalShadowData dirShadowData =
		GetDirectionalShadowData(index, shadowData);
	light.attenuation =
		GetDirectionalShadowAttenuation(dirShadowData, shadowData, surfaceWS);
	return light;
}




Light GetOtherLight(int index, Surface surfaceWS, ShadowData shadowData){
	Light light;

	float3 spotDir = _OtherLightDirections[index].xyz;
	float3 position = _OtherLightPositions[index].xyz;

	OtherShadowData otherShadowData = GetOtherShadowData(index);

	light.color = _OtherLightColors[index].rgb;
	float3 d = position - surfaceWS.position; // distance

	light.direction = normalize(d);
	float distanceSquare = max(dot(d, d), 0.00001);
	float rangeAttenuation = Square(saturate(1.0 - Square(distanceSquare * _OtherLightPositions[index].w)));

	float4 spotAngle = _OtherLightAngles[index];
	float spotAttenuation = Square(saturate(dot(light.direction, spotDir) * spotAngle.x + spotAngle.y));
	light.attenuation = GetOtherShadowAttenuation(otherShadowData, shadowData, surfaceWS) * rangeAttenuation * spotAttenuation;

	otherShadowData.lightPositionWS = position;
	otherShadowData.spotDirectionWS = spotDir;
	return light;
}

int GetOtherLightCount(){
	return _OtherLightCount;
}

#endif
