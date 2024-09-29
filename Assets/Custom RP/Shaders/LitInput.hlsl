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
    UNITY_DEFINE_INSTANCED_PROP(float4, _NormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float4, _DetailNormalScale)
    UNITY_DEFINE_INSTANCED_PROP(float4, _DetailMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float1, _DetailAlbedo)
    UNITY_DEFINE_INSTANCED_PROP(float1, _DetailSmoothness)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
    UNITY_DEFINE_INSTANCED_PROP(float, _OcclusionStrength)
    UNITY_DEFINE_INSTANCED_PROP(float, _Cutoff)
    UNITY_DEFINE_INSTANCED_PROP(float, _Metallic)
    UNITY_DEFINE_INSTANCED_PROP(float, _Smoothness)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)




float4 GetMask(float2 baseUV){
    return SAMPLE_TEXTURE2D(_MaskMap, sampler_BaseMap, baseUV);
}

float3 GetEmission(float2 baseUV){
    float4 map = SAMPLE_TEXTURE2D(_EmissionMap, sampler_BaseMap, baseUV); // use the same as base map
    float4 color = INPUT_Prop(_EmissionColor);
    return (map * color).rgb;
}
float2 TransformBaseUV (float2 baseUV) {
    float4 baseST = INPUT_Prop( _BaseMap_ST);
	// xy = scale, zw = offset.
    return baseUV * baseST.xy + baseST.zw;
}

float4 GetBase (float2 baseUV) {
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
    float4 color = INPUT_Prop( _BaseColor);
    return map * color;
}

float2 TransformDetailUV (float2 detailUV) {
    float4 detailST = INPUT_Prop(_DetailMap_ST);
    return detailUV * detailST.xy + detailST.zw;
}

float4 GetDetail(float2 detailUV){
    float4 map= SAMPLE_TEXTURE2D(_DetailMap, sampler_DetailMap, detailUV);
    return map * 2.0 - 1.0; // convert from 0 - 1 to -1 - 1
}

float GetCutoff (float2 baseUV) {
    return INPUT_Prop( _Cutoff);
}
float GetMetallic (float2 baseUV) {
    float4 m = GetMask(baseUV).r;
    return INPUT_Prop( _Metallic) * m;
}
float GetSmoothness (float2 baseUV, float2 detailUV = 0.0) {
    float s = GetMask(baseUV).a;
    float smoothness = INPUT_Prop( _Smoothness) * s;
    float smoothnessDetail = GetDetail(detailUV).b * INPUT_Prop(_DetailSmoothness);
    float m = GetMask(baseUV).b;
    float pushedSmoothness = smoothnessDetail < 0.0 ? 0.0 : 1.0;
    smoothness = lerp(smoothness, pushedSmoothness, abs(smoothnessDetail) * m);
    return smoothness;
}
float GetOcclusion(float2 baseUV){
    float strength = INPUT_Prop(_OcclusionStrength);
    float occlusion = GetMask(baseUV).g;
    return lerp(occlusion, 1.0, strength);
}

float3 GetNormalTS (float2 baseUV, float2 detailUV = 0.0) {
    float4 map = SAMPLE_TEXTURE2D(_NormalMap, sampler_BaseMap, baseUV);
    float scale = INPUT_Prop(_NormalScale);
    float3 normal = DecodeNormal(map, scale);

    float4 detailMap = SAMPLE_TEXTURE2D(_DetailNormalMap, sampler_BaseMap, baseUV);
    float4 detailScale = INPUT_Prop(_DetailNormalScale) * GetMask(baseUV).b;
    float3 detail = DecodeNormal(detailMap, detailScale);

    normal = BlendNormalRNM(normal, detail);
    
    return map.xyz;
}

float4 GetBaseDetail(float2 baseUV, float2 detailUV = 0.0){
    float4 map = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, baseUV);
    float4 mask = GetMask(baseUV).b; // get avaliable area for detail map
    float4 color = INPUT_Prop(_BaseColor);

    float4 detail = GetDetail(detailUV).r *INPUT_Prop(_DetailAlbedo)* mask;
    float pushedColor = detail < 0.0 ? 0.0 : 1.0;
    map.rgb = lerp(sqrt(map.rgb), pushedColor, abs(detail));
    map.rgb *= map.rgb;
    return map;
}
#endif
