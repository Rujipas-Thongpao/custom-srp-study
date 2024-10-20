using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using static PostFXSettings;

public partial class PostFXStack
{
    const string BUFFER_NAME = "FX stack";
    const int MAX_BLOOM_PYRAMID_LEVELS = 16;
    static int fxSourceId = Shader.PropertyToID("_PostFXSource"),
               fxSourceId2 = Shader.PropertyToID("_PostFXSource2"),
               useBicubicId = Shader.PropertyToID("_useBicubic"),
               bloomPrefilterId = Shader.PropertyToID("_BloomPrefilter"),
               bloomThresholdId = Shader.PropertyToID("_BloomThreshold"),
               bloomIntensityId = Shader.PropertyToID("_BloomIntensity"),
               bloomResultId = Shader.PropertyToID("_BloomResult"),

               colorAdjustmentsId = Shader.PropertyToID("_ColorAdjustments"),
               colorFilterId = Shader.PropertyToID("_ColorFilter"),


               whiteBalanceId = Shader.PropertyToID("_WhiteBalance"),

               // Split tone
               splitToneShadowId = Shader.PropertyToID("_SplitToneShadow"),
               splitToneHighlightId = Shader.PropertyToID("_SplitToneHighlight"),
               splitToneBalanceId = Shader.PropertyToID("_SplitToneBalance"),

               // Channel Mixer
               channelMixerRedId = Shader.PropertyToID("_ChannelMixerRed"),
               channelMixerGreenId = Shader.PropertyToID("_ChannelMixerGreen"),
               channelMixerBlueId = Shader.PropertyToID("_ChannelMixerBlue"),

               // Shadow Midtone Highlight 
               smhShadowsColorId = Shader.PropertyToID("_SMHShadows"),
               smhMidtonesColorId = Shader.PropertyToID("_SMHMidtones"),
               smhHighlightsColorId = Shader.PropertyToID("_SMHHighlights"),
               smhRangeId = Shader.PropertyToID("_SMHRange"),

               colorLUTId = Shader.PropertyToID("_ColorGradingLUT"),
               colorLUTParametersId = Shader.PropertyToID("_ColorLUTParameters"),
               colorGradingLUTParametersId = Shader.PropertyToID("_ColorGradingLUTParameters"),
               colorGradingLUTInLogId = Shader.PropertyToID("_ColorGradingLUTInLogId")
               ;





    CommandBuffer buffer = new CommandBuffer
    {
        name = BUFFER_NAME
    };
    ScriptableRenderContext context;
    Camera camera;
    PostFXSettings settings;

    int bloomPyramidId;
    bool useHDR;
    ColorLUTResolution colorLUTResolution;


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

    public void Setup(ScriptableRenderContext _context, Camera _camera, PostFXSettings _settings, bool _useHDR, ColorLUTResolution _lutRes)
    {
        this.useHDR = _useHDR;
        this.context = _context;
        this.camera = _camera;
        this.colorLUTResolution = _lutRes;
        this.settings = (this.camera.cameraType <= CameraType.SceneView) ? _settings : null;
        ApplySceneViewState();
    }

