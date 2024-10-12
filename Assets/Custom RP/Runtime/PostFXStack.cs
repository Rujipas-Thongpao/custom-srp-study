using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class PostFXStack
{
    const string BUFFER_NAME = "FX stack";
    static int fxSourceId = Shader.PropertyToID("_PostFXSource");
    CommandBuffer buffer = new CommandBuffer
    {
        name = BUFFER_NAME
    };
    ScriptableRenderContext context;
    Camera camera;
    PostFXSettings settings;


    public bool IsActive => settings != null;

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
        Draw(_sourceId, BuiltinRenderTextureType.CameraTarget, Pass.copy);
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
}

public enum Pass
{
    copy
}
