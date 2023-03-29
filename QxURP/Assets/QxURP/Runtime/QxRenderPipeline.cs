using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class QxRenderPipeline : RenderPipeline
{

    private QxCameraRender renderer = new QxCameraRender();
    
    public Cubemap _diffuseIBL;
    public Cubemap _specularIBL;
    public Texture brdfLut;


    public QxCSMSettings _csmSettings;
    
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        renderer._diffuseIBL = _diffuseIBL;
        renderer._specularIBL = _specularIBL;
        renderer._brdfLut = brdfLut;
        renderer._csmSettings = _csmSettings;
        
        foreach (var camera in cameras)
        {
            renderer.Render(context, camera);
        }
    }
}
