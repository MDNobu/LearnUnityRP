﻿using UnityEngine;

[CreateAssetMenu(fileName = "QxRenderPipeline/InstanceData")]
public class QxInstanceData : ScriptableObject
{
    [HideInInspector]
    public Matrix4x4[] mats;

    [HideInInspector]
    // 全部实体的变换矩阵，运行时生成的GPU Buffer
    public ComputeBuffer matrixBuffer;

    [HideInInspector]
    // 剔除后剩余的instance的变换矩阵，运行时生成的GPU Buffer
    public ComputeBuffer validMatrixBuffer;

    [HideInInspector]
    // 绘制参数， 运行时生成的GPU Buffer
    public ComputeBuffer argsBuffer;

    [HideInInspector]
    // submesh 下标
    public int subMeshIndex = 0;
    [HideInInspector]
    // instance 数目，持久保存
    public int instanceCount = 0;

    public Mesh instanceMesh;
    public Material instanceMaterial;

    public Vector3 center = new Vector3(0, 0, 0);
    public int randomInstanceNum = 5000;
    public float distanceMin = 5.0f;
    public float distanceMax = 50.0f;
    public float heightMin = -0.5f;
    public float heightMax = 0.5f;
    
    // 随机生成
    public void GenerateRandomData()
    {
        instanceCount = randomInstanceNum;
        
        // 生成变换矩阵
        mats = new Matrix4x4[instanceCount];
        for (int i = 0; i < instanceCount; i++)
        {
            float angle = Random.Range(0.0f, Mathf.PI * 2.0f);
            float distance = Mathf.Sqrt(Random.Range(0.0f, 1.0f) * (distanceMax - distanceMin) + distanceMin);
            float height = Random.Range(heightMin, heightMax);

            Vector3 pos = new Vector3(Mathf.Sin(angle) * distance, height, Mathf.Cos(angle) * distance);
            Vector3 dir = pos - center;

            Quaternion q = new Quaternion();
            q.SetLookRotation(dir, new Vector3(0, 1, 0));

            Matrix4x4 m = Matrix4x4.Rotate(q);
            m.SetColumn(3, new Vector4(pos.x, pos.y, pos.z, 1.0f));

            mats[i] = m;
        }

        if (matrixBuffer != null)
        {
            matrixBuffer.Release();
            matrixBuffer = null;
        }

        if (validMatrixBuffer != null)
        {
            validMatrixBuffer.Release();
            validMatrixBuffer = null;
        }

        if (argsBuffer != null)
        {
            argsBuffer.Release();
            argsBuffer = null;
        }
        
        Debug.Log("Instance Data Generate Success");
    }
}