#ifndef __QxLight__
#define __QxLight__

#define MAX_DIRECTIONAL_LIGHT_COUNT 4

// CBUFFER_START(_CustomLight)
//     float3 _DirectionalLightColor;
//     float3 _DirectionalLightDirection; //这里的方向是指向光源的方向，而不是离开光源的方向
// CBUFFER_END

CBUFFER_START(_CustomLight)
    int _DirectionalLightCount;
    float4 _DirectionalLightColors[MAX_DIRECTIONAL_LIGHT_COUNT];
    float4 _DirectionalLightDirections[MAX_DIRECTIONAL_LIGHT_COUNT];
CBUFFER_END


struct QxLight
{
    float3 color;
    float3 direction;
};

int GetDirectionalLightCount()
{
    return _DirectionalLightCount;
}

QxLight GetDirectionalLight(int index)
{
    QxLight light;
    light.color = _DirectionalLightColors[index].rgb;
    light.direction = _DirectionalLightDirections[index].xyz;
    return light;
}


#endif