#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE3D(_BaseShapeTex);
SAMPLER(sampler_BaseShapeTex);
TEXTURE3D(_DetailShapeTex);
SAMPLER(sampler_DetailShapeTex);
TEXTURE2D(_WeatherTex);
SAMPLER(sampler_WeatherTex);

// 采样云时用的信息
struct SamplingInfo
{
    float3 position; // 采样位置
    float baseShapeTiling;
    float3 baseShapeRatio;
    float boundBoxScaleMax;
    float3 boundBoxPosition;
    float detailShapeTiling;
    float weatherTexTiling;
    float2 weatherTexOffset;
    float baseShapeDetailEffect;
    float detailEffect;
    float densityMultiplier;
    float cloudDensityAdjust; // //云密度调整，用于调整天气纹理云的覆盖率, 0 ~ 0.5 ~ 1 => 0 ~ weatherTex.r ~ 1
    float cloudAbsorptAdjust; // 云吸收率影响， 调整天气纹理云的吸收率， 0 ~ 0.5 ~ 1 => 0 ~ weatherTex.b ~ 1
    float3 windDirection;
    float windSpeed;
    float2 cloudHeightMinMax;  //云高度的最小(x) 最大(y)值
    float3 stratusInfo; //层云信息，层云最小高度(x)  层云最大高度(y)  层云边缘羽化强度(z)
    float3 cumulusInfo; // 积云信息, 积云最小高度(x)  积云最大高度(y)  积云边缘羽化强度(z)
    float cloudOffsetLower; // 云底部偏移，渲染模式为No3DTex时用
    float cloudOffsetUpper; // 云顶部偏移，渲染模式为No3DTex时用
    float feather; // 云层羽化,渲染模式为No3DTex时用
    float3 sphereCenter; // 地球中心坐标
    float earthRadius;
    
};

// 采样完后云的信息
struct CloudInfo
{
    float density; // 密度
    float absorptivity; //吸收率
    float sdf; // 有向距离长,渲染模式为Bake时启用
    float lum; // 烘焙的光照,渲染模式为Bake时启用
};

//射线和AABB相交，x是p + RayDir * t，中最近的t,y是穿过包围盒的距离
float2 RayBoxDst(float3 boxMin, float3 boxMax, float3 pos, float3 rayDir)
{
    float3 t0 = (boxMin - pos) / rayDir;
    float3 t1 = (boxMax - pos) / rayDir;

    float3 tmin = min(t0, t1);
    float3 tmax = max(t0, t1);

    // 射线到box两个交点的位置,dstA最近距离，dstB最远距离
    float dstA = max(max(tmin.x, tmin.y), tmin.z);
    float dstB = min(min(tmax.x, tmax.y), tmax.z);

    float dstToBox = max(0, dstA);
    float dstInBox = max(0, dstB - dstToBox);

    return float2(dstToBox, dstInBox);
}

// 射线于球体相交,x进入球面， y穿过球体
// //原理是将射线方程(x = o + dl)带入球面方程求解(|x - c|^2 = r^2)
// 可以参考https://zhuanlan.zhihu.com/p/136763389
float2 RaySphereDst(float3 sphereCenter, float sphereRadius, float3 pos, float3 rayDir)
{
    float3 oc = pos - sphereCenter;
    float b = dot(rayDir, oc);
    float c = dot(oc, oc) - sphereRadius * sphereRadius;
    float t = b * b - c; // t > 0,则有2个交点，=0相切， <0不想交

    float delta = sqrt(max(t, 0));
    float dstToSphere = max(0, -b - delta);
    float dstInSphere = max(-b + delta - dstToSphere, 0);
    return float2(dstToSphere, dstInSphere);
    
}

