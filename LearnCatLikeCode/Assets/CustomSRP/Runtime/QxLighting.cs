

using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class QxLighting
{
    private const  int maxDirLightCount = 4;
    
    private const string bufferName = "Lighting";

    // private static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    // private static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");

    private static int dirLightCountId = Shader.PropertyToID("_DirectionalLightCount");
    private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");
    

    private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];
    

    private CullingResults cullingResults;
    
    private CommandBuffer buffer = new CommandBuffer()
    {
        name = bufferName
    };

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        // SetupDirectionalLight();
        SetupLights();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
        
    }
    
    

    private void SetupLights()
    {
        NativeArray<VisibleLight> visibleLights = cullingResults.visibleLights;
        int dirLightCount = 0;
        for (int i = 0; i < visibleLights.Length; i++)
        {
            VisibleLight visibleLight = visibleLights[i];
            if (visibleLight.lightType == LightType.Directional)
            {
                SetupDirectionalLight(dirLightCount, ref visibleLight);
                ++dirLightCount;
                if (dirLightCount >= maxDirLightCount )
                {
                    break;
                }
                else
                {
                    Debug.LogError("There are more lights than pipeline cableble");
                }
            }
        }
        buffer.SetGlobalInt(dirLightCountId, visibleLights.Length);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);
    }

    void SetupDirectionalLight(int index,ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
    }

    // private void SetupDirectionalLight()
    // {
    //     // RenderSetting.sun代表场景的主光源
    //     Light light = RenderSettings.sun;
    //     buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
    //     buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);
    // }
}