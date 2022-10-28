#ifndef __QxLightning__
#define __QxLightning__

#include "QxSurface3.hlsl"
// float3 GetLighting(QxSurface surface)
// {
//     return surface.normal.y * surface.color;
// }



float3 IncomingLight(QxSurface surface, QxLight light)
{
    return saturate(dot(surface.normal, light.direction)) * light.color;
}

float3 GetLighting(QxSurface surface, QxLight light)
{
    return IncomingLight(surface, light) * surface.color;
}

float3 GetLighting(QxSurface surface)
{
    return GetLighting(surface, GetDirectionalLight());
}

#endif