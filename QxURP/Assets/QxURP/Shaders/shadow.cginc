// Upgrade NOTE: replaced 'defined #else' with 'defined (#else)'

// Upgrade NOTE: replaced 'defined #else' with 'defined (#else)'


// 返回0或1, 0表示在阴影中，1表示没在阴影中
float ShadowMap01(float3 worldPos, sampler2D shadowTex, float4x4 shadowVpMatrix)
{
    float4 posShadowNDC = mul(shadowVpMatrix, float4(worldPos, 1.0f));
    posShadowNDC /= posShadowNDC.w;
    float2 uv = posShadowNDC.xy * 0.5 + 0.5;

    if (any(uv) <0 || any(uv) > 1)
        return 1.0f;

    float d = posShadowNDC.z;
    float dSampled = tex2D(shadowTex, uv).r;

    float result = 0.0f;
    #if defined  (UNITY_REVERSED_Z)
    result = d < dSampled ? 0.0f : 1.0f;
    #else
    result = dSampled > d ? 0.0f : 1.0f;
    #endif
    return result;
}

float ShadowMapPCSS(
    float4 worldPos, sampler2D shadowMapTex,
    float4x4 shadowVpMatrix, float orthoWidth,
    float orthoDistance, float shadowmapResolution,
    float rotateAngle, float pcssSearchRadius,
    float pcssFilterRadius
    )
{
    float posShadowNDC = mul(shadowVpMatrix, worldPos);
    posShadowNDC /= posShadowNDC.w;
    float depthShadowNDC = posShadowNDC.z;
    
    
}