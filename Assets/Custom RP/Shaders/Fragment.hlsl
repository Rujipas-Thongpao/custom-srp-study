#ifndef FRAGMENT_INCLUDED
#define FRAGMENT_INCLUDED

TEXTURE2D(_CameraDepthTexture);
TEXTURE2D(_CameraColorTexture);

struct Fragment{
    float2 positionSS;
    float depth;
    float2 screenUV;
    float bufferDepth;
};


Fragment GetFragment(float4 positionSS){
    Fragment f;
    f.positionSS = positionSS.xy;
    f.screenUV = f.positionSS/ _ScreenParams.xy; 
    f.depth = IsOrthographicCamera() ? OrthographicDepthBufferToLinear(positionSS.z) :  positionSS.w;
    f.bufferDepth= SAMPLE_DEPTH_TEXTURE_LOD(_CameraDepthTexture, sampler_point_clamp, f.screenUV,0.);
    f.bufferDepth = IsOrthographicCamera() ?
	OrthographicDepthBufferToLinear(f.bufferDepth) :
	LinearEyeDepth(f.bufferDepth, _ZBufferParams);
    return f;
}

float4 GetBufferColor(float2 screenUV, float2 offset = float2(0.,0.)){
    float2 uv = screenUV + offset;
    return SAMPLE_TEXTURE2D_LOD(_CameraColorTexture,  sampler_point_clamp, uv, 0 );
}



#endif 
