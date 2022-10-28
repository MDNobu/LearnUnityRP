#ifndef __QxLight__
#define __QxLight__

struct QxLight
{
    float3 color;
    float3 direction;
};


CBUFFER_START(_CustomLight)
    float3 _DirectionalLightColor;
    float3 _DirectionalLightDirection;
CBUFFER_END

QxLight GetDirectionalLight()
{
    QxLight light;
    light.color = _DirectionalLightColor;
    light.direction = _DirectionalLightDirection;

    return light;
}

#endif