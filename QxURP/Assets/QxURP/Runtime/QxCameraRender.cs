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

    // 噪声图
    public Texture blurNoiseTex;

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
    private RenderTexture shadowMask;
    private RenderTexture shadowStrength;

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


        shadowMask = new RenderTexture(Screen.width / 4, Screen.height / 4,
            0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        shadowStrength = new RenderTexture(Screen.width, Screen.height,
            0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        // 创建shadow map贴图
        for (int i = 0; i < 4; i++)
        {
            shadowTextures[i] = new RenderTexture(shadowMapResolution, shadowMapResolution, 24
                , RenderTextureFormat.Depth, RenderTextureReadWrite.Linear);
        }
        
        _csm = new QxCSM();
    }

    private void SetupGlobalShaderParams()
    {
        Shader.SetGlobalTexture("_gdepth", gdepth);
        for (int i = 0; i < 4; i++)
        {
            Shader.SetGlobalTexture("_GT"+i, gbuffers[i]);
        }
        
        Shader.SetGlobalFloat("_far", _camera.farClipPlane);
        Shader.SetGlobalFloat("_near", _camera.nearClipPlane);
        Shader.SetGlobalFloat("_screenWidth", Screen.width);
        Shader.SetGlobalFloat("_screenHeight", Screen.height);
        Shader.SetGlobalTexture("_noiseTex", blurNoiseTex);
        Shader.SetGlobalFloat("_noiseTexResolution", blurNoiseTex.width);
            
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
        Shader.SetGlobalFloat("_shadowmapResolution", shadowMapResolution);
        Shader.SetGlobalTexture("_shadowStrength", shadowStrength);
        Shader.SetGlobalTexture("_shadoMask", shadowMask);
        Shader.SetGlobalFloat("_orthoDistance", orthoDistance);
        for (int i = 0; i < 4; i++)
        {
            Shader.SetGlobalTexture("_shadowTex"+i, shadowTextures[i]);
            Shader.SetGlobalFloat("_split"+i, _csm.splits[i]);
        }
        
    }
    
    public void Render(ScriptableRenderContext context, Camera camera)
    {
        this._context = context;
        this._camera = camera;
        
        // _context.SetupCameraProperties(camera);

        // 设置一些全局的shader 参数
        SetupGlobalShaderParams();
        
        RenderShadowDepthPass();
        
        RenderBasePass();

        RenderShadowProjectionPass();
        
        // _context.SetupCameraProperties(_camera);
        RenderLightPass();
        
        // sky box and gizmos
        context.DrawSkybox(camera);
        if (Handles.ShouldRenderGizmos())
        {
            context.DrawGizmos(camera, GizmoSubset.PreImageEffects);
            context.DrawGizmos(camera, GizmoSubset.PostImageEffects);
        }

        // _lighting.Setup(_context);
        
        context.Submit();
    }

    private void RenderShadowProjectionPass()
    {
        CommandBuffer cmdBuffer = new CommandBuffer();
        cmdBuffer.name = "qxShadowProjection";
        cmdBuffer.BeginSample("qxShadowProjection");
        
        RenderTexture tempTex1 = RenderTexture.GetTemporary(
            Screen.width/4, Screen.height/4, 
            0, RenderTextureFormat.R8, RenderTextureReadWrite.Linear);
        RenderTexture tempTex2 = RenderTexture.GetTemporary(
            Screen.width/4, Screen.height/4,
            0, RenderTextureFormat.R8,
            RenderTextureReadWrite.Linear
            );
        RenderTexture tempTex3 = RenderTexture.GetTemporary(
            Screen.width, Screen.height,
            0, RenderTextureFormat.R8,
            RenderTextureReadWrite.Linear
            );

        if (_csmSettings.usingShadowMask)
        {
            cmdBuffer.Blit(gbufferIDs[0], tempTex1, 
                new Material(Shader.Find("QxRP/preshadowmappingpass")));
            cmdBuffer.Blit(tempTex1, tempTex2, 
                new Material(Shader.Find("QxRP/blurNx1")));
            cmdBuffer.Blit(tempTex2, shadowMask, 
                new Material(Shader.Find("QxRP/blur1XN")));
        }
        
        // cmdBuffer.Blit(gbufferIDs[0], tempTex3, new Material(Shader.Find("QxRP/shadowProjectionPass")));
        // cmdBuffer.Blit(tempTex3, shadowStrength, new Material(Shader.Find("QxRP/blurNXN")));
        cmdBuffer.Blit(gbufferIDs[0], shadowStrength, new Material(Shader.Find("QxRP/shadowProjectionPass")));

        
        RenderTexture.ReleaseTemporary(tempTex1);
        RenderTexture.ReleaseTemporary(tempTex2);
        RenderTexture.ReleaseTemporary(tempTex3);
        
        cmdBuffer.EndSample("qxShadowProjection");
        _context.ExecuteCommandBuffer(cmdBuffer);
        _context.Submit();
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
        
        // _csm.CacheCameraSettings(ref _camera);
        // _camera.orthographic = true;
        Camera shadowCamera = GameObject.FindWithTag("Shadow").GetComponent<Camera>();
        // shadowCamera = _camera;
        if (shadowCamera == null)
        {
            Debug.Log("需要添加shadow camera");
            return;
        }

        for (int level = 0; level < 4; level++)
        {
            // 相机移到当前split的光源位置
            _csm.ConfigCameraToShadowSpace(ref shadowCamera, lightDir, level, orthoDistance, shadowMapResolution);
            
            // 设置阴影矩阵，视锥分割参数
            Matrix4x4 v = shadowCamera.worldToCameraMatrix;
            Matrix4x4 p = GL.GetGPUProjectionMatrix(shadowCamera.projectionMatrix, false);
            Shader.SetGlobalMatrix("_shadowVpMatrix"+level, p * v);
            Shader.SetGlobalFloat("_orthoWidth"+level, _csm.orthoWidths[level]);

            CommandBuffer cmd = new CommandBuffer();
            cmd.name = "qxShadowmap_" + level;
            cmd.BeginSample("qxShadowmap_" + level);
            
            // 绘制前的准备
            _context.SetupCameraProperties(shadowCamera);
            cmd.SetRenderTarget(shadowTextures[level]);
            cmd.ClearRenderTarget(true, true, Color.clear);
            _context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            
            // 剔除
            // ScriptableCullingParameters cullingParameters;
            bool isCullValid = shadowCamera.TryGetCullingParameters(out var cullingParameters);
            if (!isCullValid)
            {
                Debug.LogError("shadow 相机 剔除 出错");
            }
            var cullingResults = _context.Cull(ref cullingParameters);
            
            // config settings
            ShaderTagId shaderTagId = new ShaderTagId("shadowDepthOnly");
            SortingSettings sortingSettings = new SortingSettings(shadowCamera);
            DrawingSettings drawSettings = new DrawingSettings(shaderTagId, sortingSettings);
            FilteringSettings filteringSettings = FilteringSettings.defaultValue;
            
            _context.DrawRenderers(cullingResults, ref drawSettings, ref filteringSettings);
            
            cmd.EndSample("qxShadowmap_" + level);
            _context.ExecuteCommandBuffer(cmd);
            _context.Submit();
        }

        // _csm.RevertMainCameraSettings(ref _camera);
        // _camera.orthographic = false;
        
        Profiler.EndSample();
    }

    private void RenderLightPass()
    {
        // 使用Blit 渲染一个全屏light pass
        CommandBuffer cmdBuffer = new CommandBuffer();
        cmdBuffer.name = "lightpass";

        Material mat = new Material(Shader.Find("QxRP/QxLightPass"));
        cmdBuffer.Blit(gbufferIDs[0], BuiltinRenderTextureType.CameraTarget, mat);
        _context.ExecuteCommandBuffer(cmdBuffer);
        _context.Submit();
    }

    private void RenderBasePass()
    {
        Profiler.BeginSample("BasePass");
        
        CommandBuffer cmdBuffer = new CommandBuffer();
        cmdBuffer.name = "basePass";
        
        _context.SetupCameraProperties(_camera);
        
        cmdBuffer.SetRenderTarget(gbufferIDs, gdepth);
        cmdBuffer.ClearRenderTarget(true, true, Color.clear);
        _context.ExecuteCommandBuffer(cmdBuffer);
        cmdBuffer.Clear();
        
        // ScriptableCullingParameters cullingParameters;
        // 剔除
        
        
        if (!_camera.TryGetCullingParameters(out var cullingParameters))
        {
            Debug.LogError("culling 结果不对");
        }
        CullingResults cullResults = _context.Cull(ref cullingParameters);
        
        // config settings
        ShaderTagId shaderTagId = new ShaderTagId("gbuffer");
        SortingSettings sortSet = new SortingSettings(_camera);
        DrawingSettings drawSet = new DrawingSettings(shaderTagId, sortSet);
        FilteringSettings filterSet = FilteringSettings.defaultValue;
        
        _context.DrawRenderers(cullResults, ref drawSet, ref filterSet);
        _context.Submit();
        
        Profiler.EndSample();
    }
}