#ifndef __QxLight__
#define __QxLight__

struct QxLight
{
    float3 color;
    float3 direction;
};

QxLight GetDirectionalLight()
{
    QxLight light;
    light.color = 1.0;
    light.direction = float3(0., 1., 0.f);

    return light;
}

#endif