﻿Shader "QxRP/QxLit"
{
    Properties
    {
        _BaseMap("Texture", 2D) = "white" {}
        _BaseColor("Color", Color) = (0.5, 0.5, 0.5, 1)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        _Metallic("Metallic", Range(0, 1)) = 0
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        [Toggle(_PREMULTIPLAY_ALPHA)] _PermulAlpha("Premultiply Alpha", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0 
        [Enum(Off, 0, On, 1)] _ZWrite("Z Write", Float) = 1
    }
    SubShader
    {

        Pass
        {
            Tags {
                "LightMode" = "CustomLit"
                }
            
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]
            HLSLPROGRAM
            #pragma target 3.5
            #pragma shader_feature _Clipping
            #pragma shader_feature _PREMULTIPLAY_ALPHA
            #pragma multi_compile_instancing
            #pragma vertex QxLitPassVertex
            #pragma fragment QxLitPassFragment
            #include "QxLitPass.hlsl"
            ENDHLSL
        }
    }
    
    CustomEditor "QxCustomShaderGUI"
}