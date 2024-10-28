#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

#define INPUT_Prop(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_DistortionMap);
SAMPLER(sampler_DistortionMap);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticleDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _SoftParticleRange)
    UNITY_DEFINE_INSTANCED_PROP(float, _DistortionStrength)
    UNITY_DEFINE_INSTANCED_PROP(float, _DistortionBlend)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct InputConfig{
    float2 baseUV;
    float4 color;
    float3 flipbookUVB;
    bool isFlipbookBlending;
    bool isNearFade;
    bool isSoftParticle;
    Fragment fragment;
};

InputConfig GetInputConfig(float4 positionSS,float2 baseUV){
    InputConfig c;
    c.baseUV = baseUV;
    c.color = 1.;
    c.flipbookUVB = 0.;
    c.isFlipbookBlending = false;
    c.fragment = GetFragment(positionSS);
    c.isNearFade = false;
    c.isSoftParticle = false;

    return c;
}



float2 TransformBaseUV (float2 baseUV) {
    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
	// xy = scale, zw = offset.
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase (InputConfig c) {
    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
    if(c.isFlipbookBlending){
	float4 flipbookMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.flipbookUVB.xy);
	baseMap = lerp(baseMap, flipbookMap, c.flipbookUVB.z);
    }
    if(c.isNearFade){
	float nearFadeDistance = INPUT_Prop(_NearFadeDistance);
	float nearFadeRange = INPUT_Prop(_NearFadeRange);
	float nearFadeAttenuation = saturate((c.fragment.depth - nearFadeDistance)/nearFadeRange);
	baseMap.a *= nearFadeAttenuation;
    }
    if(c.isSoftParticle){
	float depthDiff = c.fragment.bufferDepth - c.fragment.depth;
	float softDis = INPUT_Prop(_SoftParticleDistance);
	float softRange = INPUT_Prop(_SoftParticleRange);
	float softAtten = saturate((depthDiff - softDis)/softRange);
	baseMap.a *= softAtten;
    }


    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    return baseMap* color * c.color;
}

float2 GetDistortion(InputConfig c){
    float4 rawMap = SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, c.baseUV);
	   if (c.isFlipbookBlending){
	float4 nextRawMap = SAMPLE_TEXTURE2D(_DistortionMap, sampler_DistortionMap, c.flipbookUVB.xy); 
	float rawMap = lerp(rawMap, nextRawMap, c.flipbookUVB.z);
	   }
    return DecodeNormal(rawMap, INPUT_Prop(_DistortionStrength)).xy;
}


float GetCutoff (InputConfig c) {
    return UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff);
}
float GetMetallic (InputConfig c) {
    return 0.0;
}
float GetSmoothness (InputConfig c) {
    return 0.0;
}
float3 GetEmission(InputConfig c){
    return GetBase(c).rgb;
}
float GetDistortionBlend(InputConfig c){
    return INPUT_Prop(_DistortionBlend);
}
#endif
