#ifndef __QxSurface3__
#define __QxSurface3__

struct QxSurface
{
    float3 normal;
    float3 viewDirection;
    float3 color;
    float alpha;
    float metallic;
    float smoothness;
}; 

#endif