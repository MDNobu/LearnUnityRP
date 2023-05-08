Shader "QxCustom/PostProcessing/QxColorTint"
{
    Properties
    {
//        _MainTex ("Texture", 2D) = "white" {}
//        _Tint("Tint", Color)= (1, 1, 1, 1)
    }
    SubShader
    {
       Cull Off ZWrite Off ZTest Always
        
        Pass
        {
            HLSLPROGRAM
            #pragma vertex VertDefault
            #pragma fragment Frag

            #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

            TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
            float _BlendMultiply;
            float4 _Color;
            float4 Frag(VaryingsDefault i) : SV_Target
            {
                float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
                color = lerp(color, color * _Color, _BlendMultiply);
                return color;
            }
            ENDHLSL
        }
    }
}
