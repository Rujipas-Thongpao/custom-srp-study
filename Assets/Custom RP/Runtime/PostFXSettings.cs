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
}

public enum bloomMode
{
    additive, scatter
}

public enum ToneMappingMode { None = -1, ACES, Neutral, Reinhard }
