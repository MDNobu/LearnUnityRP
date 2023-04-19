using System;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu( menuName = "Rendering/QxRenderPipeline")]
public class QxRenderPipelineAssets : RenderPipelineAsset
{
    public Cubemap diffuseIBL;
    public Cubemap specularIBL;
    public Texture brdfLut;
    public Texture blueNoiseTex;

    [SerializeField]
    public QxCSMSettings csmSettings;

    public QxInstanceData[] instanceDatas;
    
    protected override RenderPipeline CreatePipeline()
    {
        QxRenderPipeline renderPipeline = new QxRenderPipeline();

        renderPipeline._diffuseIBL = diffuseIBL;
        renderPipeline._specularIBL = specularIBL;
        renderPipeline.brdfLut = brdfLut;
        renderPipeline._csmSettings = csmSettings;
        renderPipeline.blurNoiseTex = blueNoiseTex;
        renderPipeline.instanceDatas = instanceDatas;
        return renderPipeline;
    }
}