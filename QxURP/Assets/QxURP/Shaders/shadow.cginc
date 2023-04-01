// Upgrade NOTE: replaced 'defined #else' with 'defined (#else)'

// Upgrade NOTE: replaced 'defined #else' with 'defined (#else)'

#define N_SAMPLE 64
static float2 poissonDisk[N_SAMPLE] = {
    float2(-0.5119625f, -0.4827938f),
    float2(-0.2171264f, -0.4768726f),
    float2(-0.7552931f, -0.2426507f),
    float2(-0.7136765f, -0.4496614f),
    float2(-0.5938849f, -0.6895654f),
    float2(-0.3148003f, -0.7047654f),
    float2(-0.42215f, -0.2024607f),
    float2(-0.9466816f, -0.2014508f),
    float2(-0.8409063f, -0.03465778f),
    float2(-0.6517572f, -0.07476326f),
    float2(-0.1041822f, -0.02521214f),
    float2(-0.3042712f, -0.02195431f),
    float2(-0.5082307f, 0.1079806f),
    float2(-0.08429877f, -0.2316298f),
    float2(-0.9879128f, 0.1113683f),
    float2(-0.3859636f, 0.3363545f),
    float2(-0.1925334f, 0.1787288f),
    float2(0.003256182f, 0.138135f),
    float2(-0.8706837f, 0.3010679f),
    float2(-0.6982038f, 0.1904326f),
    float2(0.1975043f, 0.2221317f),
    float2(0.1507788f, 0.4204168f),
    float2(0.3514056f, 0.09865579f),
    float2(0.1558783f, -0.08460935f),
    float2(-0.0684978f, 0.4461993f),
    float2(0.3780522f, 0.3478679f),
    float2(0.3956799f, -0.1469177f),
    float2(0.5838975f, 0.1054943f),
    float2(0.6155105f, 0.3245716f),
    float2(0.3928624f, -0.4417621f),
    float2(0.1749884f, -0.4202175f),
    float2(0.6813727f, -0.2424808f),
    float2(-0.6707711f, 0.4912741f),
    float2(0.0005130528f, -0.8058334f),
    float2(0.02703013f, -0.6010728f),
    float2(-0.1658188f, -0.9695674f),
    float2(0.4060591f, -0.7100726f),
    float2(0.7713396f, -0.4713659f),
    float2(0.573212f, -0.51544f),
    float2(-0.3448896f, -0.9046497f),
    float2(0.1268544f, -0.9874692f),
    float2(0.7418533f, -0.6667366f),
    float2(0.3492522f, 0.5924662f),
    float2(0.5679897f, 0.5343465f),
    float2(0.5663417f, 0.7708698f),
    float2(0.7375497f, 0.6691415f),
    float2(0.2271994f, -0.6163502f),
    float2(0.2312844f, 0.8725659f),
    float2(0.4216993f, 0.9002838f),
    float2(0.4262091f, -0.9013284f),
    float2(0.2001408f, -0.808381f),
    float2(0.149394f, 0.6650763f),
    float2(-0.09640376f, 0.9843736f),
    float2(0.7682328f, -0.07273844f),
    float2(0.04146584f, 0.8313184f),
    float2(0.9705266f, -0.1143304f),
    float2(0.9670017f, 0.1293385f),
    float2(0.9015037f, -0.3306949f),
    float2(-0.5085648f, 0.7534177f),
    float2(0.9055501f, 0.3758393f),
    float2(0.7599946f, 0.1809109f),
    float2(-0.2483695f, 0.7942952f),
    float2(-0.4241052f, 0.5581087f),
    float2(-0.1020106f, 0.6724468f)
};

float2 RotateVec2(float2 v, float angle)
{
    float sinA = sin(angle);
    float cosA = cos(angle);

    return float2(v.x*cosA+v.y*sinA, -v.x*sinA+v.y*cosA);
}

