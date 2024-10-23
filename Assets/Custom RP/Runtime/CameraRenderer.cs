using UnityEngine;
using UnityEngine.Rendering;

public partial class CameraRenderer
{
    const string bufferName = "Render Camera";

    static ShaderTagId
        unlitShaderTagId = new ShaderTagId("SRPDefaultUnlit"),
        litShaderTagId = new ShaderTagId("CustomLit");

    static int
        colorBufferId = Shader.PropertyToID("_CameraColorAttachment"),
        depthBufferId = Shader.PropertyToID("_CameraDepthAttachment"),
        depthTextureId = Shader.PropertyToID("_CameraDepthTexture"),
        sourceTextureId = Shader.PropertyToID("_SourceTexture")

        ;

    CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    ScriptableRenderContext context;

    Camera camera;

    CullingResults cullingResults;
    bool allowHDR;
    bool useDepthTexture, useIntermediateBuffer;

    Lighting lighting = new Lighting();
    PostFXStack postFXStack = new PostFXStack();

    ColorLUTResolution colorLUTResolution;

    Material material;

    public CameraRenderer(Shader _shader)
    {
        this.material = CoreUtils.CreateEngineMaterial(_shader);
    }

    void Setup()
    {
        context.SetupCameraProperties(camera);
        CameraClearFlags flags = camera.clearFlags;


        useIntermediateBuffer = postFXStack.IsActive || useDepthTexture;
        if (useIntermediateBuffer)
        {
            // NOTE : we use this render texture as an immediate texture for post-processing and camera

            // color buffer
            buffer.GetTemporaryRT(
                colorBufferId, camera.pixelWidth, camera.pixelHeight, 0,
                FilterMode.Bilinear, allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default
            );
            // depth buffer
            buffer.GetTemporaryRT(
                depthBufferId, camera.pixelWidth, camera.pixelHeight, 32,
                FilterMode.Point, RenderTextureFormat.Depth
            );
            // public void SetRenderTarget( color,  colorLoadAction,  colorStoreAction,  depth,  depthLoadAction,  depthStoreAction);
            buffer.SetRenderTarget(
                colorBufferId,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store,
                depthBufferId,
                RenderBufferLoadAction.DontCare,
                RenderBufferStoreAction.Store
            );

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
        this.useDepthTexture = true;

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
            postFXStack.Render(colorBufferId);
        }
        else if (useIntermediateBuffer) // if copy depthbuffer or postfx active
        {
            Draw(colorBufferId, BuiltinRenderTextureType.CameraTarget);
            ExecuteBuffer();
        }
        DrawGizmosAfterFX();

        Cleanup();

        Submit();
    }

    // Draw texture from _from to render in _to
    void Draw(RenderTargetIdentifier _from, RenderTargetIdentifier _to)
    {
        buffer.SetGlobalTexture(sourceTextureId, _from); // set source texture to be _from
        buffer.SetRenderTarget(_to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store); // Change destination of render to _to instead of camera
        buffer.DrawProcedural( // draw first pass
            Matrix4x4.identity, this.material, 0,
            MeshTopology.Triangles, 3
        );

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

        CopyAttachments();



        sortingSettings.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortingSettings;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;

        context.DrawRenderers(
            cullingResults, ref drawingSettings, ref filteringSettings
        );
    }


    void CopyAttachments()
    {
        if (!useDepthTexture) return;
        buffer.GetTemporaryRT(depthTextureId, camera.pixelWidth, camera.pixelHeight,
                32, FilterMode.Point, RenderTextureFormat.Depth);
        buffer.CopyTexture(depthBufferId, depthTextureId);
        ExecuteBuffer();
    }

    void Cleanup()
    {
        lighting.Cleanup();
        if (useIntermediateBuffer)
        {
            buffer.ReleaseTemporaryRT(colorBufferId);
            buffer.ReleaseTemporaryRT(depthBufferId);
            if (useDepthTexture)
            {
                buffer.ReleaseTemporaryRT(depthTextureId);

            }
        }
    }

    public void Dispose()
    {
        CoreUtils.Destroy(material);
    }
}
