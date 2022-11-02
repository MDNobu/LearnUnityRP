#ifndef __QxBRDF__
#define __QxBRDF__

#include "QxSurface3.hlsl"
#include "QxCommon.hlsl"

#define MIN_REFLECTIVITY 0.04

struct QxBRDF
{
    float3 diffuse;
    float3 specular;
    float roughness;
};

float OneMinusReflectivity(float metallic)
{
    float range = 1 - MIN_REFLECTIVITY;
    return range * (1 - metallic);
}

QxBRDF GetBRDF(QxSurface surface)
{
    QxBRDF brdf = (QxBRDF)0;
    brdf.diffuse = surface.color * OneMinusReflectivity(surface.metallic);
    // brdf.specular = surface.color - brdf.diffuse;
    brdf.specular = lerp(MIN_REFLECTIVITY, surface.color, surface.metallic);

    float perceptualRoughness =
        PerceptualSmoothnessToPerceptualRoughness(surface.smoothness);
    brdf.roughness = PerceptualRoughnessToRoughness(perceptualRoughness);
    return brdf;
}

#endif