#ifndef CUSTOM_UNLIT_INPUT_INCLUDED
#define CUSTOM_UNLIT_INPUT_INCLUDED

#define INPUT_Prop(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeDistance)
    UNITY_DEFINE_INSTANCED_PROP(float, _NearFadeRange)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct InputConfig{
    float2 baseUV;
    float4 color;
    float3 flipbookUVB;
    bool isFlipbookBlending;
    bool isNearFade;
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
	float nearFadeAttenuation = (c.fragment.depth - nearFadeDistance)/nearFadeRange;
	baseMap.a *= 1.;
    }
    float4 color = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);
    return baseMap* color * c.color;
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
#endif
