#ifndef CUSTOM_META_PASS_INCLUDED
#define CUSTOM_META_PASS_INCLUDED

#include "../ShaderLibrary/Surface.hlsl"
#include "../ShaderLibrary/Shadows.hlsl"
#include "../ShaderLibrary/Light.hlsl"
#include "../ShaderLibrary/BRDF.hlsl"

struct Attributes {
    float3 positionOS : POSITION;
    float2 baseUV : TEXCOORD0;
    float2 lightMapUV : TEXCOORD1;
};
struct Varyings {
    float4 lightMapPositionCS : SV_POSITION;
    float2 baseUV : VAR_BASE_UV;
};
Varyings MetaPassVertex (Attributes input) {
    Varyings output;
    float3 lightMapPosition = 0.0;
    lightMapPosition.xy = input.lightMapUV * unity_LightmapST.xy + unity_LightmapST.zw;
    lightMapPosition.z = input.positionOS.z > 0.0 ? FLT_MIN : 0.0; // FLT_MIN is smallest number that is greater than 0
    output.lightMapPositionCS = TransformWorldToHClip(lightMapPosition);

    output.baseUV = TransformBaseUV(input.baseUV);
    return output;
}
float4 MetaPassFragment (Varyings input) : SV_TARGET {
    InputConfig config = GetInputConfig(input.lightMapPositionCS, input.baseUV);
    float4 base = GetBase(config);
    Surface surface;
    ZERO_INITIALIZE(Surface, surface);
    surface.color = base.rgb;
    surface.metallic = GetMetallic(config);
    surface.smoothness = GetSmoothness(config);
    BRDF brdf = GetBRDF(surface);
    float4 meta = 0.0;
    if (unity_MetaFragmentControl.x) {
        meta = float4(brdf.diffuse, 1.0);
        meta.rgb += brdf.specular * brdf.roughness * 0.5;
        meta.rgb = min(
            PositivePow(meta.rgb, unity_OneOverOutputBoost), unity_MaxOutputValue
        );

    }
    else if(unity_MetaFragmentControl.y){
        meta = float4(GetEmission(config), 1.0);
    }
    return meta;
    // return float4(1.,1.,1.,1.);
}
#endif
