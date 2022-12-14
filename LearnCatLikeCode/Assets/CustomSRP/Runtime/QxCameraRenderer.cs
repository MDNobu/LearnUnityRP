using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public partial class QxCameraRenderer
{
    private ScriptableRenderContext RenderContext;
    private Camera M_Camera;

    private CommandBuffer M_CommandBuffer = new CommandBuffer();
    const string BufferName = "Render Camera";

    private CullingResults M_CullingResults;

    // private static ShaderTagId utilShaderTagId = new ShaderTagId("Standard");
    
    private static ShaderTagId ulitShaderTagId = new ShaderTagId("SRPDefaultUnlit");
    private static ShaderTagId litShaderTagId = new ShaderTagId("CustomLit");

    private QxLighting lighting = new QxLighting();
    
    public void Render(ScriptableRenderContext inRenderContext, Camera inCamera,
        bool useDynamicInstancing, bool useGPUInstancing)
    {
        RenderContext = inRenderContext;
        M_Camera = inCamera;

        PrepareBuffer();
        PrepareForSceneWindow();
        if (!Cull())
        {
            return;
        }
        Setup();
        lighting.Setup(RenderContext, M_CullingResults);
        DrawVisibleGeometry(useDynamicInstancing, useGPUInstancing);
        DrawLegacyShaderGeometry();
        DrawGizmos();
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
        
        M_CommandBuffer.name = BufferName;
        
        RenderContext.SetupCameraProperties(M_Camera);
        CameraClearFlags flags = M_Camera.clearFlags;
        
        M_CommandBuffer.ClearRenderTarget(flags <= CameraClearFlags.Depth , flags == CameraClearFlags.Color, 
            flags == CameraClearFlags.Color ? M_Camera.backgroundColor.linear : Color.clear);
        // M_CommandBuffer.BeginSample(BufferName);
        ExecuteBuffer();
    }

    private void ExecuteBuffer()
    {
        RenderContext.ExecuteCommandBuffer(M_CommandBuffer);
        M_CommandBuffer.Clear();
    }

    private void Submit()
    {
        // M_CommandBuffer.EndSample(BufferName);
        RenderContext.Submit();
    }

    private void DrawVisibleGeometry(bool useDynamicBatching, bool useGPUInstancing)
    {
        SortingSettings sortSetting = new SortingSettings(M_Camera)
        {
            criteria = SortingCriteria.CommonOpaque
        };
        DrawingSettings drawingSettings = new DrawingSettings(ulitShaderTagId, sortSetting)
        {
            enableInstancing = useGPUInstancing,
            enableDynamicBatching = useDynamicBatching
        };
        drawingSettings.SetShaderPassName(1, litShaderTagId);
        FilteringSettings filteringSettings = new FilteringSettings(RenderQueueRange.opaque);

        RenderContext.DrawRenderers(
            M_CullingResults, ref drawingSettings, ref filteringSettings
            );

        
        RenderContext.DrawSkybox(M_Camera);
        
        
        sortSetting.criteria = SortingCriteria.CommonTransparent;
        drawingSettings.sortingSettings = sortSetting;
        filteringSettings.renderQueueRange = RenderQueueRange.transparent;
        
        RenderContext.DrawRenderers(
            M_CullingResults, ref drawingSettings, ref filteringSettings
            );
    }
    
    
}