#ifndef CUSTOM_LIT_PASS_INCLUDED
#define CUSTOM_LIT_PASS_INCLUDED

#include "../ShaderLibrary/QxCommon.hlsl"
#include "../ShaderLibrary/QxSurface3.hlsl"
#include "../ShaderLibrary/QxLight.hlsl"
#include "../ShaderLibrary/QxLighting.hlsl"

// CBUFFER_START(UnityPerMaterial)
//     float4 _BaseColor;
// CBUFFER_END
TEXTURE2D(_BaseMap);
SAMPLER(sampler_BaseMap);

UNITY_INSTANCING_BUFFER_START(UnityPerMaterial)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseMap_ST)
    UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Cutoff)
UNITY_INSTANCING_BUFFER_END(UnityPerMaterial)

struct Atttibutes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_Position;
    float3 normalWS : VAR_NORMAL;
    float2 baseUV : VAR_BASE_UV;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

Varyings LitpasVertex(Atttibutes input ) 
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    // return  float4(positionWS, 1.f);
    // return QxTransformWorldToHClip(positionWS);
    output.positionCS = TransformWorldToHClip(positionWS);

    float4 baseST = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseMap_ST);
    output.baseUV = input.baseUV * baseST.xy + baseST.zw;
    output.normalWS = TransformObjectToWorldNormal(input.normalOS);
    return  output;
} 

float4 LitpassFragment(Varyings input) : SV_Target
{
    // return _BaseColor;
    UNITY_SETUP_INSTANCE_ID(input);
    float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _BaseColor);

    float4 baseMap = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.baseUV);
    float4 baseResult = baseColor * baseMap;

    #if defined(_CLIPPING)
    clip(baseResult.a - UNITY_ACCESS_INSTANCED_PROP(UnityPerMaterial, _Cutoff));
    #endif

    // baseResult.rgb = input.normalWS;
    // 测试法线，注意下面的input.normalWS是从vs传过来的经过插值的法线，不在是单位向量所以length(input.normalWS)是有意义的
    // 参考https://catlikecoding.com/unity/tutorials/custom-srp/directional-lights/
    // baseResult.rgb = abs(length(input.normalWS) - 1.0) * 10.0f;
    baseResult.rgb = normalize(input.normalWS);

    QxSurface surface;
    surface.normal = normalize(input.normalWS);
    surface.color = baseResult.rgb;
    surface.alpha = baseResult.a;

    float3 color = GetLighting(surface);
    return float4(color, surface.alpha);
}


#endif
