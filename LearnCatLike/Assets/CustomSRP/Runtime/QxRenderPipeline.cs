using UnityEngine;
using UnityEngine.Rendering;

public class QxRenderPipeline : RenderPipeline
{
    private bool useDynamicBatching, useGPUInstancing;
    
    public QxRenderPipeline(
        bool useDynamicBatching, bool useGPUInstancing, bool useSRPBatcher)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useGPUInstancing = useGPUInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }
    
    private QxCameraRenderer renderer = new QxCameraRenderer();
    
    protected override void Render(
        ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            renderer.Render(context, camera, useDynamicBatching, useGPUInstancing);
        }
    }
}