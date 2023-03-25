using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

public class QxCameraRender
{
    private ScriptableRenderContext _context;
    private Camera _camera;

    private const string bufferName = "gbuffer";

    // gbuffer render textures
    private RenderTexture gdepth;
    private RenderTexture[] gbuffers = new RenderTexture[4];
    private RenderTargetIdentifier[] gbufferIDs = new RenderTargetIdentifier[4];

    private QxLighting _lighting = new QxLighting();
    
    public QxCameraRender()
    {
        // 创建GBuffer用的纹理
        gdepth = new RenderTexture(Screen.width, Screen.height, 24,  
            RenderTextureFormat.Depth,
            RenderTextureReadWrite.Linear);
        gbuffers[0] = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
        gbuffers[1] = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGB2101010, RenderTextureReadWrite.Linear);
        gbuffers[2] = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGB64, RenderTextureReadWrite.Linear);
        gbuffers[3] = new RenderTexture(Screen.width, Screen.height, 0,
            RenderTextureFormat.ARGBFloat, RenderTextureReadWrite.Linear);

        for (int i = 0; i < gbuffers.Length; i++)
        {
            gbufferIDs[i] = gbuffers[i];
        }
    }
    
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this._context = context;
        this._camera = camera;
        
        _context.SetupCameraProperties(camera);
        
        

        RenderBasePass();
       

        ScriptableCullingParameters cullingParameters;
        // 剔除
        camera.TryGetCullingParameters(out cullingParameters);
        CullingResults cullResults = context.Cull(ref cullingParameters);
        
        // config settings
        ShaderTagId shaderTagId = new ShaderTagId("gbuffer");
        SortingSettings sortSet = new SortingSettings(camera);
        DrawingSettings drawSet = new DrawingSettings(shaderTagId, sortSet);
        FilteringSettings filterSet = FilteringSettings.defaultValue;
        
        context.DrawRenderers(cullResults, ref drawSet, ref filterSet);

        
        
        // sky box and gizmos
        context.DrawSkybox(camera);
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }

        // _lighting.Setup(_context, cullResults);
        
        RenderLightPass(ref cullResults);
        context.Submit();
    }

    private void RenderLightPass(ref CullingResults cullResults)
    {
        // 使用Blit 渲染一个全屏light pass
        CommandBuffer cmdBuffer = new CommandBuffer();
        cmdBuffer.name = "lightpass";
        
        // 设置相机矩阵
        Matrix4x4 viewMatrix = _camera.worldToCameraMatrix;
        Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false);
        Matrix4x4 vpMatrix = projMatrix * viewMatrix;
        Matrix4x4 vpMatrixInv = vpMatrix.inverse;
        cmdBuffer.SetGlobalMatrix("_vpMatrix", vpMatrix);
        cmdBuffer.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);
        // cmdBuffer.SetGlobalColor("_TestLightColor", Color.red);
        
        // 设置光源参数
        NativeArray<VisibleLight> visibleLights = cullResults.visibleLights;
        Vector4 mainLightColor = visibleLights[0].finalColor;
        Vector4 mainLightDir = visibleLights[0].localToWorldMatrix.GetColumn(2);
        


        Material mat = new Material(Shader.Find("QxRP/QxLightPass"));
        cmdBuffer.Blit(gbufferIDs[0], BuiltinRenderTextureType.CameraTarget, mat);
        _context.ExecuteCommandBuffer(cmdBuffer);
    }

    private void RenderBasePass()
    {
        CommandBuffer cmdBuffer = new CommandBuffer();
        cmdBuffer.SetGlobalTexture("_gdepth", gdepth);
        for (int i = 0; i < 4; i++)
        {
            cmdBuffer.SetGlobalTexture("_GT"+i, gbuffers[i]);
        }
        
        cmdBuffer.name = bufferName;
        cmdBuffer.SetRenderTarget(gbufferIDs, gdepth);

        cmdBuffer.ClearRenderTarget(true, true, Color.red);
        _context.ExecuteCommandBuffer(cmdBuffer);
    }
}