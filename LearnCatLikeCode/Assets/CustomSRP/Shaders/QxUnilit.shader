Shader "CustomRP/QxUnlit"
{
    Properties
    { 
        _BaseMap("Texture", 2D) = "White" {}
        _BaseColor("Color", Color) = (1., 1., 1., 1.)
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [Toggle(_CLIPPING)] _Clipping("Alpha Clipping", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend("Dst Blend", Float) = 0
        [Enum(Off, 0, On, 1)]   _Zwrite ("Z Write", Float) = 1 
        
    }
    
    SubShader 
    {
        Pass
        {
            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_Zwrite]
            
            HLSLPROGRAM
            #pragma enable_d3d11_debug_symbols
            #pragma shader_feature _CLIPPING
            #pragma multi_compile_instancing
            #pragma vertex UnlitPasssVertex
            #pragma fragment UnlitPassFragment
            #include "QxUnlitPass.hlsl"
            ENDHLSL
        }
    }
    
}