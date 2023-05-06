using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QxSSRFeature : ScriptableRendererFeature
{
    class SSR_RenderPass : ScriptableRenderPass
    {
        // private RenderTargetIdentifier source { get; set; }
        // private RenderTargetIdentifier destination { get; set; }
        //
        // public Material ssrMaterial = null;
        //
        // private RenderTargetHandle MainTexID;
        // private RenderTargetHandle BlurID;
        // private RenderTargetHandle ReflectID;
        // private RenderTargetHandle MaskID;
        // private RenderTargetHandle SourceID;
        //
        // private FilteringSettings filter;
        // private FilteringSettings filterDepth;

        private Material ssrMaterial;
        private QxSSR ssr;
        private RenderTextureDescriptor _descriptor;
        private RenderTargetHandle ssrHandle;
        private RenderTargetIdentifier source;

        private const string ssrTag = "QxSSR Pass";
        private ShaderTagId shaderTag = new ShaderTagId("UniversalForward");

        public void Setup(RenderTargetIdentifier source)
        {
            this.source = source;
        }

        public SSR_RenderPass(Material ssrMaterial, RenderTargetHandle ssrHandle)
        {
            this.ssrMaterial = ssrMaterial;
            this.ssrHandle = ssrHandle;

            var stack = VolumeManager.instance.stack;

            ssr = stack.GetComponent<QxSSR>();
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);

            _descriptor = cameraTextureDescriptor;
            cmd.GetTemporaryRT(ssrHandle.id, _descriptor, FilterMode.Bilinear);
            ConfigureTarget(ssrHandle.Identifier());
            ConfigureClear(ClearFlag.All, Color.black);
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (ssr != null && ssr.isActive.value)
            {
                CommandBuffer cmd = CommandBufferPool.Get(ssrTag);

                using (new ProfilingScope(cmd, new ProfilingSampler(ssrTag)))
                {
                    // 这个blit 不放到后面行吗? #TODO
                    cmd.Blit(source, ssrHandle.Identifier(), ssrMaterial);
                    cmd.SetGlobalTexture("_SSRTexture", ssrHandle.Identifier());
                    ssrMaterial.SetFloat("_MaxStep", ssr.MaxStep.value);
                    ssrMaterial.SetFloat("_StepSize", ssr.StepSize.value);
                    ssrMaterial.SetFloat("_MaxDistance", ssr.MaxDistance.value);
                    ssrMaterial.SetFloat("_Thickness", ssr.Thickness.value);
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
        }
    }

    public Material ssrMaterial;

    SSR_RenderPass m_SSRPass;
    private RenderTargetHandle m_SSRHandle;

    /// <inheritdoc/>
    public override void Create()
    {
        m_SSRPass = new SSR_RenderPass(ssrMaterial, m_SSRHandle);

        m_SSRHandle.Init("_SSRTexture");
        // Configures where the render pass should be injected.
        m_SSRPass.renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
    }

    // Here you can inject one or multiple render passes in the renderer.
    // This method is called when setting up the renderer once per-camera.
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        m_SSRPass.Setup(renderer.cameraColorTarget);
        renderer.EnqueuePass(m_SSRPass);
    }
}


