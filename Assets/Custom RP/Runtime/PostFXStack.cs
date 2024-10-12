using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack
{
    const string BUFFER_NAME = "FX stack";
    const int MAX_BLOOM_PYRAMID_LEVELS = 16;
    static int fxSourceId = Shader.PropertyToID("_PostFXSource");
    CommandBuffer buffer = new CommandBuffer
    {
        name = BUFFER_NAME
    };
    ScriptableRenderContext context;
    Camera camera;
    PostFXSettings settings;

    int bloomPyramidId;


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

    public void Setup(ScriptableRenderContext _context, Camera _camera, PostFXSettings _settings)
    {
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

    void Draw(RenderTargetIdentifier _from, RenderTargetIdentifier _to, Pass _pass)
    {
        buffer.SetGlobalTexture(fxSourceId, _from);
        buffer.SetRenderTarget(_to, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);
        buffer.DrawProcedural(
                Matrix4x4.identity, settings.Material, (int)_pass,
                MeshTopology.Triangles, 3
                );

    }

    void DoBloom(int _sourceId)
    {
        buffer.BeginSample("Bloom");
        PostFXSettings.BloomSettings bloom = settings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;
        int fromId = _sourceId, toId = bloomPyramidId + 1;
        RenderTextureFormat format = RenderTextureFormat.Default;
        int i;
        for (i = 0; i < bloom.maxIterations; i++)
        {
            int midId = toId - 1;
            if (height < bloom.downscaleLimit || width < bloom.downscaleLimit)
            {
                break;
            }
            buffer.GetTemporaryRT(
                midId, width, height, 0, FilterMode.Bilinear, format
            );
            buffer.GetTemporaryRT(
                toId, width, height, 0, FilterMode.Bilinear, format
            );
            Draw(fromId, midId, Pass.BloomHorizontal);
            Draw(midId, toId, Pass.BloomVertical);

            fromId = toId;
            toId += 2;
            width /= 2;
            height /= 2;
        }
        Draw(fromId, BuiltinRenderTextureType.CameraTarget, Pass.copy); // render to camera

        for (i -= 1; i >= 0; i--)
        {
            buffer.ReleaseTemporaryRT(fromId);
            buffer.ReleaseTemporaryRT(fromId - 1);
            fromId -= 2;
        }

        buffer.EndSample("Bloom");
    }
}

public enum Pass
{
    copy, BloomHorizontal, BloomVertical
}
