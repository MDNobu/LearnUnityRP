#ifndef __QxBRDF__
#define __QxBRDF___

#include "QxSurface.hlsl"

struct QxBRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};

#define MIN_REFLECTIVITY 0.04

float OneMinusReflectivity(float matallic)
{
    float range = 1.0 - MIN_REFLECTIVITY;
    return range * (1 - matallic);
}

QxBRDF GetBRDF(QxSurface surface, bool applyAlphaToDiffuse = false)
{
    QxBRDF brdf;
    float oneMinusReflectivity = OneMinusReflectivity(surface.metallic);
    brdf.diffuse = surface.color * oneMinusReflectivity;
    if (applyAlphaToDiffuse)
    {
        brdf.diffuse *= surface.alpha;
    }
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);

    float perceptualRoughness =
        PerceptualSmoothnessToRoughness(surface.smoothness);
    brdf.roughness = perceptualRoughness;
    return brdf;
}

#endif