﻿using UnityEngine;
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
        colorTextureId = Shader.PropertyToID("_CameraColorTexture"),
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
    bool useDepthTexture, useColorTexture, useIntermediateBuffer;


    Lighting lighting = new Lighting();
    PostFXStack postFXStack = new PostFXStack();

    ColorLUTResolution colorLUTResolution;

    Material material;
    CameraBufferSettings cameraBufferSettings;
    Texture2D missingTexture;

    public CameraRenderer(Shader _shader)
    {
        this.material = CoreUtils.CreateEngineMaterial(_shader);
        missingTexture = new Texture2D(1, 1)
        {
            hideFlags = HideFlags.HideAndDontSave,
            name = "missing"
        };

        missingTexture.SetPixel(0, 0, Color.white * 0.5f);
        missingTexture.Apply(true, true);
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
        buffer.SetGlobalTexture(depthTextureId, missingTexture);
        buffer.SetGlobalTexture(colorTextureId, missingTexture);
        ExecuteBuffer();
    }

    public void Render(
        ScriptableRenderContext _context, Camera _camera,
        bool _useDynamicBatching, bool _useGPUInstancing,
        ShadowSettings _shadowSettings, PostFXSettings _postFxSettings,
        ColorLUTResolution _lutRes, CameraBufferSettings _cameraBufferSettings
    )
    {

        this.cameraBufferSettings = _cameraBufferSettings;
        this.context = _context;
        this.camera = _camera;
        if (camera.cameraType == CameraType.Reflection)
        {
            useDepthTexture = _cameraBufferSettings.copyDepthReflections;
        }
        else
        {
            useDepthTexture = _cameraBufferSettings.copyDepth;
        }
        useColorTexture = _cameraBufferSettings.copyColor;
        this.allowHDR = this.cameraBufferSettings.allowHDR && camera.allowHDR;
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
        postFXStack.Setup(_context, _camera, _postFxSettings, true, _lutRes);
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
        buffer.DrawProcedural( // use camera renderer material to render this
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
        if (useDepthTexture)
        {
            buffer.GetTemporaryRT(depthTextureId, camera.pixelWidth, camera.pixelHeight,
                    32, FilterMode.Point, RenderTextureFormat.Depth);
            buffer.CopyTexture(depthBufferId, depthTextureId);
        }

        if (useColorTexture)
        {
            buffer.GetTemporaryRT(colorTextureId, camera.pixelWidth, camera.pixelHeight,
                    0, FilterMode.Point, allowHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
            buffer.CopyTexture(colorBufferId, colorTextureId);
        }

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
                buffer.ReleaseTemporaryRT(colorTextureId);

            }
        }
    }

    public void Dispose()
    {
        CoreUtils.Destroy(material);
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
}
