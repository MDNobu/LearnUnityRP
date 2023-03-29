using Unity.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.Profiling;
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

    public Cubemap _diffuseIBL;
    public Cubemap _specularIBL;
    public Texture _brdfLut;

    public QxCSMSettings _csmSettings;

    private QxCSM _csm;
    
    // 阴影的部分参数管理
    public int shadowMapResolution = 1024;
    public float orthoDistance = 500.0f;

    private RenderTexture[] shadowTextures = new RenderTexture[4];
    
    
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

        
        // 创建shadow map贴图
        for (int i = 0; i < 4; i++)
        {
            shadowTextures[i] = new RenderTexture(shadowMapResolution, shadowMapResolution, 24
                , RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        }
        
        _csm = new QxCSM();
    }
    
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this._context = context;
        this._camera = camera;
        
        _context.SetupCameraProperties(camera);

        // 设置一些全局的shader 参数
        {
            Shader.SetGlobalTexture("_gdepth", gdepth);
            for (int i = 0; i < 4; i++)
            {
                Shader.SetGlobalTexture("_GT"+i, gbuffers[i]);
            }
            
            // 设置相机矩阵
            Matrix4x4 viewMatrix = _camera.worldToCameraMatrix;
            Matrix4x4 projMatrix = GL.GetGPUProjectionMatrix(_camera.projectionMatrix, false);
            Matrix4x4 vpMatrix = projMatrix * viewMatrix;
            Matrix4x4 vpMatrixInv = vpMatrix.inverse;
            Shader.SetGlobalMatrix("_vpMatrix", vpMatrix);
            Shader.SetGlobalMatrix("_vpMatrixInv", vpMatrixInv);
            
            
            // 设置IBL 贴图
            Shader.SetGlobalTexture("_diffuseIBL", _diffuseIBL);
            Shader.SetGlobalTexture("_specularIBL", _specularIBL);
            Shader.SetGlobalTexture("_brdfLut", _brdfLut);
            
            // 设置CSM 相关全局shader参数
            for (int i = 0; i < 4; i++)
            {
                Shader.SetGlobalTexture("shadowTex"+i, shadowTextures[i]);
                Shader.SetGlobalFloat("_split"+i, _csm.splits[i]);
            }
        }
        // cmdBuffer.SetGlobalColor("_TestLightColor", Color.red);

        RenderShadowDepthPass();
        
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

        
        RenderLightPass(ref cullResults);
        
        // sky box and gizmos
        context.DrawSkybox(camera);
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }

        // _lighting.Setup(_context, cullResults);
        
        context.Submit();
    }

    private void RenderShadowDepthPass()
    {
        Profiler.BeginSample("QxShadowDepthPass");
        
        // 获得光源信息
        Light light = RenderSettings.sun;
        Vector3 lightDir = light.transform.rotation * Vector3.forward;
        
        // 更新csm 分割
        _csm.Update(_camera, lightDir);
        _csmSettings.Set();
        
        _csm.CacheCameraSettings(_camera);
        // 后面的阴影用正交投影计算
        _camera.orthographic = true;


        for (int level = 0; level < 4; level++)
        {
            // 相机移到当前split的光源位置
            _csm.ConfigCameraToShadowSpace(_camera, lightDir, level, orthoDistance, shadowMapResolution);
            
            
            
            _context.DrawRenderers();
            _context.Submit();
        }

        _csm.RevertMainCameraSettings(_camera);
        _camera.orthographic = false;
        
        Profiler.EndSample();
        
    }

    private void RenderLightPass(ref CullingResults cullResults)
    {
        // 使用Blit 渲染一个全屏light pass
        CommandBuffer cmdBuffer = new CommandBuffer();
        cmdBuffer.name = "lightpass";
        
        
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
        
        
        cmdBuffer.name = bufferName;
        cmdBuffer.SetRenderTarget(gbufferIDs, gdepth);

        cmdBuffer.ClearRenderTarget(true, true, Color.red);
        _context.ExecuteCommandBuffer(cmdBuffer);
    }
}