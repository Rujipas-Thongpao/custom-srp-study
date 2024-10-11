using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class CustomRenderPipeline : RenderPipeline
{

    CameraRenderer renderer = new CameraRenderer();

    bool useDynamicBatching, useGPUInstancing;

    ShadowSettings shadowSettings;
    PostFXSettings postFXSettings;

    public CustomRenderPipeline(
        bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher,
        ShadowSettings shadowSettings, PostFXSettings _postFxSettings
    )
    {
        InitializeForEditor();
        this.shadowSettings = shadowSettings;
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        this.postFXSettings = _postFxSettings;
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
                shadowSettings, this.postFXSettings
            );
        }
    }
}
