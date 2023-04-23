using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class QxSkinFeature : ScriptableRendererFeature
{
    public Material sssMat;

    [ColorUsage(false, true)]
    public Color scatteringDistance = Color.black;

    [ColorUsage(false, true)]
    public Color transmissionTint = Color.black;

    public Vector2 thicknessRemap = new Vector2(1f, 5f);
    public float worldScale = 1f;
    public float ior = 1.4f;

    class QxSkinRenderPass : ScriptableRenderPass
    {
        private int[] _diffuseRT;
        private QxSkinFeature _feature;

        private const string _profileTag = "Skin Diffuse";
        private int width, height;
        public QxSkinRenderPass(QxSkinFeature inFeature)
        {
            this._feature = inFeature;
        }

        // 这个是URP中配置render target和其clear state的回调，不要在这里调用SetRenderTarget，而要用configuretarget
        // 在这里分配temporal的render texture
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            base.Configure(cmd, cameraTextureDescriptor);

            width = cameraTextureDescriptor.width;
            height = cameraTextureDescriptor.height;

            if (_diffuseRT == null)
            {
                _diffuseRT = new int[3];
                _diffuseRT[0] = Shader.PropertyToID("_SkinDiffuse");
                _diffuseRT[1] = Shader.PropertyToID("_SkinDepth");
                _diffuseRT[2] = Shader.PropertyToID("_SkinSSS");
            }
            cmd.GetTemporaryRT(_diffuseRT[0], cameraTextureDescriptor.width, cameraTextureDescriptor.height,
                0, FilterMode.Point, RenderTextureFormat.ARGBHalf);
            cmd.GetTemporaryRT(_diffuseRT[1], cameraTextureDescriptor.width, cameraTextureDescriptor.height,
                16, FilterMode.Point, RenderTextureFormat.Depth);
            cmd.GetTemporaryRT(_diffuseRT[2], cameraTextureDescriptor.width, cameraTextureDescriptor.height,
                0, FilterMode.Point, RenderTextureFormat.ARGBHalf);
        }
        
        static readonly ShaderTagId skinDiffuseShaderTagId = new ShaderTagId("SkinDiffuse");
        private FilteringSettings _filteringSettings = new FilteringSettings(RenderQueueRange.all);
        
        

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            base.FrameCleanup(cmd);
            cmd.ReleaseTemporaryRT(_diffuseRT[0]);
            cmd.ReleaseTemporaryRT(_diffuseRT[1]);
            cmd.ReleaseTemporaryRT(_diffuseRT[2]);
        }
    }
    
    public override void Create()
    {
        throw new System.NotImplementedException();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        throw new System.NotImplementedException();
    }
}