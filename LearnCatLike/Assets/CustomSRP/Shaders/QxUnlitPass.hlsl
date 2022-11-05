#ifndef __QxUnlitPass__
#define __QxUnlitPass__

#include "QxCommon.hlsl"

CBUFFER_START(UnityPerMaterial)
    float4 _BaseColor;
CBUFFER_END

float4 QxUnlitPassVertex(float3 positionOS : POSITION) : SV_Position 
{
    // float3 positionWS = QxTransformLocalToWorld(positionOS);
    // return QxTransformWorldToHClip(positionWS);
    float3 positionWS = TransformLocalToWorld(positionOS);

    return TransformObjectToHClip(positionWS);
}

float4 QxUnlitPassFragment() : SV_Target
{
    return _BaseColor;
}

#endif