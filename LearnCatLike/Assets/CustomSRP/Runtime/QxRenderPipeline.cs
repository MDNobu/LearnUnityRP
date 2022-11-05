using UnityEngine;
using UnityEngine.Rendering;

public class QxRenderPipeline : RenderPipeline
{
    public QxRenderPipeline()
    {
        // GraphicsSettings.useScriptableRenderPipelineBatching = true;
    }
    
    private QxCameraRenderer renderer = new QxCameraRenderer();
    
    protected override void Render(
        ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            renderer.Render(context, camera);
        }
    }
}