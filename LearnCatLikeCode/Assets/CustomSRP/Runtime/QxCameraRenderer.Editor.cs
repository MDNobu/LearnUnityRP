using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;

public partial class QxCameraRenderer
{
    partial void DrawLegacyShaderGeometry();

    partial void DrawGizmos();

    partial void PrepareForSceneWindow();

    partial void PrepareBuffer();

    
#if UNITY_EDITOR
    private static ShaderTagId[] LegacyShaderTagIds =
    {
        new ShaderTagId("Always"),
        new ShaderTagId("FowardBase"),
        new ShaderTagId("PrepassBase"),
        new ShaderTagId("Vertex"),
        new ShaderTagId("VertexLMRGBM"),
        new ShaderTagId("VertexLM")
    };

    private static Material ErrorMaterial;

    partial void PrepareBuffer()
    {
        Profiler.BeginSample("Editory Only");
        M_CommandBuffer.name =  M_Camera.name;
        Profiler.EndSample();
    }
    
    

    partial void PrepareForSceneWindow()
    {
        if (M_Camera.cameraType == CameraType.SceneView)
        {
            ScriptableRenderContext.EmitWorldGeometryForSceneView(M_Camera);
        }
    }
    
    partial void DrawGizmos()
    {
        if (Handles.ShouldRenderGizmos())
        {
            RenderContext.DrawGizmos(M_Camera, GizmoSubset.PreImageEffects);
            RenderContext.DrawGizmos(M_Camera, GizmoSubset.PostImageEffects);
        }
    }

    partial void DrawLegacyShaderGeometry()
    {
        if (ErrorMaterial == null)
        {
            ErrorMaterial = new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        DrawingSettings drawingSettings = new DrawingSettings(
            LegacyShaderTagIds[0],
            new SortingSettings(M_Camera))
        {
            overrideMaterial = ErrorMaterial
        };
        for (int i = 0; i < LegacyShaderTagIds.Length; i++)
        {
            drawingSettings.SetShaderPassName(i, LegacyShaderTagIds[i]);
        }
        FilteringSettings filteringSettings = FilteringSettings.defaultValue;
        RenderContext.DrawRenderers(
            M_CullingResults, ref drawingSettings, ref filteringSettings
        );
    }
#else
    string SampleName => BufferName;    
#endif
    
    

    
}