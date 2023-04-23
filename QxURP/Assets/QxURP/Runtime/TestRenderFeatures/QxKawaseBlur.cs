using UnityEngine;
using Unity.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


public class QxKawaseBlur : ScriptableRendererFeature
{
    public class KawaseBlurSettings
    {
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingTransparents;
        public Material blurMaterial = null;

        [Range(2, 15)]
        public int blurPasses = 2;

        [Range(1, 4)]
        public int downsample = 1;

        public bool copyToFrameBuffer = true;
        public string targetName = "_blurTexture";
    }

    public KawaseBlurSettings settings = new KawaseBlurSettings();

    class CustomRenderPass : ScriptableRenderPass
    {
        public Material blurMatrerial;
        public int passes;
        public int downsample = 1;
        public bool copyToFrameBuffer;
        public string targetName;
        private string profilerTag;

        private int tmpId1;
        private int tmpId2;

        private RenderTargetIdentifier tmpRT1;
        private RenderTargetIdentifier tmpRT2;

        private RenderTargetIdentifier cameraColorTexture;

        public CustomRenderPass(string inProfileTag)
        {
            profilerTag = inProfileTag;
        }

        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);
            var width = cameraTextureDescriptor.width / downsample;
            var height = cameraTextureDescriptor.height / downsample;

            tmpId1 = Shader.PropertyToID("tmpBlurRT1");
            tmpId2 = Shader.PropertyToID("tmpBlurRT2");
            
            cmd.GetTemporaryRT(tmpId1, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);
            cmd.GetTemporaryRT(tmpId2, width, height, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

            tmpRT1 = new RenderTargetIdentifier(tmpId1);
            tmpRT2 = new RenderTargetIdentifier(tmpId2);
            
            ConfigureTarget(tmpRT1);
            ConfigureTarget(tmpRT2);
        }
        
        

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            cameraColorTexture = renderingData.cameraData.renderer.cameraColorTarget;
            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);
            cmd.BeginSample("testKawase");

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;
            
            // fist pass
            cmd.SetGlobalFloat("_offset", 1.5f);
            cmd.Blit(cameraColorTexture, tmpRT1, blurMatrerial);

            for (int i = 1; i < passes - 1; i++)
            {
                cmd.SetGlobalFloat("_offset", 0.5f + i);
                cmd.Blit(tmpRT1, tmpRT2, blurMatrerial);
                
                // pingpong
                (tmpRT1, tmpRT2) = (tmpRT2, tmpRT1);
            }
            
            // final pass
            cmd.SetGlobalFloat("_offset", 0.5f + passes - 1f);
            if (copyToFrameBuffer)
            {
                cmd.Blit(tmpRT1, cameraColorTexture, blurMatrerial);
            }
            else
            {
                cmd.Blit(tmpRT1, tmpRT2, blurMatrerial);
                cmd.SetGlobalTexture(targetName, tmpRT2);
            }
            

            cmd.EndSample("testKawase");
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            CommandBufferPool.Release(cmd);
        }
    }

    private CustomRenderPass _customRenderPass;
    
    public override void Create()
    {
        _customRenderPass = new CustomRenderPass("testKawasePass");
        _customRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingShadows + 1;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(_customRenderPass);
    }
}