using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QxDepthNormalFeature : ScriptableRendererFeature
{
    class DepthNormalPass : ScriptableRenderPass
    {
        private int kDepthBufferBits = 32;
        private RenderTargetHandle depthAttachmentHandle {
            get;
            set;
        }

        internal RenderTextureDescriptor descriptor { get; private set; }

        private Material depthNormalMaterial = null;
        private FilteringSettings m_FilterSettings;
        private string m_ProfilterTag = "QxDepthNormal Prepass";
        private ShaderTagId m_shaderTagId = new ShaderTagId("DepthOnly");

        public DepthNormalPass(RenderQueueRange renderQueueRange, LayerMask layerMask, Material material)
        {
            m_FilterSettings = new FilteringSettings(renderQueueRange, layerMask);
            depthNormalMaterial = material;
        }

        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle depthAttachmentHandle)
        {
            this.depthAttachmentHandle = depthAttachmentHandle;
            baseDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            baseDescriptor.depthBufferBits = kDepthBufferBits;
            descriptor = baseDescriptor;
        }
        
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);
            cmd.GetTemporaryRT(depthAttachmentHandle.id, descriptor, FilterMode.Point);
            ConfigureTarget(depthAttachmentHandle.Identifier());
            ConfigureClear(ClearFlag.All, Color.black);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get(m_ProfilterTag);
            using (new ProfilingScope(cmd, new ProfilingSampler(m_ProfilterTag)))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(m_shaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;

                ref CameraData cameraData = ref renderingData.cameraData;
                Camera camera = cameraData.camera;
                if (cameraData.isStopNaNEnabled)
                {
                    context.StartMultiEye(camera);
                }

                drawSettings.overrideMaterial = depthNormalMaterial;
                
                context.DrawRenderers(renderingData.cullResults, ref  drawSettings, ref m_FilterSettings);
                cmd.SetGlobalTexture("_CameraDepthNormalsTexture", depthAttachmentHandle.id);
            }
            
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
            if (depthAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                cmd.ReleaseTemporaryRT(depthAttachmentHandle.id);
                depthAttachmentHandle = RenderTargetHandle.CameraTarget;
            }
        }
    }

    DepthNormalPass m_DepthNormalPass;
    private RenderTargetHandle depthNormalTextureHandle;
    private Material depthNormalMaterial;

    /// <inheritdoc/>
    public override void Create()
    {
        depthNormalMaterial = CoreUtils.CreateEngineMaterial("Hidden/Internal-DepthNormalsTexture");
        m_DepthNormalPass = new DepthNormalPass(RenderQueueRange.opaque, -1, depthNormalMaterial);
        m_DepthNormalPass.renderPassEvent = RenderPassEvent.AfterRenderingPrePasses;
        depthNormalTextureHandle.Init("_CameraDepthNormalsTexture");
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_DepthNormalPass.Setup(renderingData.cameraData.cameraTargetDescriptor, depthNormalTextureHandle);
        renderer.EnqueuePass(m_DepthNormalPass);
    }
}


