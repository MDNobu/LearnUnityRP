using System;
using UnityEngine;

[DisallowMultipleComponent]
public class QxPerObjectMaterialProperties : MonoBehaviour
{
    private static int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static int cutoffId = Shader.PropertyToID("_Cutoff");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    private Color BaseColor = Color.white;

    [SerializeField, Range(0f, 1f)] private float cutoff = 0.5f, metallic = 0f, smoothness = 0.5f;

    static MaterialPropertyBlock MaterialBlock;

    void Awake()
    {
        OnValidate();    
    }

    void OnValidate()
    {
        if (MaterialBlock == null)
        {
            MaterialBlock = new MaterialPropertyBlock();
        }
        MaterialBlock.SetColor(BaseColorId, BaseColor);
        MaterialBlock.SetFloat(cutoffId, cutoff);
        MaterialBlock.SetFloat(metallicId, metallic);
        MaterialBlock.SetFloat(smoothnessId, smoothness);
        GetComponent<Renderer>().SetPropertyBlock(MaterialBlock);
    }
    
}