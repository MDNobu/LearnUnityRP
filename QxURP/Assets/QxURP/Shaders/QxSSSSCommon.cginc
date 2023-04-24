#include "UnityGBuffer.cginc"
#include "UnityCG.cginc"

#define DistanceToProjectionWindow     5.671281819617709             //1.0 / tan(0.5 * radians(20));
#define DPTimes300 1701.384545885313                             //DistanceToProjectionWindow * 300

#define SamplerSteps 25
uniform sampler2D _CameraDepthTexture;
float4 _CameraDepthTexture_TexelSize;

uniform sampler2D _MainTex;
uniform float4 _MainTex_ST;
uniform float _SSSScale;
uniform float4 _Kernel[SamplerSteps];

struct VertexInput
{
    float4 vertex : POSITION;
    float2 uv : TEXCOORD0;
};

struct VertexOutput
{
    float4 pos : SV_POSITION;
    float2 uv : TEXCOORD0;
};

VertexOutput vert(VertexInput vsIn)
{
    VertexOutput vsOut;
    vsOut.pos = vsIn.vertex;
    vsOut.uv = vsIn.uv;
    return vsOut;
}

float4 SSS(float4 sceneColor, float2 uv, float2 sssIntensity)
{
    float sceneDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv));
    float blurLength = DistanceToProjectionWindow/ sceneDepth;
    float2 uvOffset = sssIntensity * blurLength;
    float4 blurSceneColor = sceneColor;
    blurSceneColor.rgb *= _Kernel[0].rgb;

    [loop]
    for (int i = 1; i < SamplerSteps; ++i)
    {
        float2 sssUv = uv + _Kernel[i].a * uvOffset;
        float4 sssSceneColor = tex2D(_MainTex, sssUv);
        float sssDepth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sssUv)).r;
        float sssScale = saturate(DPTimes300 * sssIntensity * abs(sceneDepth - sssDepth));
        sssSceneColor.rgb = lerp(sssSceneColor.rgb, sceneColor.rgb, sssScale);
        blurSceneColor.rgb += _Kernel[i].rgb * sssSceneColor.rgb;
    }
    return blurSceneColor;
}
