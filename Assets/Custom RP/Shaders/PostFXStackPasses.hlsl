#ifndef CUSTOM_POST_FX_PASSES_INCLUDED
#define CUSTOM_POST_FX_PASSES_INCLUDED


TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);
SAMPLER(sampler_linear_clamp);
bool _useBicubic;
float4 _BloomThreshold;

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"

float4 _PostFXSource_TexelSize; // texel Size of _PostFXSource which is assign by unity's black magic

float4 GetSourceTexelSize(){
    return _PostFXSource_TexelSize;
}

float4 GetSource(float2 screenUV){
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource, sampler_linear_clamp, screenUV,0);
}

float4 GetSource2(float2 screenUV){
    return SAMPLE_TEXTURE2D_LOD(_PostFXSource2, sampler_linear_clamp, screenUV,0);
}


float4 GetSourceBicubic(float2 screenUV){
    return SampleTexture2DBicubic(
	TEXTURE2D_ARGS(_PostFXSource, sampler_linear_clamp), 
	screenUV,
	_PostFXSource_TexelSize.zwxy
	, 1.0, 0.0
    );
}


struct Varyings {
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};

Varyings DefaultPassVertex (uint vertexID : SV_VertexID) {
    Varyings output;
    // This make 1 Triangle cover all screen space
    output.positionCS = float4(
	vertexID <= 1 ? -1.0 : 3.0, // X coordinate
	vertexID == 1 ? 3.0 : -1.0, // Y coordinate
	0.0, 1.0
    );
    output.screenUV = float2(
	vertexID <= 1 ? 0.0 : 2.0,
	vertexID == 1 ? 2.0 : 0.0
    );

    // NOTE : because sometimes the textrue in flip upsidedown, 
    // we have to look at _ProjectionPrams whether we need to munually flip or not

    if(_ProjectionParams.x < 0.0){
	output.screenUV.y = 1.0 - output.screenUV.y;
    }

    ///  ---------------------------------------------------

    return output;
}



float4 CopyPassFragment(Varyings input) : SV_TARGET {

    return GetSource(input.screenUV);
}




float4 BloomHorizontalPassFragment(Varyings input) : SV_TARGET{
    float3 color = 0.0;
    float offsets[] = {
	-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
    };
    float weights[] = {
	0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
	0.19459459, 0.12162162, 0.05405405, 0.01621622
    };



    for(int i =0;i<9;i++){
	float offset = offsets[i] *  GetSourceTexelSize().x * 2; // because we have to down scale by half, so we have to multiply by 2
	color += GetSource(input.screenUV + float2(offset,0.)).rgb * weights[i];
    }
    return float4(color, 1.);
}

float4 BloomVerticalPassFragment(Varyings input) : SV_TARGET{
    float3 color = 0.0;
    float offsets[] = {
	-4.0, -3.0, -2.0, -1.0, 0.0, 1.0, 2.0, 3.0, 4.0
    };
    float weights[] = {
	0.01621622, 0.05405405, 0.12162162, 0.19459459, 0.22702703,
	0.19459459, 0.12162162, 0.05405405, 0.01621622
    };

    for(int i =0;i<9;i++){
	float offset = offsets[i] *  GetSourceTexelSize().y * 2; // because we have to down scale by half, so we have to multiply by 2
	color += GetSource(input.screenUV + float2(0.,offset)).rgb * weights[i];
    }
    return float4(color, 1.);
}

float4 BloomCombinePassFragment(Varyings input) : SV_TARGET {
    float3 lowRes = GetSource(input.screenUV).rgb;
    if(_useBicubic){
	lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    float3 highRes = GetSource2(input.screenUV).rgb;
    return float4(lowRes + highRes, 1.0);
}

float3 ApplyBloomThreshold (float3 color) {
    float brightness = Max3(color.r, color.g, color.b); // brightness in this case is define by the maximum value between 3 channel
    float soft = brightness + _BloomThreshold.y;
    soft = clamp(soft, 0.0, _BloomThreshold.z);
    soft = soft * soft * _BloomThreshold.w;
    float contribution = max(soft, brightness - _BloomThreshold.x);
    contribution /= max(brightness, 0.00001);
    return color * contribution;
}

float4 BloomPrefilterPassFragment (Varyings input) : SV_TARGET {
    float3 color = ApplyBloomThreshold(GetSource(input.screenUV).rgb);
    return float4(color, 1.0);
}



#endif
