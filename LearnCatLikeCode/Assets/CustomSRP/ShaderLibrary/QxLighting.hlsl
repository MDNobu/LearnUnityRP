#ifndef __QxLightning__
#define __QxLightning__

#include "QxSurface3.hlsl"
float3 GetLighting(QxSurface surface)
{
    return surface.normal.y * surface.color;
}

#endif