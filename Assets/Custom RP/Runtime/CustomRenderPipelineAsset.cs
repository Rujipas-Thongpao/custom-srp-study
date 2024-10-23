using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custom Render Pipeline")]
public class CustomRenderPipelineAsset : RenderPipelineAsset
{

    [SerializeField]
    bool useDynamicBatching = true, useGPUInstancing = true, useSRPBatcher = true;

    [SerializeField]
    ShadowSettings shadows = default;


    [SerializeField]
    PostFXSettings postFXSettings = default;


    [SerializeField]
    ColorLUTResolution colorLUTResolution = ColorLUTResolution._32;

    [SerializeField]
    Shader cameraRendererShader = default;


    [SerializeField]
    CameraBufferSettings cameraBufferSettings = new CameraBufferSettings
    {
        allowHDR = true
    };

    protected override RenderPipeline CreatePipeline()
    {
        return new CustomRenderPipeline(
            useDynamicBatching, useGPUInstancing, useSRPBatcher, shadows, postFXSettings, colorLUTResolution, cameraRendererShader, cameraBufferSettings
        );
    }
}
public enum ColorLUTResolution
{
    _16 = 16, _32 = 32, _64 = 64
}
