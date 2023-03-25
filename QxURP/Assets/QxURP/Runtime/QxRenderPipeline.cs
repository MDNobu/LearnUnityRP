using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class QxRenderPipeline : RenderPipeline
{

    private QxCameraRender renderer = new QxCameraRender();
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        foreach (var camera in cameras)
        {
            renderer.Render(context, camera);
        }
    }
}
