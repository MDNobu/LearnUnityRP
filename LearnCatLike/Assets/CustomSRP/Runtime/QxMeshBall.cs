﻿using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class QxMeshBall : MonoBehaviour
{
    private const int InstanceMax = 1023;
    
    private static int baseColorId = Shader.PropertyToID("_BaseColor");
    private static int metallicId = Shader.PropertyToID("_Metallic");
    private static int smoothnessId = Shader.PropertyToID("_Smoothness");

    [SerializeField]
    private Mesh mesh = default;

    [SerializeField]
    private Material material = default;

    private Matrix4x4[] matrices = new Matrix4x4[InstanceMax];
    private Vector4[] baseColors = new Vector4[InstanceMax];
    private float[] metallics = new float[InstanceMax];
    private float[] smoothnesses = new float[InstanceMax];

    private MaterialPropertyBlock block;

    void Awake()
    {
        for (int i = 0; i < matrices.Length; i++)
        {
            matrices[i] = Matrix4x4.TRS(
                Random.insideUnitSphere * 10.0f,
                Quaternion.Euler(
                    Random.value * 360.0f,
                    Random.value * 360.0f,
                    Random.value * 360.0f
                    ), 
                Vector3.one * Random.Range(0.5f, 1.5f)
            );
            baseColors[i]
                = new Vector4(Random.value, Random.value, Random.value, Random.Range(0.5f, 1f));
            metallics[i] = Random.value < 0.25f ? 1f : 0f;
            smoothnesses[i] = Random.Range(0.05f, 0.95f);
        }    
    }

    private void Update()
    {
        if (block == null)
        {
            block = new MaterialPropertyBlock();
            block.SetVectorArray(baseColorId, baseColors);
            block.SetFloatArray(metallicId, metallics);
            block.SetFloatArray(smoothnessId, smoothnesses);
        }
        Graphics.DrawMeshInstanced(mesh, 0, material, matrices, InstanceMax, block);
    }
}