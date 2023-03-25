#pragma once


struct FVSInput
{
    float3 positionOS : POSITIOON;
    float3 normalOS : NORMAL;
    float2 baseUV : TEXCOORD0;
    
};

struct FVertexToPixel
{
    float4 positionCS : Sv_Position;    
};

FVertexToPixel QxBasePassVertex(
    FVSInput vsIn
    )
{
    FVertexToPixel vsOut = (FVertexToPixel)0;

    // vsOut.positionCS 

    return vsOut;
}


void QxBassPassFragment(
    FVertexToPixel psIn,
    out float4 OutTarget0 : SV_Target0,
    out float4 OutTarget1 : SV_Target1,
    out float4 OutTarget2 : SV_Target2,
    out float4 OutTarget3 : SV_Target3
    )
{
    
}