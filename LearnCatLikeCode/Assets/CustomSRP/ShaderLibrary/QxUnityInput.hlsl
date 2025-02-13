﻿#ifndef __QxUnityInput
#define __QxUnityInput

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"


CBUFFER_START(UnityPerDraw)
float4x4 unity_ObjectToWorld;
float4x4 unity_WorldToObject;
float4 unity_LODFade;
real4 unity_WorldTransformParams;
CBUFFER_END

float4x4 unity_MatrixVP;
float4x4 unity_MatrixV;
float4x4 glstate_matrix_projection; 

float4x4 unity_MatrixInvV;
float3 _WorldSpaceCameraPos;


#endif