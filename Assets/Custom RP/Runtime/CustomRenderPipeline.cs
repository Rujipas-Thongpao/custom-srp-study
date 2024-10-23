using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{

    CameraRenderer renderer;

    bool useDynamicBatching, useGPUInstancing;

    ShadowSettings shadowSettings;
    PostFXSettings postFXSettings;

    bool allowHDR;

    ColorLUTResolution colorLUTResolution;

    public CustomRenderPipeline(
        bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,
        ShadowSettings shadowSettings, PostFXSettings _postFxSettings, bool _allowHDR,
        ColorLUTResolution _lutRes, Shader _cameraRendererShader
    )
    {
        InitializeForEditor();
        this.shadowSettings = shadowSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.postFXSettings = _postFxSettings;
        this.allowHDR = _allowHDR;
        this.colorLUTResolution = _lutRes;
        this.renderer = new CameraRenderer(_cameraRendererShader);
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
                shadowSettings, this.postFXSettings, this.allowHDR, this.colorLUTResolution
            );
        }
    }
}
