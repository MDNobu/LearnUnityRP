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


        // #TODO 不知道这个函数的作用????
        // https://zero-radiance.github.io/post/sampling-diffusion/
        // Performs sampling of a Normalized Burley diffusion profile in polar coordinates.
        // 'u' is the random number (the value of the CDF): [0, 1).
        // rcp(s) = 1 / ShapeParam = ScatteringDistance.
        // Returns the sampled radial distance, s.t. (u = 0 -> r = 0) and (u = 1 -> r = Inf).
        static float SampleBurleyDiffusionProfile(float u, float rcpS)
        {
            u = 1 - u; // Convert CDF to CCDF

            float g = 1 + (4 * u) * (2 * u + Mathf.Sqrt(1 + (4 * u) * u));
            float n = Mathf.Pow(g, -1.0f / 3.0f);                      // g^(-1/3)
            float p = (g * n) * n;                                   // g^(+1/3)
            float c = 1 + p + n;                                     // 1 + g^(+1/3) + g^(-1/3)
            float x = 3 * Mathf.Log(c / (4 * u));

            return x * rcpS;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // RGB 散射距离，作为参数调节
            Color scatteringDistance = _feature.scatteringDistance;
            Vector3 shapeParam = new Vector3(
                Mathf.Min(16777216, 1.0f / scatteringDistance.r),
                Mathf.Min(16777216, 1.0f / scatteringDistance.g),
                Mathf.Min(16777216, 1.0f / scatteringDistance.b)
            );
            
            // 通过0.997的cdf计算出最大散射范围
            float maxScatteringDistance = Mathf.Max(scatteringDistance.r, scatteringDistance.g, scatteringDistance.b);
            float cdf = 0.997f;
            float filterRadius = SampleBurleyDiffusionProfile(cdf, maxScatteringDistance);

            CommandBuffer cmd = CommandBufferPool.Get(_profileTag);
            
            /// 透射部分的参数
            float fresnel0 = (_feature.ior - 1.0f) / (_feature.ior + 1.0f);
            fresnel0 *= fresnel0;
            Vector4 transmissionTintAndFresnel0 = new Vector4(
                _feature.transmissionTint.r * 0.25f,
                _feature.transmissionTint.g * 0.25f,
                _feature.transmissionTint.b * 0.25f,
                fresnel0
            );
            cmd.SetGlobalVector("_TransmissionTintAndFresnel0", transmissionTintAndFresnel0);
            cmd.SetGlobalVector("_ThickRemap", new Vector4(
                _feature.thicknessRemap.x, _feature.thicknessRemap.y, 
                ));
            
            CommandBufferPool.Release(cmd);
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