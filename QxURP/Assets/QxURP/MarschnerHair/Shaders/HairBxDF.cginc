struct SurfaceOutputHair
{
    half3 Albedo;
    half3 Normal; // Tangent actually, 注意：这里的Normal实际上表示是头发方向的tangent
    half3 VNormal; // vertex normal;
    half Eccentric; // 
    half Alpha;
    half Roughness;
    half3 Emission;
    half Specular;
};

#define PI 3.1415926

float Square(float x)
{
    return x * x;
}

// UE4 对头发渲染的diffuse 部分的简化实现, 参考UE4中同名函数实现
// Kajiya Diffuse的近似
float3 HairDiffuseKajiyaUE(SurfaceOutputHair s, float3 L, float3 V, half3 N, half Shadow)
{
    float3 S = 0;
    // Use soft Kajiya diffuse attenuation
    float KajiyaDiffuse = 1 - abs(dot(N, L));
    
    float3 FakeNormal = normalize(V - N * dot(V, N));
    N = FakeNormal;

    // 从这里来看通过wrap lighting 能够模拟多散射??
    // Hack approximation for multiple scattering
    float Wrap = 1;
    float NoL = saturate((dot(N, L) + Wrap) / Square(1 + Wrap));
    float DiffuseScatter = (1 / PI) * lerp(NoL, KajiyaDiffuse, 0.33); // * s.Metallic
    float Luma = Luminance(s.Albedo);
    float3 ScatterTint = pow(s.Albedo / Luma, 1 - Shadow);
    S = sqrt(s.Albedo) * DiffuseScatter * ScatterTint;
    return S;
}

float HairIOF(float Eccentric) {
    float n = 1.55;
    float a = 1 - Eccentric;
    float ior1 = 2 * (n - 1) * (a * a) - n + 2;
    float ior2 = 2 * (n - 1) / (a * a) - n + 2;
    return 0.5f * ((ior1 + ior2) + 0.5f * (ior1 - ior2)); //assume cos2PhiH = 0.5f 
}

#define SQRT2PI 2.50663

//Gaussian Distribution for M term
inline float Hair_G(float B, float Theta)
{
    return exp(-0.5 * Square(Theta) / (B*B)) / (SQRT2PI * B);
}

inline float3 SpecularFresnel(float3 F0, float vDotH) {
    return F0 + (1.0f - F0) * pow(1 - vDotH, 5);
}

float3 HairSpecularMarschner(SurfaceOutputHair s, float3 L, float3 V, half3 N, float Shadow, float Backlit, float Area)
{
    float3 S = 0;

    const float VoL = dot(V, L);
    const float SinThetaL = dot(N, L);
    const float SinThetaV = dot(N, V);
    float cosThetaL = sqrt(max(0, 1 - SinThetaL * SinThetaL));
    float cosThetaV = sqrt(max(0, 1 - SinThetaV * SinThetaV));
    // thetaD是(ThetaV - ThetaL)/2
    float cosThetaD = sqrt((1 + cosThetaL * cosThetaV + SinThetaL * SinThetaV) / 2.0);

    const float3 Lp = L - SinThetaL * N;
    const float3 Vp = V - SinThetaV * N;
    const float CosPhi = dot(Lp, Vp) * rsqrt(dot(Lp, Lp) * dot(Vp, Vp) + 1e-4);
    const float CosHalfPhi = sqrt(saturate(0.5 + 0.5 * CosPhi));

    float n_prime = 1.19 / cosThetaD + 0.36 * cosThetaD;

    float Shift = 0.0499f;
    float Alpha[] =
    {
        -0.0998,//-Shift * 2,
        0.0499f,// Shift,
        0.1996  // Shift * 4
    };
    float B[] =
    {
        Area + Square(s.Roughness),
        Area + Square(s.Roughness) / 2,
        Area + Square(s.Roughness) * 2
    };

    // #TODO 为什么不是IOR index of refraction
    float hairIOF = HairIOF(s.Eccentric);
    float F0 = Square((1 - hairIOF) / ( 1 + hairIOF));

    float3 Tp;
    float Mp, Np, Fp, a, h, f;
    float ThetaH = SinThetaL + SinThetaV;

    // R
    Mp = Hair_G(B[0], ThetaH - Alpha[0]);
    Np = 0.25 * CosHalfPhi;
    // #TODO 为啥用sqrt(saturate(0.5 + 0.5 * VoL)) 这部分作为Frenel函数的VoH
    Fp = SpecularFresnel(F0, sqrt(saturate(0.5 + 0.5 * VoL)));
    // Backlit lerp这部分的意图是????
    S += (Mp * Np) * (Fp * lerp(1, Backlit, saturate(-VoL)));

    // TT
    Mp = Hair_G(B[1], ThetaH - Alpha[1]);
    a = (1.55f / hairIOF) * rcp(n_prime);
    h = CosHalfPhi * (1 + a * (0.6 - 0.8 * CosPhi));
    f = SpecularFresnel(F0, cosThetaD * sqrt(saturate(1 - h * h)));
    Fp = Square(1 - f);
    Tp = pow(s.Albedo, 0.5 * sqrt(1 - Square((h * a))) / cosThetaD);
    Np = exp(-3.65 * CosPhi - 3.98);
    // #TODO Backlit 这部分的意图是???
    S += (Mp * Np) * (Fp * Tp) * Backlit;

    // TRT
    // TRT
    Mp = Hair_G(B[2], ThetaH - Alpha[2]);
    f = SpecularFresnel(F0, cosThetaD * 0.5f);
    Fp = Square(1 - f) * f;
    Tp = pow(s.Albedo, 0.8 / cosThetaD);
    Np = exp(17 * CosPhi - 16.78);

    S += (Mp * Np) * (Fp * Tp);
    return S;
}


float3 HairShading(SurfaceOutputHair s, float3 L, float3 V, half3 N, float Shadow, float Backlit, float Area)
{
    float3 S = float3(0, 0, 0);

    S += HairSpecularMarschner(s, L, V, N, Shadow, Backlit, Area);
    S += HairDiffuseKajiyaUE(s, L, V, N, Shadow);

    // S = HairSpecularMarschner(s, L, V, N, Shadow, Backlit, Area);
    
    // #TODO 为什么这么做， 直接max(0, s)不行吗?
    S = -min(-S, 0.0); 
    return S;
}

float3 HairBxDF(SurfaceOutputHair s, half3 N, half3 V, half3 L, float Shadow, float Backlit, float Area)
{
    return HairShading(s, L, V, N, Shadow, Backlit, Area);
}

fixed4 LightingQxHair(SurfaceOutputHair s, half3 viewDir, UnityGI gi)
{
    clip(s.Alpha - 0.5f);
    fixed4 resultColor = fixed4(0, 0, 0, s.Alpha);
    // Direct light ,单散射, single scatter
    resultColor.rgb = gi.light.color * HairBxDF(s, s.Normal, viewDir, gi.light.dir, 1.0f, 1.0f, 0.0f);    
    

    return resultColor;
}

void LightingQxHair_GI(SurfaceOutputHair s, UnityGIInput data,inout UnityGI gi)
{
    gi = UnityGlobalIllumination(data, 1.0, s.Normal);
}