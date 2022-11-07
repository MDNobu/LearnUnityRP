using System;
using UnityEngine;

[DisallowMultipleComponent]
public class QxPerObjectMaterialProperties : MonoBehaviour
{
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int cutoffId = Shader.PropertyToID("_Cutoff");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");
    
    [SerializeField]
    private Color baseColor = Color.white;

    [SerializeField]
    private float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;

    private static MaterialPropertyBlock block;

    void OnValidate()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
        }
        block.SetColor(baseColorId, baseColor);
        block.SetFloat(cutoffId, cutoff);
        block.SetFloat(metallicId, metallic);
        block.SetFloat(smoothnessId, smoothness);
        GetComponent<MeshRenderer>().SetPropertyBlock(block);
    }

    private void Awake()
    {
        OnValidate();
    }
}