// 射线和云层相交， x进入云层, y穿过云层的距离
// 同2个射线和球体相交进行计算
float2 RayCloudLayerDst(float3 sphereCenter, float earthRadius, float heightMin,
    float heightMax, float3 pos, float3 rayDir, bool isShape = true)
{
    float2 cloudDstMin = RaySphereDst(sphereCenter, heightMin + earthRadius, pos, rayDir);
    float2 cloudDstMax = RaySphereDst(sphereCenter, heightMax + earthRadius, pos, rayDir);

    //射线到云层的最近距离
    float dstToCloudLayer  = 0;
    // 射线穿过云层的距离
    float dstInCloudLayer = 0;

    // 形状步进计算相交 #TODO 这个isSphape是什么意思
    if (isShape)
    {
        // 在地表上
        if (pos.y <= heightMin)
        {
            float3 startPos = pos + rayDir * cloudDstMin.y;
            // 开始位置在地平线上时，设置距离
            if (startPos.y >= 0)
            {
                dstToCloudLayer = cloudDstMin.y;
                dstInCloudLayer = cloudDstMax.y - cloudDstMin.y;
            }
            return float2(dstToCloudLayer, dstInCloudLayer);
        }

        // 在云层内
        if (pos.y > heightMin && pos.y <= heightMax)
        {
            dstToCloudLayer = 0;
            dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.y : cloudDstMax.y;
            return float2(dstToCloudLayer, dstInCloudLayer);
        }

        // 在云层外
        dstToCloudLayer = cloudDstMax.x;
        dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.x - dstToCloudLayer :cloudDstMax.y;
    }
    else // 光照步进时，步进开始点一定在云层内
    {
        dstToCloudLayer = 0;
        // #TODO 这个是什么意思
        dstInCloudLayer = cloudDstMin.y > 0 ? cloudDstMin.x : cloudDstMax.y;
    }

    return float2(dstToCloudLayer, dstInCloudLayer);
}

// 获得高度比率
float GetHeightFraction(float3 sphereCenter, float earthRadius,
    float3 pos, float heightMin, float heightMax)
{
    float heigth = length(pos - sphereCenter) - earthRadius;
    return (heigth - heightMin) / (heightMax - heightMin);
}

// 重映射
float Remap(float originalValue, float originalMin, float originalMax, float newMin, float newMax)
{
    return newMin + (newMax - newMin) * ((originalValue - originalMin)/(originalMax - originalMin));
}

// 获取云类型密度
float GetCloudTypeDensity(float heightFraction, float cloudMin, float cloudMax, float feather)
{
    // 云的底部羽化要弱一些，所以乘0.5
    return saturate(Remap(heightFraction,cloudMin, cloudMin + feather * 0.5, 0, 1)) *
        saturate(Remap(heightFraction, cloudMax - feather, cloudMax, 1, 0));
}

// 在3个值之间插值， offset用于中间值value2的偏移  #TODO???
float Interpolation3(float value1, float value2, float value3, float x, float offset = 0.5)
{
    offset = clamp(offset, 0.0001, 0.9999);
    return lerp(lerp(value1, value2,min(x, offset)/offset), value3, max(0, x - offset)/(1.0 - offset));
}

// 在3个值之间插值， offset用于中间值value2的偏移  #TODO???
float3 Interpolation3(float3 value1, float3 value2, float3 value3, float x, float offset = 0.5)
{
    offset = clamp(offset, 0.0001, 0.9999);
    return lerp(lerp(value1, value2,min(x, offset)/offset), value3, max(0, x - offset)/(1.0 - offset));
}

// 计算天气纹理的UV
float2 GetWeatherTexUV(float3 sphereCenter, float3 pos,
    float weatherTiling, float weatherTexRepair)
{
    float3 direction = normalize(pos - sphereCenter);
    //uv为pos.xz平铺，但是在球的边缘处会出现明显的拉伸
    //这里通过除以direction.y(处理过)来对边缘进行缩放，以减少拉伸，虽然仍然会有问题
    float2 uv  = pos.xy / pow(abs(direction.y), weatherTexRepair);
    return uv * weatherTiling;
}

// 获得索引，给定一个uv,纹理宽度高度，以及要分帧的次数，返回当前uv所对应的迭代索引
int GetIndex(float2 uv, int width, int height, int iterationCount)
{
    //分帧渲染时的顺序索引
    int FrameOrder_2x2[] = {
        0, 2, 3, 1
    };
    int FrameOrder_4x4[] = {
        0, 8, 2, 10,
        12, 4, 14, 6,
        3, 11, 1, 9,
        15, 7, 13, 5
    };

    // #TODO 这里/8 是为什么
    int x = floor(uv.x * width / 8) % iterationCount;
    int y = floor(uv.y * height / 8) % iterationCount;
    int index = x + y * iterationCount;

    if (iterationCount == 2)
    {
        index = FrameOrder_2x2[index];
    }
    if (iterationCount == 4)
    {
        index = FrameOrder_4x4[index];
    }

    return index;
}

