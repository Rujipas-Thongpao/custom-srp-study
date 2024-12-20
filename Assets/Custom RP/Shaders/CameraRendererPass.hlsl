#ifndef CAMERA_RENDERER
#define CAMERA_RENDERER


TEXTURE2D(_SourceTexture);

struct Varyings {
    float4 positionCS : SV_POSITION;
    float2 screenUV : VAR_SCREEN_UV;
};


Varyings DefaultPassVertex(uint vertexID : SV_VertexID){
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


float4 CopyPassFragment(Varyings input) : SV_TARGET{
    return SAMPLE_TEXTURE2D_LOD(_SourceTexture, sampler_linear_clamp, input.screenUV,0);
}


#endif
