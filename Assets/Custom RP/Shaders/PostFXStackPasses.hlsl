#ifndef CUSTOM_POST_FX_PASSES_INCLUDED
#define CUSTOM_POST_FX_PASSES_INCLUDED


TEXTURE2D(_PostFXSource);
TEXTURE2D(_PostFXSource2);
SAMPLER(sampler_linear_clamp);
bool _useBicubic;
float4 _BloomThreshold;
float _BloomIntensity;
float4 _ColorAdjustments;
float4 _ColorFilter;
float4 _WhiteBalance;
float4 _SplitToneShadow;
float4 _SplitToneHighlight;
float _SplitToneBalance;
float4 _ChannelMixerRed, _ChannelMixerGreen, _ChannelMixerBlue;
float4 _SMHShadows, _SMHMidtones, _SMHHighlights, _SMHRange;
float4 _ColorLUTParameters, _ColorGradingLUTParameters;
TEXTURE2D(_ColorGradingLUT);
float _ColorGradingLUTInLogId;

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

float4 _PostFXSource_TexelSize; // texel Size of _PostFXSource which is assign by unity's black magic

float4 GetSourceTexelSize(){
    return _PostFXSource_TexelSize;
}

float3 ColorGradingSMH(float3 color){
    float lum = Luminance(color);
    float shadowWeight = 1. - smoothstep(_SMHRange.x, _SMHRange.y, lum);
    float highlightWeight = smoothstep(_SMHRange.z, _SMHRange.w, lum);
    float midtoneWeight = 1. - shadowWeight - highlightWeight;

    return color * _SMHShadows.rgb * shadowWeight +
    color * _SMHMidtones.rgb * midtoneWeight +
    color * _SMHHighlights.rgb * highlightWeight;
}

float3 ColorGradingChannelMixer(float3 color){
    return max(0,mul(
	float3x3(_ChannelMixerRed.rgb, _ChannelMixerGreen.rgb, _ChannelMixerBlue.rgb),
	color
    ));
}

float3 ColorGradingSplitTone( float3 color ){
    color = PositivePow(color, 1./2.2);

    float t = saturate(Luminance(saturate(color)) + _SplitToneBalance);
    float3 s = lerp(.5, _SplitToneShadow.rgb, 1.-t);
    float3 h = lerp(.5, _SplitToneHighlight.rgb, t);

    color = SoftLight(color,s);
    color = SoftLight(color,h);

    return PositivePow(color, 2.2);
}
float3 ColorGradingWhiteBalance( float3 color){
    color = LinearToLMS(color);
    color *= _WhiteBalance.rgb;
    return LMSToLinear(color);
}
float3 ColorGradingExposure(float3 color){
    return color * _ColorAdjustments.x;
}

float3 ColorGradingContrast(float3 color){
    color = LinearToLogC(color);
    color = (color - ACEScc_MIDGRAY) * _ColorAdjustments.y + ACEScc_MIDGRAY;
    return LogCToLinear(color);
}

float3 ColorGradingColorFilter(float3 color){
    return max(color * _ColorFilter.rgb,0.);
}

float3 ColorGradingHueShift(float3 color){
    float3 colorHSV = RgbToHsv(color);
    float hue = colorHSV.x + _ColorAdjustments.z;
    colorHSV.x = RotateHue(hue, 0.0,1.0);
    return HsvToRgb(colorHSV);
}


float3 ColorGradingSaturation(float3 color){
    float3 lum = Luminance(color);
    return (color-lum) * _ColorAdjustments.w + lum;
}

float3 ColorGrade(float3 color){
    color = ColorGradingWhiteBalance(color);
    color = ColorGradingExposure(color);
    color = ColorGradingContrast(color);
    color = ColorGradingColorFilter(color);
    color = ColorGradingSplitTone(color);
    color = ColorGradingChannelMixer(color);
    color = ColorGradingSMH(color);
    color = ColorGradingHueShift(color);
    color = ColorGradingSaturation(color);

    return color;
}

