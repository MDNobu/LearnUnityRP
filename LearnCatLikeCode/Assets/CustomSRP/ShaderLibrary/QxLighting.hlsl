#ifndef __QxLightning__
#define __QxLightning__

#include "QxSurface3.hlsl"
// float3 GetLighting(QxSurface surface)
// {
//     return surface.normal.y * surface.color;
// }

float SpecularStrength(QxSurface surface, QxBRDF brdf, QxLight light)
{
    // #TODO 这里的light.direction方向对吗?
    float3 h = SafeNormalize(light.direction + surface.viewDirection);
    float nh2 = Square(saturate(dot(surface.normal, h)));
    float lh2 = Square(saturate(dot(light.direction, h)));
    float r2 = Square(brdf.roughness);
    float d2 = Square(nh2 * (r2 - 1.0) + 1.00001);
    float normalization = brdf.roughness * 4.0 + 2.0;
    return r2 / (d2 * max(0.1, lh2) * normalization);
}


float3 DirectBRDF(QxSurface surface, QxBRDF brdf, QxLight light)
{
    return SpecularStrength(surface, brdf, light) * brdf.specular + brdf.diffuse;
}

float3 IncomingLight(QxSurface surface, QxLight light)
{
    return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLighting(QxSurface surface, QxBRDF brdf, QxLight light)
{
    return IncomingLight(surface, light) * DirectBRDF(surface, brdf, light);
}

float3 GetLighting(QxSurface surface, QxBRDF brdf)
{
    float3 color = 0.0;
    for (int i = 0; i < GetDirectionalLightCount(); ++i)
    {
        color += GetLighting(surface, brdf,  GetDirectionalLight(i));
    }
    // return GetLighting(surface, GetDirectionalLight());
    return color;
}




#endif