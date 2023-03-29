using System;
using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu( menuName = "Rendering/QxRenderPipeline")]
public class QxRenderPipelineAssets : RenderPipelineAsset
{
    public Cubemap diffuseIBL;
    public Cubemap specularIBL;
    public Texture brdfLut;


    [SerializeField]
    public QxCSMSettings csmSettings;
    
    protected override RenderPipeline CreatePipeline()
    {
        QxRenderPipeline renderPipeline = new QxRenderPipeline();

        renderPipeline._diffuseIBL = diffuseIBL;
        renderPipeline._specularIBL = specularIBL;
        renderPipeline.brdfLut = brdfLut;
        renderPipeline._csmSettings = csmSettings;
        return renderPipeline;
    }
}