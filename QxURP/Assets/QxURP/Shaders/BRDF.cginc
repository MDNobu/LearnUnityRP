  #define PI 3.14159265358

// Normal Distribution
float Throwbridge_Reitz_GGX(float NoH, float a)
{
    float a2 = a * a;
    float NoH2 = NoH * NoH;

    float nom = a2;
    float denom = (NoH2 * (a2 - 1.0) + 1.0);
    denom = PI * denom * denom;
    return nom / denom;
}

// Fresnel
float3 SchlickFresnel(float HoV, float3 F0)
{
    float m = clamp(1 - HoV, 0, 1);
    float m2 = m * m;
    float m5 = m2 * m2 * m;
    return F0 + (1.0 - F0) * m5;
}

// Geometry term (shadow mask term)
float SchlickGGX(float NoV, float k)
{
    float nom = NoV;
    float denom = NoV * (1.0 - k) + k;

    return nom / denom;
}

float3 PBR(float3 N, float3 V, float3 L, float3 albedo, float3 irradiance,
    float roughness, float metallic)
{
    roughness = max(roughness, 0.05);

    float3 H = normalize(L + V);
    float NoL = max(dot(N, L), 0);
    float NoV = max(dot(N, V), 0);
    float NoH = max(dot(N, H), 0);
    float HoV = max(dot(H, V), 0);
    float roughness2 = roughness * roughness;
    float k = ((roughness2 + 1.0) * (roughness2 + 1.0)) / 8.0;
    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);

    float D = Throwbridge_Reitz_GGX(NoH, roughness2);
    float3 F = SchlickFresnel(HoV, F0);
    float G  = SchlickGGX(NoV, k) * SchlickGGX(NoL, k);

    float3 k_s = F;
    float3 k_d = (1.0 - k_s) * (1.0 - metallic);
    float3 f_diffuse = albedo / PI;
    float3 f_specular = (D * F * G) / (4.0 * NoV * NoL + 0.0001);

    // Unity albedo 没乘pi,为保持d s的比例,specular 也乘以PI
    f_diffuse *= PI;
    f_specular *= PI;

    float3 diffuseTerm = k_d * f_diffuse * irradiance * NoL;
    float3 specularTerm = f_specular * irradiance * NoL;
    float3 color = (k_d * f_diffuse + f_specular) * irradiance * NoL;
    // color = albedo;

    // color = NoL;
    // color = f_specular  * NoL * irradiance;
    // color = diffuseTerm + specularTerm;
    return color;
}

// Unity 使用这个作为IBL的 Fresnel 项
float3 FresnelSchlickRoughness(float NoV, float3 F0, float roughness)
{
    float r1 = 1 - roughness;
    return F0 + (max(float3(r1, r1, r1), F0) - F0) * pow(1 - NoV, 5.0f);
}

float3 IBL(
    float3 N, float3 V,
    float3 albedo, float roughness, float metallic,
    samplerCUBE diffuseIBL, samplerCUBE specularIBL,
    sampler2D brdfLut
    )
{
    roughness = min(roughness, 0.99);

    float3 H = normalize(N); //用法向量作为半角向量
    float NoV = max(dot(N, V), 0);
    float HoV = max(dot(H, V), 0);
    float3 R = normalize(reflect(-V, N)); // 反射向量

    float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);

    float3 F = FresnelSchlickRoughness(HoV, F0, roughness);
    float3 k_s = F;
    float3 k_d = (1.0 - k_s) * (1.0 - metallic);

    // 漫反射
    float3 IBL_D = texCUBE(diffuseIBL, N).rgb;
    float3 diffuse = k_d * albedo * IBL_D;

    // 镜面反射
    float rgh = roughness * (1.7 - 0.7 * roughness);
    float lod = 6.0 * rgh; // Unity默认6级mipmap
    float3 IBL_s = texCUBElod(specularIBL, float4(R, lod)).rgb;
    float2 brdf = tex2D(brdfLut, float2(NoV, roughness)).rg;
    float3 specular = IBL_s * (F0 * brdf.x + brdf.y);
    
    float3 ambient = diffuse + specular;
    ambient = specular;

    // 镜面反射
    return ambient;
}