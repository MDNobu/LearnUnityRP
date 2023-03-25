using UnityEngine;
using UnityEngine.Rendering;

[CreateAssetMenu( menuName = "Rendering/QxRenderPipeline")]
public class QxRenderPipelineAssets : RenderPipelineAsset
{
    protected override RenderPipeline CreatePipeline()
    {
        return new QxRenderPipeline();
    }
}