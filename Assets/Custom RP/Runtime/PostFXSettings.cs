using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/Custome Post Fx settings")]
public class PostFXSettings : ScriptableObject
{

    [SerializeField] Shader shader;
    [System.NonSerialized] Material material;

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




}