float3 GetColorGradedLUT(float2 uv){
    float3 color = GetLutStripValue(uv, _ColorLUTParameters);
    return ColorGrade(_ColorGradingLUTInLogId ? LogCToLinear(color) : color);
}

float3 ApplyColorGradingLUT(float3 color){
    return ApplyLut2D(
	TEXTURE2D_ARGS( _ColorGradingLUT, sampler_linear_clamp),
	saturate(color),
	_ColorGradingLUTParameters.xyz
    );
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

// Note that because we do horizontal first meaning the input is alreayd half resolution, meaning that we have to only do 5 loop
float4 BloomVerticalPassFragment (Varyings input) : SV_TARGET {
	float3 color = 0.0;
	float offsets[] = {
		-3.23076923, -1.38461538, 0.0, 1.38461538, 3.23076923
	};
	float weights[] = {
		0.07027027, 0.31621622, 0.22702703, 0.31621622, 0.07027027
	};
	for (int i = 0; i < 5; i++) {
		float offset = offsets[i] * GetSourceTexelSize().y;
		color += GetSource(input.screenUV + float2(0.0, offset)).rgb * weights[i];
	}
	return float4(color, 1.0);
}

float4 BloomCombinePassFragment(Varyings input) : SV_TARGET {
    float3 lowRes = GetSource(input.screenUV).rgb;
    if(_useBicubic){
	lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    float3 highRes = GetSource2(input.screenUV).rgb;
    return float4(lowRes * _BloomIntensity+ highRes, 1.0) ;
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

float4 BloomPrefilterFirefliesPassFragment (Varyings input) : SV_TARGET {
    float3 color = 0.0;
    float weightSum = 0.0;
    float2 offsets[] = {
	float2(0.,0.),
	float2(-1.,-1.),
	float2(0.,-1.),
	float2(1.,-1.),
	float2(-1.,0.),
	float2(0.,0.),
	float2(1.,0.),
	float2(-1.,1.),
	float2(0.,1.),
	float2(1.,1.),
    };


    for(int i =0;i<9;i++){
	float3 c = GetSource(input.screenUV + offsets[i] * GetSourceTexelSize().xy * 2.0).rgb;
	c = ApplyBloomThreshold(c);
	float w = 1.0/(Luminance(c) + 1.);
	color += c * w;
	weightSum += w;
    }
    color /= weightSum;
    return float4(color, 1.0);
}


float4 BloomScatterPassFragment(Varyings input) : SV_TARGET {
    float3 lowRes = GetSource(input.screenUV).rgb;
    if(_useBicubic){
	lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    float3 highRes = GetSource2(input.screenUV).rgb;
    return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0) ;
}

float4 BloomScatterFinalPassFragment(Varyings input) : SV_TARGET {
    float3 lowRes = GetSource(input.screenUV).rgb;
    if(_useBicubic){
	lowRes = GetSourceBicubic(input.screenUV).rgb;
    }
    float3 highRes = GetSource2(input.screenUV).rgb;
    lowRes += highRes - ApplyBloomThreshold(highRes);
    return float4(lerp(highRes, lowRes, _BloomIntensity), 1.0) ;
}

float4 ToneMappingNonePassFragment(Varyings input) : SV_TARGET{
    float3 color = GetColorGradedLUT(input.screenUV);
    return float4(color,0.);
}

float4 ToneMappingReinhardPassFragment(Varyings input)  : SV_TARGET{
    float3 color = GetColorGradedLUT(input.screenUV);
    color/= color+ 1.;
    return float4(color,0.);
}

float4 ToneMappingNeutralPassFragment(Varyings input)  : SV_TARGET{
    float3 color = GetColorGradedLUT(input.screenUV);
    color = NeutralTonemap(color);
    return float4(color,0.);
}
float4 ToneMappingACESPassFragment(Varyings input)  : SV_TARGET{
    float3 color = GetColorGradedLUT(input.screenUV);
    color = AcesTonemap(unity_to_ACES(color));
    return float4(color,0.);
}

float4 FinalPassFragment(Varyings input): SV_TARGET{
    float4 color = GetSource(input.screenUV);
    color.rgb = ApplyColorGradingLUT(color);
    return color;
}

#endif