CloudInfo SampleCloudDensity_No3DTex(SamplingInfo dsi)
{
    CloudInfo o;

    float heightFraction =
        GetHeightFraction(dsi.sphereCenter, dsi.earthRadius, dsi.position,
            dsi.cloudHeightMinMax.x, dsi.cloudHeightMinMax.y);

    // 添加风的影响
    float3 wind = dsi.windDirection * dsi.windSpeed * _Time.y;
    float3 position = dsi.position + wind * 100;

    // 采样天气纹理，默认1000km平铺，r是密度，g吸收率，b云层底部高度，a云层顶部高度
    float2 weatherTexUV = dsi.position.xz * dsi.weatherTexTiling;
    float4 weatherData = SAMPLE_TEXTURE2D_LOD(_WeatherTex, sampler_WeatherTex, weatherTexUV *  0.000001 +
        dsi.weatherTexOffset + wind.xz * 0.01, 0
        );

    weatherData.r = Interpolation3(0, weatherData.r, 1, dsi.cloudDensityAdjust);
    weatherData.b = saturate(weatherData.b + dsi.cloudOffsetLower);
    weatherData.a = saturate(weatherData.a + dsi.cloudOffsetUpper);
    // 云底部的高度
    float lowerLayerHeight = Interpolation3(weatherData.b, weatherData.b, 0, dsi.cloudDensityAdjust);
    // 云顶部的高度
    float upperLayerHeight = Interpolation3(weatherData.a, weatherData.a, 1, dsi.cloudDensityAdjust);
    if (weatherData.r <= 0)
    {
        o.density = 0;
        o.absorptivity = 1;
        return o;
    }

    // 计算云密度
    float cloudDensity = GetCloudTypeDensity(heightFraction, min(lowerLayerHeight, upperLayerHeight),
        max(lowerLayerHeight, upperLayerHeight), dsi.feather);
    if (cloudDensity <= 0)
    {
        o.density = 0;
        o.absorptivity = 1;
        return o;
    }

    // 云吸收率
    float cloudabsorptivity = Interpolation3(0, weatherData.g, 1, 1, dsi.cloudDensityAdjust);

    cloudDensity *= weatherData.r;

    o.density = cloudDensity * dsi.densityMultiplier * 0.01;
    o.absorptivity = cloudabsorptivity;

    return o;
}

// 采样云的密度， isCheaply = true时不采样细节纹理
CloudInfo SampleCloudDensity_RealTime(SamplingInfo dsi, bool isCheaply = true)
{
    CloudInfo o;

    float heightFraction = GetHeightFraction(dsi.sphereCenter,
        dsi.earthRadius, dsi.position, dsi.cloudHeightMinMax.x,
        dsi.cloudHeightMinMax.y);

    // 添加风的影响
    float3 wind = dsi.windDirection * dsi.windSpeed * _Time.y;
    float3 position = dsi.position + wind * 100;

    //采样天气纹理，默认1000km平铺， r 密度, g 吸收率, b 云类型(0~1 => 层云~积云)
    float2 weatherTexUV = dsi.position.xz * dsi.weatherTexTiling;
    float4 weatherData = SAMPLE_TEXTURE2D_LOD(_WeatherTex, sampler_WeatherTex,
        weatherTexUV * 0.000001 + dsi.weatherTexOffset + wind.xz * 0.01, 0);
    weatherData.r = Interpolation3(0, weatherData.r, 1, dsi.cloudDensityAdjust);
    weatherData.b = Interpolation3(0, weatherData.b, 1, dsi.cloudDensityAdjust);
    if (weatherData.r <= 0)
    {
        o.density = 0;
        o.absorptivity = 1;
        return o;
    }

    // 计算云类型的密度
    float stratusDensity = GetCloudTypeDensity(heightFraction,
        dsi.stratusInfo.x, dsi.stratusInfo.y, dsi.stratusInfo.z);
    float cumulusDensity = GetCloudTypeDensity(heightFraction,
        dsi.cumulusInfo.x, dsi.cumulusInfo.y, dsi.cumulusInfo.z);
    float cloudTypeDensity = lerp(stratusDensity, cumulusDensity, weatherData.b);
    if (cloudTypeDensity <= 0)
    {
        o.density = 0;
        o.absorptivity = 1;
        return o;
    }

    // 云吸收率
    float cloudAbsorptivity = Interpolation3(0, weatherData.g, 1, dsi.cloudDensityAdjust);

    // 采样基础纹理
    float4 baseTex = SAMPLE_TEXTURE3D_LOD(_BaseShapeTex, sampler_BaseShapeTex, position * dsi.baseShapeTiling * 0.0001, 0);
    // 构建基础纹理的FBM
    float baseTexFBM = dot(baseTex.gba, float3(0.5, 0.25, 0.125));
    // 对基础形状添加细节，通过Remap可以不影响基础形状下添加细节
    float baseShape = Remap(baseTex.r, saturate((1.0 - baseTexFBM) * dsi.baseShapeDetailEffect), 1.0,
        0, 1.0);

    float cloudDensity = baseShape * weatherData.r * cloudTypeDensity;

    // 添加细节
    if (cloudDensity > 0 && !isCheaply)
    {
        // 细节噪声受更强风的影响，添加稍微向上的偏移
        position += (dsi.windDirection + float3(0, 0.1, 0)) * dsi.windSpeed * _Time.y * 0.1;
        float3 detailTex = SAMPLE_TEXTURE3D_LOD(_DetailShapeTex, sampler_DetailShapeTex,
            position * dsi.detailShapeTiling * 0.0001, 0).rgb;
        float detailFBM = dot(detailTex,float3(0.5, 0.25, 0.125));

        // 根据高度从纤细到波纹的形状进行变化
        float detailNoise = detailFBM;
        // 通过使用remap映射细节噪声，可以保留基本形状，在边缘进行变化
        cloudDensity = Remap(cloudDensity, detailNoise * dsi.detailEffect,
            1.0, 0.0, 1.0);
    }

    o.density = cloudDensity * dsi.densityMultiplier * 0.01;
    o.absorptivity = cloudAbsorptivity;
    return o;
}

