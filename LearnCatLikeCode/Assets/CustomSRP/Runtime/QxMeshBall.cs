using System;
using UnityEngine;
using Matrix4x4 = UnityEngine.Matrix4x4;
using Random = UnityEngine.Random;


public class QxMeshBall : MonoBehaviour
{
    private static int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static int cutoffId = Shader.PropertyToID("_Cutoff");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    private Mesh mesh = default;

    [SerializeField]
    private Material material = default;

    
    private Matrix4x4[] matrices = new Matrix4x4[1023];
    private Vector4[] baseColors = new Vector4[1023];
    private float[] metallic = new float[1023];
    private float[] soomthness = new float[1023];
    private float[] cutoffs = new float[1023];

    private MaterialPropertyBlock block;

    void Awake()
    {
        for (int i = 0; i < matrices.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 10f, Quaternion.identity, Vector3.one);
            baseColors[i] =
                new Vector4(Random.value, Random.value, Random.value, 1f);
            cutoffs[i] = 0.5f;
        }    
    }

    private void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(BaseColorId, baseColors);
            block.SetFloatArray(cutoffId, cutoffs);
            block.SetFloatArray(smoothnessId, soomthness);
            block.SetFloatArray(metallicId, metallic);
        }
        
        
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, matrices.Length, block);
    }
}