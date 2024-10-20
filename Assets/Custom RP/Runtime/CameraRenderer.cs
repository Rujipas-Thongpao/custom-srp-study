using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    const string bufferName = "Render Camera";

    static ShaderTagId
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
        litShaderTagId = new ShaderTagId("CustomLit");

    static int frameBufferId = Shader.PropertyToID("_CameraFrameBuffer");

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;

    Camera camera;

    CullingResults cullingResults;
    bool allowHDR;

    Lighting lighting = new Lighting();
    PostFXStack postFXStack = new PostFXStack();

    ColorLUTResolution colorLUTResolution;

    public void Render(
        ScriptableRenderContext _context, Camera _camera,
        bool _useDynamicBatching, bool _useGPUInstancing,
        ShadowSettings _shadowSettings, PostFXSettings _postFxSettings, bool _allowHDR,
        ColorLUTResolution _lutRes
    )
    {
        this.context = _context;
        this.camera = _camera;
        this.allowHDR = _allowHDR && camera.allowHDR;
        this.colorLUTResolution = _lutRes;

        // Set up
        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull(_shadowSettings.maxDistance))
        {
            return;
        }

        buffer.BeginSample(SampleName);
        ExecuteBuffer();

        lighting.Setup(_context, cullingResults, _shadowSettings);
        postFXStack.Setup(_context, _camera, _postFxSettings, _allowHDR, _lutRes);
        buffer.EndSample(SampleName);
        Setup();

        // Actually draw
        DrawVisibleGeometry(_useDynamicBatching, _useGPUInstancing);
        DrawUnsupportedShaders();
        DrawGizmosBeforeFX();
        if (postFXStack.IsActive)
        {
            postFXStack.Render(frameBufferId);
        }
        DrawGizmosAfterFX();

        Cleanup();

        Submit();
    }

    bool Cull(float maxShadowDistance)
    {
        if (camera.TryGetCullingParameters(out ScriptableCullingParameters p))
        {
            p.shadowDistance = Mathf.Min(maxShadowDistance, camera.farClipPlane);
            cullingResults = context.Cull(ref p);
            return true;
        }
        return false;
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;

        if (postFXStack.IsActive)
        {
            // NOTE : we use this render texture as an immediate texture for post-processing and camera

            buffer.GetTemporaryRT(
                frameBufferId, camera.pixelWidth, camera.pixelHeight, 32,
                FilterMode.Bilinear, allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
            );
            buffer.SetRenderTarget(
                frameBufferId,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store
            );

            // 
            if (flags > CameraClearFlags.Color)
            {
                flags = CameraClearFlags.Color;
            }
        }

        buffer.ClearRenderTarget(
            flags <= CameraClearFlags.Depth,
            flags <= CameraClearFlags.Color,
            flags == CameraClearFlags.Color ?
                camera.backgroundColor.linear : Color.clear
        );

        buffer.BeginSample(SampleName);
        ExecuteBuffer();
    }

    void Submit()
    {
        buffer.EndSample(SampleName);
        ExecuteBuffer();
        context.Submit();
    }

    void ExecuteBuffer()
    {
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        var sortingSettings = new SortingSettings(camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        var drawingSettings = new DrawingSettings(
            unlitShaderTagId, sortingSettings
        )
        {
            enableDynamicBatching = useDynamicBatching,
            enableInstancing = useGPUInstancing,
            perObjectData = PerObjectData.Lightmaps
            | PerObjectData.LightProbe
            | PerObjectData.LightProbeProxyVolume
            | PerObjectData.ShadowMask
            | PerObjectData.OcclusionProbe
            | PerObjectData.OcclusionProbeProxyVolume
            | PerObjectData.ReflectionProbes
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);

        var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );

        context.DrawSkybox(camera);

        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }

    void Cleanup()
    {
        lighting.Cleanup();
        if (postFXStack.IsActive)
        {
            buffer.ReleaseTemporaryRT(frameBufferId);
        }
    }
}
