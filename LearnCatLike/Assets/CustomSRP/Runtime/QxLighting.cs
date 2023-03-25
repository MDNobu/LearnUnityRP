
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering;

public class QxLighting
{
    private const int maxDirLightCount = 4;
    
    // private static int dirLightColorId = Shader.PropertyToID("_DirectionalLightColor");
    // private static int dirLightDirectionId = Shader.PropertyToID("_DirectionalLightDirection");
    
    private static int direLightCount = Shader.PropertyToID("_DirectionalLightCount");
    private static int dirLightColorsId = Shader.PropertyToID("_DirectionalLightColors");
    private static int dirLightDirectionsId = Shader.PropertyToID("_DirectionalLightDirections");

    private const string bufferName = "Lighting";
    private CommandBuffer buffer = new CommandBuffer
    {
        name = bufferName
    };

    private CullingResults cullingResults;

    private static Vector4[] dirLightColors = new Vector4[maxDirLightCount];
    private static Vector4[] dirLightDirections = new Vector4[maxDirLightCount];

    public void Setup(ScriptableRenderContext context, CullingResults cullingResults)
    {
        this.cullingResults = cullingResults;
        buffer.BeginSample(bufferName);
        SetupLights();
        buffer.EndSample(bufferName);
        context.ExecuteCommandBuffer(buffer);
        buffer.Clear();
    }

    void SetupDirectionalLight(int index,ref VisibleLight visibleLight)
    {
        dirLightColors[index] = visibleLight.finalColor;
        dirLightDirections[index] = -visibleLight.localToWorldMatrix.GetColumn(2);
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
                SetupDirectionalLight(i, ref visibleLight);
                direLightCount++;
                if (dirLightCount >= maxDirLightCount)
                {
                    break;
                }
            }
        }
        buffer.SetGlobalInt(direLightCount, visibleLights.Length);
        buffer.SetGlobalVectorArray(dirLightColorsId, dirLightColors);
        buffer.SetGlobalVectorArray(dirLightDirectionsId, dirLightDirections);

        // Light light = RenderSettings.sun;
        // buffer.SetGlobalVector(dirLightColorId, light.color.linear * light.intensity);
        // buffer.SetGlobalVector(dirLightDirectionId, -light.transform.forward);
        

    }
}