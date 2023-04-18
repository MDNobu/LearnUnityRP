using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Random = UnityEngine.Random;

[CustomEditor(typeof(QxIBLTool))]
public class QxIBLToolEditor : Editor
{
    const  float halfPi = MathF.PI / 2.0f;
    private QxIBLTool _iblTool;
    private void OnEnable()
    {
        _iblTool = target as QxIBLTool;
    }

    private void PrefilterDiffuseCubemap(Cubemap envCubemap,Cubemap outputCubeMap)
    {
        Debug.LogWarning("Input Cubemap" + envCubemap.name);
        
        // Get the asset path
        string assetPath = AssetDatabase.GetAssetPath(envCubemap);

        // Get the folder path by removing the asset file name from the full path
        string folderPath = Path.GetDirectoryName(assetPath);
        Debug.LogWarning("Output Cubemap:" + folderPath);

        // cubemap 一个面的宽
        // int size = 128; //envCubemap.height
        int size = 128;
        // Debug.LogError("cube map widt:" + envCubemap.width );
        outputCubeMap = new Cubemap(size, TextureFormat.RGBAFloat, false);
        // outputCubeMap.filterMode = FilterMode.Trilinear;
        
       

        Color[] tmpColors = new Color[size * size];
        Vector4[] tmpVecs = new Vector4[size * size];
        // Array.Fill(tmpColors, Color.green);
        
        ComputeShader genIrradianceMapCS = Resources.Load<ComputeShader>("Shaders/QxGenerateIrradianceMap");
        
        ComputeBuffer resultBuffer = new ComputeBuffer(size * size, sizeof(float) * 4);
        
        for (int i = 0; i < 6; i++)
        {
            int kid = genIrradianceMapCS.FindKernel("GenerateIrradianceMap");
            genIrradianceMapCS.SetInt("_Face", i);
            genIrradianceMapCS.SetInt("_Resolution", size);
            // genIrradianceMapCS.SetTexture(kid, "_Cubemap", envCubemap);
            genIrradianceMapCS.SetTexture(kid, "_PointCubemap", envCubemap);
            genIrradianceMapCS.SetBuffer(kid, "_Result", resultBuffer);
            genIrradianceMapCS.SetVector("_Dispatch",  new Vector4(size/8, size/8, 1, 0));
            genIrradianceMapCS.Dispatch(kid, size/8, size/8, 1);

            resultBuffer.GetData(tmpColors);
            // resultBuffer.GetData(tmpVecs);
            outputCubeMap.SetPixels(tmpColors, (CubemapFace)i);
        } 
        outputCubeMap.Apply();

        // string tmpStr = "";
        // foreach (var tmpVec in tmpVecs)
        // {
        //     tmpStr = tmpStr +  ",{" + tmpVec + "}";
        // }
        // Debug.Log(tmpStr);

        AssetDatabase.CreateAsset(outputCubeMap,folderPath+"/ModifiedCubemap.asset");
        AssetDatabase.SaveAssets();
    }


    #region 测试蒙特卡洛方法/重要性采样求积分

    // 求0到pi/2 的cos的积分
    static float IntegrateCosThetaUniform(int N)
    {
        float sum = 0;
        
        float PDF = 1.0f / halfPi;
        for (int i = 0; i < N; i++)
        {
            float x = Random.value * halfPi;
            sum += MathF.Cos(x) / PDF;
        }

        return sum / (float)N;
    }
    
    // 基于重要性采样的Monte Carlo积分, 参考https://zhuanlan.zhihu.com/p/441901883
    // 
    static float IntegrateCosThetaImportance(int N)
    {
        Func<float, float> PDF = (float x) =>
        {
            return 4.0f / Mathf.PI - 8.0f / (Mathf.PI * Mathf.PI) * x;
        };
        Func<float, float> CDF = (float x) =>
        {
            return Mathf.PI / 4.0f * x - (x * x) / 4.0f;
        };
        // CDF函数的逆，用来得到样本分布
        Func<float, float> ICDF = (float y) =>
        {
            return halfPi * (1.0f - Mathf.Sqrt(1.0f - y));
        };
        float sum = 0.0f;
        for (int i = 0; i < N; i++)
        {
            float x = ICDF(Random.value);
            sum += Mathf.Cos(x) / PDF(x);
        }

        return sum / (float)N;
    }

    void TestMonteCarlo(Func<int, float> integrateFunc)
    {
        string tmp = "";
        for (int i = 20; i < 2048; i+= 20)
        {
            float result = integrateFunc(i);
            tmp += result + "\n";
        } 
        Debug.LogWarning(tmp);
    }
    #endregion
    
    
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("生成IrradianceMap"))
        {
            PrefilterDiffuseCubemap(_iblTool.srcCubeMap, _iblTool.outCubeMap);
        }

        if (GUILayout.Button("Uniform Monte Carlo"))
        {
            TestMonteCarlo(IntegrateCosThetaUniform);
            TestMonteCarlo(IntegrateCosThetaImportance);
        }
    }
}