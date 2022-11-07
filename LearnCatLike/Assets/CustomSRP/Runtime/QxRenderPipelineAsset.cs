using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu(menuName = "Rendering/QxRenderPipeline")]
public class QxRenderPipelineAsset : RenderPipelineAsset
{
    [SerializeField]
    private bool useDynamicBathcing = true, useGPUInstancing = true, useSRPBatcher = true;
    
    protected override RenderPipeline CreatePipeline()
    {
        return new QxRenderPipeline(useDynamicBathcing, useGPUInstancing, useSRPBatcher);
    }
}