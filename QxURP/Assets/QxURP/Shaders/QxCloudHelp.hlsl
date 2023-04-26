#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE3D(_BaseShapeTex);
SAMPLER(sampler_BaseShapeTex);
TEXTURE3D(_DetailShapeTex);
SAMPLER(sampler_DetailShapeTex);
TEXTURE2D(_WeatherTex);
SAMPLER(sampler_WeatherTex);


// 射线于球体相交,x进入球面， y穿过球体
// //原理是将射线方程(x = o + dl)带入球面方程求解(|x - c|^2 = r^2)
float2 RaySphereDst()
{
    
}

// 射线和云层相交， x进入云层,y射出云层
// 同2个射线和球体相交进行计算
float2 RayCloudLayerDst()
{
    float2 cloudDstMin = 
}