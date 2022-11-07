#ifndef __QxSurface__
#define __QxSurface__

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