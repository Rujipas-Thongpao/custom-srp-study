using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{

    CameraRenderer renderer;

    bool useDynamicBatching, useGPUInstancing;

    ShadowSettings shadowSettings;
    PostFXSettings postFXSettings;


    ColorLUTResolution colorLUTResolution;
    CameraBufferSettings cameraBufferSettings;

    public CustomRenderPipeline(
        bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,
        ShadowSettings shadowSettings, PostFXSettings _postFxSettings,
        ColorLUTResolution _lutRes, Shader _cameraRendererShader, CameraBufferSettings _cameraBufferSettings
    )
    {
        InitializeForEditor();
        this.shadowSettings = shadowSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.postFXSettings = _postFxSettings;
        this.colorLUTResolution = _lutRes;
        this.renderer = new CameraRenderer(_cameraRendererShader);
        this.cameraBufferSettings = _cameraBufferSettings;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }

    protected override void Render(
        ScriptableRenderContext context, Camera[] cameras
    )
    { }

    protected override void Render(
        ScriptableRenderContext context, List<Camera> cameras
    )
    {
        for (int i = 0; i < cameras.Count; i++)
        {
            renderer.Render(
                context, cameras[i], useDynamicBatching, useGPUInstancing,
                shadowSettings, this.postFXSettings, this.colorLUTResolution, this.cameraBufferSettings
            );
        }
    }
}
