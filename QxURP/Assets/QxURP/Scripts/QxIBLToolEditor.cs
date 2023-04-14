using System;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(QxIBLTool))]
public class QxIBLToolEditor : Editor
{
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
        int size = 128; //envCubemap.height
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
            genIrradianceMapCS.SetTexture(kid, "_Cubemap", envCubemap);
            genIrradianceMapCS.SetBuffer(kid, "_Result", resultBuffer);
            genIrradianceMapCS.SetVector("_Dispatch",  new Vector4(size/8, size/8, 1, 0));
            genIrradianceMapCS.Dispatch(kid, size/8, size/8, 1);

            resultBuffer.GetData(tmpColors);
            // resultBuffer.GetData(tmpVecs);
            outputCubeMap.SetPixels(tmpColors, (CubemapFace)i);
        } 
        outputCubeMap.Apply();

        string tmpStr = "";
        foreach (var tmpVec in tmpVecs)
        {
            tmpStr = tmpStr +  ",{" + tmpVec + "}";
        }
        Debug.Log(tmpStr);

        AssetDatabase.CreateAsset(outputCubeMap,folderPath+"/ModifiedCubemap.asset");
        AssetDatabase.SaveAssets();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (GUILayout.Button("生成IrradianceMap"))
        {
            PrefilterDiffuseCubemap(_iblTool.srcCubeMap, _iblTool.outCubeMap);
        }
    }
}