    public void Render(int _sourceId)
    {
        /*buffer.Blit(_sourceId, BuiltinRenderTextureType.CameraTarget);*/
        /*Draw(_sourceId, BuiltinRenderTextureType.CameraTarget, Pass.copy);*/
        DoBloom(_sourceId);

        if (DoBloom(_sourceId))
        {
            DoToneMapping(bloomResultId);
            buffer.ReleaseTemporaryRT(bloomResultId);
        }
        else
        {
            DoToneMapping(_sourceId);
        }
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

    bool DoBloom(int _sourceId)
    {
        PostFXSettings.BloomSettings bloom = settings.Bloom;
        int width = camera.pixelWidth / 2, height = camera.pixelHeight / 2;

        if (
            bloom.maxIterations == 0 || bloom.intensity <= 0f ||
            height < bloom.downscaleLimit * 2 || width < bloom.downscaleLimit * 2
        )
        {
            Draw(_sourceId, BuiltinRenderTextureType.CameraTarget, Pass.copy);
            return false;
        }

        buffer.BeginSample("Bloom");

        // Set threshold 
        Vector4 threshold = GetThreshold(bloom);
        buffer.SetGlobalVector(bloomThresholdId, threshold);
        buffer.SetGlobalInt(useBicubicId, bloom.useBicubic ? 1 : 0);


        Pass combinePass, finalPass;
        float intensity;
        if (bloom.mode == bloomMode.additive)
        {
            combinePass = finalPass = Pass.BloomAdd;
            intensity = 1f;
        }
        else
        {
            combinePass = Pass.BloomScatter;
            finalPass = Pass.BloomScatterFinal;
            intensity = bloom.scatter;

        }
        buffer.SetGlobalFloat(bloomIntensityId, intensity);



        RenderTextureFormat format = useHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default;


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
        //
        buffer.GetTemporaryRT(bloomResultId, camera.pixelWidth, camera.pixelHeight, 0, FilterMode.Bilinear, format);
        Draw(fromId, bloomResultId, finalPass);

        buffer.ReleaseTemporaryRT(fromId);
        buffer.ReleaseTemporaryRT(bloomPrefilterId);
        buffer.EndSample("Bloom");

        return true;
    }

    void ConfigureSMH()
    {
        ShadowsMidtonesHighlightsSettings smh = settings.SMH;

        buffer.SetGlobalColor(smhShadowsColorId, smh.shadows.linear);
        buffer.SetGlobalColor(smhMidtonesColorId, smh.midtones.linear);
        buffer.SetGlobalColor(smhHighlightsColorId, smh.highlights.linear);

        buffer.SetGlobalVector(smhRangeId, new Vector4(
            smh.shadowsStart,
            smh.shadowsEnd,
            smh.highlightsStart,
            smh.highlightsEnd
        ));
    }

    void ConfigureChannelMixer()
    {
        ChannelMixerSettings cm = settings.ChannelMixer;
        buffer.SetGlobalVector(channelMixerRedId, cm.red);
        buffer.SetGlobalVector(channelMixerGreenId, cm.green);
        buffer.SetGlobalVector(channelMixerBlueId, cm.blue);
    }

    void ConfigureSplitTone()
    {
        SplitToneSettings st = settings.SplitTone;
        Color s = st.shadows;
        Color h = st.highlights;
        float b = st.balance * .01f;

        buffer.SetGlobalColor(splitToneShadowId, s);
        buffer.SetGlobalColor(splitToneHighlightId, h);
        buffer.SetGlobalFloat(splitToneBalanceId, b);

    }
    void ConfigureWhiteBalance()
    {
        WhiteBalanceSettings w = settings.Whitebalance;
        buffer.SetGlobalVector(
                whiteBalanceId,
                ColorUtils.ColorBalanceToLMSCoeffs(w.temperature, w.tint)
        );
    }
    void ConfigureColorAdjustments()
    {
        ColorAdjustmentsSettings c = settings.ColorAdjustments;

        Vector4 colorAdjustData = new Vector4(
                Mathf.Pow(2f, c.postExposure),
                c.contrast * .01f + 1f,
                c.hueShift * 1f / 360f,
                c.saturation * .01f + 1f
                );

        buffer.SetGlobalVector(colorAdjustmentsId, colorAdjustData);
        buffer.SetGlobalColor(colorFilterId, c.colorFilter.linear);
    }
    void DoToneMapping(int _sourceId)
    {

        ConfigureColorAdjustments();
        ConfigureWhiteBalance();
        ConfigureSplitTone();
        ConfigureChannelMixer();
        ConfigureSMH();

        ToneMappingMode mode = settings.ToneMapping.mode;
        Pass pass = mode < 0 ? Pass.copy : Pass.ColorGradingNone + (int)mode;

        int LUTheight = (int)colorLUTResolution;
        int LUTwidth = LUTheight * LUTheight;
        buffer.SetGlobalVector(colorLUTParametersId, new Vector4(
                    LUTheight,
                    .5f / LUTwidth,
                    .5f / LUTheight,
                    LUTheight / (LUTheight - 1f)
        ));
        buffer.SetGlobalVector(colorGradingLUTParametersId, new Vector4(
                    1f / LUTwidth,
                    1f / LUTheight,
                    LUTheight - 1f
        ));
        bool lutInLog = useHDR && pass != Pass.ColorGradingNone;
        buffer.SetGlobalFloat(colorGradingLUTInLogId, lutInLog ? 1f : 0f);

        buffer.GetTemporaryRT(colorLUTId, LUTwidth, LUTheight, 0, FilterMode.Bilinear, RenderTextureFormat.DefaultHDR);



        Draw(_sourceId, colorLUTId, pass); // draw to LUT
        Draw(_sourceId, BuiltinRenderTextureType.CameraTarget, Pass.Final);

        buffer.ReleaseTemporaryRT(colorLUTId);

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
    copy, BloomHorizontal, BloomVertical, BloomAdd, Prefilter, PrefilterFireflies, BloomScatter, BloomScatterFinal, ColorGradingNone, ToneMappingACES, ToneMappingNeutral, ToneMappingReinhard, Final
}
