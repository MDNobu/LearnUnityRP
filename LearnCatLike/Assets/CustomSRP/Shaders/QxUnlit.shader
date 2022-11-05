Shader "QxRP/Unlit"
{
    Properties
    {
        _BaseColor("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {

        Pass
        {
            HLSLPROGRAM
            #pragma vertex QxUnlitPassVertex
            #pragma fragment QxUnlitPassFragment
            #include "QxUnlitPass.hlsl"
            ENDHLSL
        }
    }
}
