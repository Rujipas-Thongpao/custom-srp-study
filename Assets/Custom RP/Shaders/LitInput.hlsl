#ifndef CUSTOM_LIT_INPUT_INCLUDED
#define CUSTOM_LIT_INPUT_INCLUDED

#define INPUT_Prop(name) UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, name)

TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);
TEXTURE2D(_DetailMap);
SAMPLER(sampler_DetailMap);
TEXTURE2D(_EmissionMap);
TEXTURE2D(_MaskMap);
TEXTURE2D(_NormalMap);
TEXTURE2D(_DetailNormalMap);


UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _NormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailNormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailAlbedo)
    UNITY_DEFINE_INSTANCED_PROP(float, _DetailSmoothness)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _OcclusionStrength)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct InputConfig {
	float2 baseUV;
	float2 detailUV;
	bool useMask;
	bool useDetail;
};

InputConfig GetInputConfig (float2 baseUV, float2 detailUV = 0.0) {
	InputConfig c;
	c.baseUV = baseUV;
	c.detailUV = detailUV;
	c.useMask = false;
	c.useDetail = false;
	return c;
}


float4 GetMask(InputConfig c){
    return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, c.baseUV);
}

float3 GetEmission(InputConfig c){
    float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, c.baseUV); // use the same as base map
    float4 color = INPUT_Prop(_EmissionColor);
    return (map * color).rgb;
}
float2 TransformBaseUV (float2 baseUV) {
    float4 baseST = INPUT_Prop( _BaseMap_ST);
	// xy = scale, zw = offset.
    return baseUV * baseST.xy + baseST.zw;
}


float2 TransformDetailUV (float2 detailUV) {
    float4 detailST = INPUT_Prop(_DetailMap_ST);
    return detailUV * detailST.xy + detailST.zw;
}

float4 GetDetail(InputConfig c){
    float4 map= SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, c.detailUV);
    return map * 2.0 - 1.0; // convert from 0 - 1 to -1 - 1
}

float GetCutoff (InputConfig c) {
    return INPUT_Prop( _Cutoff);
}
float GetMetallic (InputConfig c) {
    float4 m = GetMask(c).r;
    return INPUT_Prop( _Metallic) * m;
}
float GetSmoothness (InputConfig c) {
    float s = GetMask(c).a;
    float smoothness = INPUT_Prop( _Smoothness) * s;
    float smoothnessDetail = GetDetail(c).b * INPUT_Prop(_DetailSmoothness);
    float m = GetMask(c).b;
    float pushedSmoothness = smoothnessDetail < 0.0 ? 0.0 : 1.0;
    smoothness = lerp(smoothness, pushedSmoothness, abs(smoothnessDetail) * m);
    return smoothness;
}
float GetOcclusion(InputConfig c){
    float strength = INPUT_Prop(_OcclusionStrength);
    float occlusion = GetMask(c).g;
    return lerp(occlusion, 1.0, strength);
}

float3 GetNormalTS (InputConfig c) {
    float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, c.baseUV);
    float scale = INPUT_Prop(_NormalScale);
    float3 normal = DecodeNormal(map, scale);
    map = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_DetailMap, c.detailUV);
    scale = INPUT_Prop(_DetailNormalScale) * GetMask(c).b;
    float3 detail = DecodeNormal(map, scale);
    normal = BlendNormalRNM(normal, detail);
    return normal;
}

float4 GetBase(InputConfig c){
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, c.baseUV);
    float4 mask = GetMask(c).b; // get avaliable area for detail map
    float4 color = INPUT_Prop(_BaseColor);

    float4 detail = GetDetail(c).r *INPUT_Prop(_DetailAlbedo)* mask;
    float pushedColor = detail < 0.0 ? 0.0 : 1.0;
    map.rgb = lerp(sqrt(map.rgb), pushedColor, abs(detail));
    map.rgb *= map.rgb;
    return map * color;
}
#endif
