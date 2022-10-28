using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class QxCustomRenderPipeline : RenderPipeline
{

    public bool useDynamicInstancing;
    public bool useDynamicBatching;
    
    public QxCustomRenderPipeline(bool useDynamicBatching, bool useDynamicInstancing, bool useSRPBatcher)
    {
        this.useDynamicBatching = useDynamicBatching;
        this.useDynamicInstancing = useDynamicInstancing;
        GraphicsSettings.useScriptableRenderPipelineBatching = useSRPBatcher;
        GraphicsSettings.lightsUseLinearIntensity = true;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    protected override void Render(ScriptableRenderContext context, Camera[] cameras)
    {
        QxCameraRenderer renderer = new QxCameraRenderer();
        foreach (var camera in cameras)
        {
            renderer.Render(context, camera, useDynamicBatching, useDynamicInstancing);
        }

    }
}
