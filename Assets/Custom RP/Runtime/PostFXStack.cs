using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack
{
    const string BUFFER_NAME = "FX stack";
    const int MAX_BLOOM_PYRAMID_LEVELS = 16;
    static int fxSourceId = Shader.PropertyToID("_PostFXSource"),
               fxSourceId2 = Shader.PropertyToID("_PostFXSource2"),
               useBicubicId = Shader.PropertyToID("_useBicubic"),
               bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
               bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
               bloomIntensityId = Shader.PropertyToID("_BloomIntensity");
    CommandBuffer buffer = new CommandBuffer
    {
        name = BUFFER_NAME
    };
    ScriptableRenderContext context;
    Camera camera;
    PostFXSettings settings;

    int bloomPyramidId;
    bool useHDR;


    public bool IsActive => settings != null;


    public PostFXStack()
    {
        bloomPyramidId = Shader.PropertyToID("_BloomPyramid0");
        // times 2 because it have to be horizontal and verical
        for (int i = 0; i < MAX_BLOOM_PYRAMID_LEVELS * 2; i++)
        {
            // we can do this because Id is assign sequencially.
            // meaning if we get bloomPyramidId + 1 the we'll get "_BloomPyramid1".
            // Therefore, we only have to keep track the first one.
            Shader.PropertyToID("_BloomPyramid" + i);
        }

    }

    public void Setup(ScriptableRenderContext _context, Camera _camera, PostFXSettings _settings, bool _useHDR)
    {
        this.useHDR = _useHDR;
        this.context = _context;
        this.camera = _camera;
        this.settings = (this.camera.cameraType <= CameraType.SceneView) ? _settings : null;
        ApplySceneViewState();
    }

    public void Render(int _sourceId)
    {
        /*buffer.Blit(_sourceId, BuiltinRenderTextureType.CameraTarget);*/
        /*Draw(_sourceId, BuiltinRenderTextureType.CameraTarget, Pass.copy);*/
        DoBloom(_sourceId);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();

    }

    // Draw texture from _from to render in _to
    void Draw(RenderTargetIdentifier _from, RenderTargetIdentifier _to, Pass _pass)
    {
        buffer.SetGlobalTexture(fxSourceId, _from); // set fxsource texture to be _from
        buffer.SetRenderTarget(_to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store); // Change destination of render to _to instead of camera
        buffer.DrawProcedural( // draw with different pass.
                Matrix4x4.identity, settings.Material, (int)_pass,
                MeshTopology.Triangles, 3
                );

    }

    void DoBloom(int _sourceId)
    {
        buffer.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloom = settings.Bloom;

        // Set threshold 
        Vector4 threshold = GetThreshold(bloom);
        buffer.SetGlobalVector(bloomThresholdId, threshold);
        buffer.SetGlobalInt(useBicubicId, bloom.useBicubic ? 1 : 0);


        Pass combinePass;
        if (bloom.mode == bloomMode.additive)
        {
            combinePass = Pass.BloomAdd;
            buffer.SetGlobalFloat(bloomIntensityId, 1f);
        }
        else
        {
            combinePass = Pass.BloomScatter;
            buffer.SetGlobalFloat(bloomIntensityId, bloom.scatter);

        }



        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;


        // Prefilter :  start at half resolution
        buffer.GetTemporaryRT(
            bloomPrefilterId, width, height, 0, FilterMode.Bilinear, format
        );
        Draw(_sourceId, bloomPrefilterId, useHDR ? Pass.PrefilterFireflies : Pass.Prefilter); // draw source into prefilter
        width /= 2;
        height /= 2;

        // not start from _sourceId but start from prefilter 
        int fromId = bloomPrefilterId, toId = bloomPyramidId + 1;

        int i;
        for (i = 0; i < bloom.maxIterations; i++)
        {
            int midId = toId - 1;
            if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
            {
                break;
            }
            // Create temp rt
            buffer.GetTemporaryRT(
                midId, width, height, 0, FilterMode.Bilinear, format
            );
            buffer.GetTemporaryRT(
                toId, width, height, 0, FilterMode.Bilinear, format
            );
            // Draw
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);


            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
            if (
                bloom.maxIterations == 0 || bloom.intensity <= 0f ||
                height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2
            )
            {
                Draw(_sourceId, BuiltinRenderTextureType.CameraTarget, Pass.copy);
                buffer.EndSample("Bloom");
                return;
            }
        }
        if (i > 1)
        {
            buffer.ReleaseTemporaryRT(fromId - 1);
            toId = fromId - 3;
            for (i = bloom.maxIterations - 1; i > 0; i--)
            {
                // 5 is last blur, 3 is last blur, 2 is half blur before 3.
                buffer.SetGlobalTexture(fxSourceId2, toId + 1); // set 3 
                Draw(fromId, toId, combinePass); // combine 5,3 -> 2 
                buffer.ReleaseTemporaryRT(fromId); // release 5
                buffer.ReleaseTemporaryRT(toId + 1); // relase 3
                fromId = toId;
                toId -= 2;
            }
        }
        else
        {
            buffer.ReleaseTemporaryRT(bloomPyramidId);
        }

        buffer.SetGlobalTexture(fxSourceId2, _sourceId);
        // Render to the camera
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, combinePass);

        buffer.ReleaseTemporaryRT(fromId);
        buffer.ReleaseTemporaryRT(bloomPrefilterId);
        buffer.EndSample("Bloom");
    }

    Vector4 GetThreshold(PostFXSettings.BloomSettings _bloom)
    {
        Vector4 threshold = new Vector4();
        threshold.x = Mathf.GammaToLinearSpace(_bloom.threshold);
        threshold.y = threshold.x * _bloom.thresholdKnee;
        threshold.z = 2f * threshold.y;
        threshold.w = 0.25f / (threshold.y + 0.00001f);
        threshold.y -= threshold.x;
        return threshold;
    }
}

public enum Pass
{
    copy, BloomHorizontal, BloomVertical, BloomAdd, Prefilter, PrefilterFireflies, BloomScatter
}