// 返回0或1, 0表示在阴影中，1表示没在阴影中
float ShadowMap01(float3 worldPos, sampler2D shadowTex, float4x4 shadowVpMatrix)
{
    float4 posShadowNDC = mul(shadowVpMatrix, float4(worldPos, 1.0f));
    posShadowNDC /= posShadowNDC.w;
    float2 uv = posShadowNDC.xy * 0.5 + 0.5;

    if (any(uv) <0 || any(uv) > 1)
        return 1.0f;

    float d = posShadowNDC.z;
    float dSampled = tex2D(shadowTex, uv).r;

    float result = 0.0f;
    #if defined  (UNITY_REVERSED_Z)
    result = d < dSampled ? 0.0f : 1.0f;
    #else
    result = dSampled > d ? 0.0f : 1.0f;
    #endif
    return result;
}

// 求pcss算法中平均的blocker深度
float2 AverageBlockerDepth(
    float4 posShadowNDC,
    sampler2D shadowmapTex,
    float depthShadowNDC,
    float searchWidth,
    float rotateAngle,
    float bias
    )
{
    float2 uv = posShadowNDC.xy * 0.5 + 0.5;
    float step = 3.0;
    float depthAverage = 0.0;
    float count = 0.0005; //防止除0

    for (int i = 0; i < N_SAMPLE; ++i)
    {
        float2 unitOffset = RotateVec2(poissonDisk[i], rotateAngle);
        float2 offset = unitOffset * searchWidth;
        float2 uvo = uv + offset;

        float depthSample = tex2D(shadowmapTex, uvo).r;
        // 被当前sample遮挡时，才作为depthAverage的计算元素
        if (depthShadowNDC + bias < depthSample)
        {
            count += 1;
            depthAverage += depthSample;
        }
    }

    depthAverage /= count;
    return float2(depthAverage, count);
}

// #TODO orthoDistance的含义
float ShadowMapPCSS(
    float3 worldPos, sampler2D shadowMapTex,
    float4x4 shadowVpMatrix, float orthoWidth,
    float orthoDistance, float shadowmapResolution,
    float rotateAngle, float pcssSearchRadius,
    float pcssFilterRadius, float bias
    )
{
    float4 posShadowNDC = mul(shadowVpMatrix, float4(worldPos, 1.0));
    posShadowNDC /= posShadowNDC.w;
    float depthShadowNDC = posShadowNDC.z;
    float2 uv = posShadowNDC.xy * 0.5 + 0.5;
    float2 dSampleDepth = tex2D(shadowMapTex, uv).r;

    // 计算平均遮挡深度, 这里其实是world space转换到screen space
    float searchWidth = pcssSearchRadius / orthoWidth;
    float2 blocker = AverageBlockerDepth(posShadowNDC, shadowMapTex, depthShadowNDC, searchWidth, rotateAngle, bias);
    float blockerDepthAvg = blocker.x;
    float blockerCnt = blocker.y;

    UNITY_BRANCH
    if (blockerCnt < 1)
    {
        return 1.0;
    }

    // 世界空间下的距离，计算PCSS用，注意reverseZ
    float depthReceiverWS = (1.0 - depthShadowNDC) * 2 * orthoDistance;
    float depthBlockerWS = (1.0 - blockerDepthAvg) * 2 * orthoDistance;

    // 世界空间下的filter 半径
    // 按照面光源、block、半影penumbra 的相似三角形规则求半影的pcf filter 大小
    float filterRadiusWS = (depthReceiverWS - depthBlockerWS) * pcssFilterRadius  / depthBlockerWS;
    // filterRadiusWS = 0.1f;
    // 深度图上的filter半径
    float filterRadius = filterRadiusWS/ orthoWidth;///orthoWidth; 

    // 点在shadow中是1
    float shadowFactor = 0.0f;

    // PCF
    for (int i = 0; i < N_SAMPLE; ++i)
    {
        float2 offset = poissonDisk[i];
        offset = RotateVec2(offset, rotateAngle);
        
        float2 uvo = uv + offset * filterRadius;

        float depthSample = tex2D(shadowMapTex, uvo).r;
        // 这里是小于是因为inverse z ?
        shadowFactor +=  (depthShadowNDC + bias < depthSample ? 1.0 : 0.0); 
    }
    shadowFactor /= N_SAMPLE;


    return 1  - shadowFactor; //这个函数需要的是visibility
}