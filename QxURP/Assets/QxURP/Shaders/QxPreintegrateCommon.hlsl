#pragma once

#define PI 3.1415926
// 这里的实现主要参考 https://zhuanlan.zhihu.com/p/90939122

// 测试在shader中求定积分
float TestIntegrateSin(float lowerBound, float upperBound)
{
    float x = lowerBound;
    float theta = 0.001;

    float sum = 0;
    // float steps = (upperBound - lowerBound) / theta;
    float steps = (upperBound - lowerBound + 0.1) / theta;


    for (int i = 0; i < steps; ++i)
    {
        sum += sin(x) * theta;

        x += theta;
        if (x >= upperBound)
        {
            break;
        }
    }
    
    return sum;
}

// 用这个函数辅助生成皮肤预积分的图，预积分的公式还没完全明白#TODO
float3 QxCreatePreIntegratedSkinBRDF(float2 uv, float offset, float radius)
{
    return float3(0, 0, 0);
}

// #define DIFFUSION_PROFILE 0 // GPU Gems3 用的Diffusion Profile
#define DIFFUSION_PROFILE 1 // UE4 SeparableSSS的Diffusion Profile
// #define DIFFUSION_PROFILE 2 // Buryley diffusion profile

#define KEEP_DIRECT_BOUNCE 0

// https://www.shadertoy.com/view/NdBGDz
// v是方差， r是x, u是0
float QxGuassian(float v, float r)
{
    return 1.0 / sqrt(2.0 * PI * v) * exp(-(r*r)/(2.0*v));
}

#if DIFFUSION_PROFILE == 0

float3 QxDiffusionProfile(float r)
{
    return float3(0.0, 0.0, 0.0)
#if KEEP_DIRECT_BOUNCE
        + QxGuassian(0.0064, r) * float3(0.233, 0.455, 0.649)
#endif
        + QxGuassian(0.0484, r) * float3(0.100, 0.336, 0.344)
        + QxGuassian(0.187, r) * float3(0.118, 0.198, 0.0)
        + QxGuassian(0.567, r) * float3(0.113, 0.007, 0.007)
        + QxGuassian(1.99, r) * float3(0.358, 0.004, 0.0)
        + QxGuassian(7.41, r) * float3(0.233, 0.0, 0.0);
}

#elif DIFFUSION_PROFILE == 1



float3 QxGetFalloffColor()
{
    const  float3 QxFalloffColor = float3(1.0, 0.3, 0.2);
    return QxFalloffColor;
}

float3 QxSeparableSSS_Gaussian(float variance, float r, float3 inQxFalloffColor)
{
    /**
     * We use a falloff to modulate the shape of the profile. Big falloffs
     * spreads the shape making it wider, while small falloffs make it
     * narrower.
     */
    float3 rr = r / (0.001 + QxGetFalloffColor());
    
    float3 Ret = exp((-(rr * rr)) / (2.0 * variance)) / (2.0 * PI * variance);
    
    return Ret;
}

float3 QxDiffusionProfile(float r)
{
    /**
    * We used the red channel of the original skin profile defined in
    * [d'Eon07] for all three channels. We noticed it can be used for green
    * and blue channels (scaled using the falloff parameter) without
    * introducing noticeable differences and allowing for total control over
    * the profile. For example, it allows to create blue SSS gradients, which
    * could be useful in case of rendering blue creatures.
    */
    // first parameter is variance in mm^2
    
    return float3(0.0, 0.0, 0.0)
#if KEEP_DIRECT_BOUNCE
        + 0.233f * QxSeparableSSS_Gaussian(0.0064f, r, QxFalloffColor)  /* We consider this one to be directly bounced light, accounted by the strength parameter (see @STRENGTH) */
#endif
        + 0.100 * QxSeparableSSS_Gaussian(0.0484, r, QxGetFalloffColor())
        + 0.118 * QxSeparableSSS_Gaussian(0.187, r, QxGetFalloffColor())
        + 0.113 * QxSeparableSSS_Gaussian(0.567, r, QxGetFalloffColor())
        + 0.358 * QxSeparableSSS_Gaussian(1.99, r, QxGetFalloffColor())
        + 0.078 * QxSeparableSSS_Gaussian(7.41, r, QxGetFalloffColor());
}


float3 PlotQxDiffusionProfile(float2 uv)
{
    float3 value = QxDiffusionProfile(uv.x * 3.5) * 0.2;
    
    return (1.0 - smoothstep(0.0, 0.02, abs(value.x - uv.y))) * float3(1, 0, 0)
        + (1.0 - smoothstep(0.0, 0.02, abs(value.y - uv.y))) * float3(0, 1, 0)
        + (1.0 - smoothstep(0.0, 0.02, abs(value.z - uv.y))) * float3(0, 0, 1);        
}
#elif DIFFUSION_PROFILE == 2

float3 QxDiffusionProfile(float r)
{
    return float3(0, 0, 0);
}
#endif


#define A 0.15
#define B 0.50
#define C 0.10
#define D 0.20
#define E 0.02
#define F 0.30
#define W 11.2

float3 QxTonemap(float3 x)
{
    return ((x * ( A * x + C * B) + D * E) / (x * (A * x + B) + D * F)) - E/F;
}

float3 QxGenerateSkinLUT(float2 uv)
{
    // ue4 的左上角是uv的0,0
    // uv.y = 1 - uv.y;
    float NoL = uv.x;
    float inv_r = uv.y;
    float theta = acos(NoL * 2.0 - 1.0);
    float r = 1.0 / inv_r;

    float3 scatteringFactor = float3(0, 0, 0);
    float3 normalizationFactor = float3(0, 0, 0);
    for (float x = -PI/2; x < PI/2; x+= PI * 0.001)
    {
        float dist = 2.0 * r * sin(x * 0.5);
        scatteringFactor += saturate(cos(x + theta)) * QxDiffusionProfile(dist);

        normalizationFactor += QxDiffusionProfile(dist);
    }
    float3 result = scatteringFactor / normalizationFactor;


    // #TODO 这里需要进行QxTonemapping吗???
    float3 tonedResult = QxTonemap(result * 12.0);
    float3 whiteScale = 1.0 / QxTonemap(float3(W, W, W));

    tonedResult = tonedResult / whiteScale;

    return tonedResult;
}