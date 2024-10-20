using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custome Post Fx settings")]
public class PostFXSettings : ScriptableObject
{

    [SerializeField] Shader shader;
    [System.NonSerialized] Material material;
    [SerializeField] BloomSettings bloom = default;
    [SerializeField] ToneMappingSettings toneMapping = default;
    [SerializeField]
    ColorAdjustmentsSettings colorAdjustments = new ColorAdjustmentsSettings
    {
        colorFilter = Color.white
    };

    [SerializeField] WhiteBalanceSettings whiteBalance = default;
    [SerializeField]
    SplitToneSettings splitTone = new SplitToneSettings
    {
        shadows = Color.gray,
        highlights = Color.gray
    };
    [SerializeField]
    ChannelMixerSettings channelMixer = new ChannelMixerSettings
    {
        red = Vector3.right,
        green = Vector3.up,
        blue = Vector3.forward
    };



    public Material Material
    {
        get
        {
            if (material == null & shader != null)
            {
                material = new Material(shader);
                material.hideFlags = HideFlags.HideAndDontSave;
            }
            return material;
        }
    }

    public BloomSettings Bloom => bloom;
    public ToneMappingSettings ToneMapping => toneMapping;
    public ColorAdjustmentsSettings ColorAdjustments => colorAdjustments;
    public WhiteBalanceSettings Whitebalance => whiteBalance;
    public SplitToneSettings SplitTone => splitTone;
    public ChannelMixerSettings ChannelMixer => channelMixer;

    [System.Serializable]
    public struct BloomSettings
    {
        [Range(0f, 16f)]
        public int maxIterations;

        [Min(1f)]
        public int downscaleLimit;

        public bool useBicubic;

        [Min(0f)]
        public float threshold;

        [Range(0f, 1f)]
        public float thresholdKnee;

        [Min(0f)]
        public float intensity;

        public bloomMode mode;

        [Range(0f, 1f)]
        public float scatter;


    }

    [System.Serializable]
    public struct ToneMappingSettings
    {
        public ToneMappingMode mode;
    }

    [System.Serializable]
    public struct ColorAdjustmentsSettings
    {
        public float postExposure;

        [Range(-100f, 100f)]
        public float contrast;

        [ColorUsage(false, true)]
        public Color colorFilter;

        [Range(-180f, 180f)]
        public float hueShift;

        [Range(-100f, 100f)]
        public float saturation;
    }

    [System.Serializable]
    public struct WhiteBalanceSettings
    {
        [Range(-100f, 100f)]
        public float temperature, tint;
    }

    [System.Serializable]
    public struct SplitToneSettings
    {
        [ColorUsage(false)]
        public Color shadows, highlights;

        [Range(-100f, 100f)]
        public float balance;
    }

    [System.Serializable]
    public struct ChannelMixerSettings
    {
        public Vector3 red, green, blue;
    }
}

public enum bloomMode
{
    additive, scatter
}

public enum ToneMappingMode { None, ACES, Neutral, Reinhard }
