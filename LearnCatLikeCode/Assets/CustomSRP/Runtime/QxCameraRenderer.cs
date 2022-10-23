using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class QxCameraRenderer
{
    private ScriptableRenderContext RenderContext;
    private Camera M_Camera;

    private CommandBuffer M_CommandBuffer = new CommandBuffer();
    const string bufferName = "Render Camera";

    private CullingResults M_CullingResults;

    private static ShaderTagId utilShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    
    public void Render(ScriptableRenderContext inRenderContext, Camera inCamera)
    {
        RenderContext = inRenderContext;
        M_Camera = inCamera;

        if (!Cull())
        {
            return;
        }
        Setup();
        DrawVisibleGeometry();
        Submit();
    }

    bool Cull()
    {
        ScriptableCullingParameters cullingParameters;
        if (M_Camera.TryGetCullingParameters(out cullingParameters))
        {
            M_CullingResults = RenderContext.Cull(ref cullingParameters);
            return true;
        }

        return false;
    }
    
    private void Setup()
    {
        
        M_CommandBuffer.name = bufferName;
        
        RenderContext.SetupCameraProperties(M_Camera);
        
        M_CommandBuffer.ClearRenderTarget(true, true, Color.clear);
        // M_CommandBuffer.BeginSample(bufferName);
        ExecuteBuffer();
        
        
    }

    private void ExecuteBuffer()
    {
        RenderContext.ExecuteCommandBuffer(M_CommandBuffer);
        M_CommandBuffer.Clear();
    }

    private void Submit()
    {
        // M_CommandBuffer.EndSample(bufferName);
        RenderContext.Submit();
    }

    private void DrawVisibleGeometry()
    {
        var sortSetting = new SortingSettings(M_Camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        DrawingSettings drawingSettings = new DrawingSettings(utilShaderTagId, sortSetting);
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.all);

        RenderContext.DrawRenderers(
            M_CullingResults, ref drawingSettings, ref filteringSettings
            );
        
        RenderContext.DrawSkybox(M_Camera);
        
    }
    
    
}