using System;
using UnityEngine;

[DisallowMultipleComponent]
public class QxPerObjectMaterialProperties : MonoBehaviour
{
    private static int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static int cutoffId = Shader.PropertyToID("_Cutoff");

    [SerializeField]
    private Color BaseColor = Color.white;

    [SerializeField, Range(0f, 1f)]
    private float cutoff = 0.5f;

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
        GetComponent<Renderer>().SetPropertyBlock(MaterialBlock);
    }
    
}