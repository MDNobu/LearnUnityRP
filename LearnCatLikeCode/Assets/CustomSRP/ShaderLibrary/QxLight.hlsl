#ifndef __QxLight__
#define __QxLight__

#define MAX_DIRECTIONAL_LIGHT_COUNT 4
struct QxLight
{
    float3 color;
    float3 direction;
};



CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
    // float3 _DirectionalLightColor;
    // float3 _DirectionalLightDirection;
CBUFFER_END

int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

QxLight GetDirectionalLight(int index)
{
    QxLight light;
    light.color = _DirectionalLightColors[index];
    light.direction = _DirectionalLightDirections[index];
    return light;
}

#endif