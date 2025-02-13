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
    public Texture blurNoiseTex;

    public QxCSMSettings _csmSettings;

    public QxInstanceData[] instanceDatas;
    
    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        renderer._diffuseIBL = _diffuseIBL;
        renderer._specularIBL = _specularIBL;
        renderer._brdfLut = brdfLut;
        renderer._csmSettings = _csmSettings;
        renderer.blurNoiseTex = blurNoiseTex;
        renderer.instanceDatas = instanceDatas;
        
        foreach (var camera in cameras)
        {
            if (!camera.CompareTag("Shadow"))
            {
                renderer.Render(context, camera);
            }
        }
    }
}
