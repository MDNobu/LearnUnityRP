#ifndef __QxCommon
#define __QxCommon

#include "QxUnityInput.hlsl"

#define UNITY_MATRIX_M unity_ObjectToWorld
#define UNITY_MATRIX_I_M unity_WorldToObject
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_P glstate_matrix_projection

#define UNITY_MATRIX_I_V unity_MatrixInvV

// 前一帧的matrix，这里还没找到unity的内置变量，先这样做
#define UNITY_PREV_MATRIX_M  unity_ObjectToWorld
#define UNITY_PREV_MATRIX_I_M unity_WorldToObject
  
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

float3 QxTransformObjectToWorld(float3 positionOS)
{
    return mul(unity_ObjectToWorld, float4(positionOS, 1.f)).xyz;
}

float4 QxTransformWorldToHClip(float3 positionWS)
{
    return mul(unity_MatrixVP, float4(positionWS, 1.0));
}

#endif