// 采样云的密度，通过已经烘焙好的3D纹理
CloudInfo SampleCloudDensity_Bake(SamplingInfo dsi)
{
    CloudInfo o;
    // 映射回原本比例
    float3 postion = dsi.position - dsi.boundBoxPosition - dsi.boundBoxScaleMax * dsi.baseShapeRatio / 2.0;
    postion = postion / dsi.baseShapeRatio / dsi.boundBoxScaleMax;
    // 采样3D纹理
    float4 baseTex = SAMPLE_TEXTURE3D_LOD(_BaseShapeTex, sampler_BaseShapeTex, postion, 0);

    o.density = baseTex.r * dsi.densityMultiplier * 0.02;
    o.sdf = baseTex.g * dsi.boundBoxScaleMax;
    o.lum = baseTex.b;
    o.absorptivity = dsi.cloudAbsorptAdjust;
    return o;
}

// 采样云的密度
CloudInfo SampleCloudDensity(SamplingInfo dsi, bool isCheaply = true)
{
    
    #ifdef  _RENDERMODE_REALTIME
        return SampleCloudDensity_RealTime(dsi, isCheaply);
    #elif _RENDERMODE_NO3DTEX
        return SampleCloudDensity_No3DTex(dsi);
    #else
        return SampleCloudDensity_Bake(dsi);
    #endif
}


//////////////云的光照计算帮助函数////////////
// Beer 衰减
float Beer(float density, float absorptivity = 1)
{
    return exp(-density * absorptivity);
}

//粉糖效应，模拟云的内散射影响
float BeerPowder(float density, float absorptivity = 1)
{
    return 2.0 * exp(-density * absorptivity) * (1.0 - exp(-2.0 * density));
}

//Henyey-Greenstein相位函数
float HenyeyGreenstein(float angle, float g)
{
    float g2 = g * g;
    return(1.0 - g2) / (4.0 * PI * pow(1.0 + g2 - 2.0 * g * angle, 1.5));
}

//两层Henyey-Greenstein散射，使用Max混合。同时兼顾向前 向后散射
float HGScatterMax(float angle, float g_1, float intensity_1, float g_2, float intensity_2)
{
    return max(intensity_1 * HenyeyGreenstein(angle, g_1), intensity_2 * HenyeyGreenstein(angle, g_2));
}

// 两层Henyey-Greenstein散射，使用Lerp混合。同时兼顾向前 向后散射
float HGScatterLerp(float angle, float g_1, float g_2, float weight)
{
    return lerp(HenyeyGreenstein(angle, g_1), HenyeyGreenstein(angle,g_2), weight);
}

// 获取光照亮度
float GetLightEnergy(float density, float absorptivity, float darknessThreshold)
{
    float energy = BeerPowder(density, absorptivity);
    return darknessThreshold + (1.0 - darknessThreshold) * energy;
}

