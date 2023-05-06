Shader "QxCustom/QxSSR"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _MaxStep ("MaxStep", Float) = 10
        _StepSize("StepSize", Float) = 1
        _MaxDistance("MaxDistance", Float) = 10
        _Thickness("Thickness", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;

                float4 vertex : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
            float _MaxStep;
            float _StepSize;
            float _MaxDistance;
            float _Thickness;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_CameraDepthNormalTexture);
            SAMPLER(sampler_CameraDepthNormalTexture);

            //===========================================================================
            inline float3 DecodeViewNormalStereo( float4 enc4 )
            {
                 float kScale = 1.7777;
                 float3 nn = enc4.xyz*float3(2*kScale,2*kScale,0) + float3(-kScale,-kScale,1);
                 float g = 2.0 / dot(nn.xyz,nn.xyz);
                 float3 n;
                 n.xy = g*nn.xy;
                 n.z = g-1;
                 return n;
            }
            inline float DecodeFloatRG( float2 enc )
            {
                 float2 kDecodeDot = float2(1.0, 1/255.0);
                 return dot( enc, kDecodeDot );
            }
             inline void DecodeDepthNormal( float4 enc, out float depth, out float3 normal )
            {
                 depth = DecodeFloatRG (enc.zw);
                 normal = DecodeViewNormalStereo (enc);
            }
            //===========================================================================
            
            v2f vert(appdata vIn)
            {
                v2f vOut;
                vOut.vertex = TransformObjectToHClip(vIn.vertex);
                vOut.uv = vIn.uv;
                #if UNITY_UV_STARTS_TOP
                o.uv.y=1-o.uv.y;
                #endif

                return vOut;
            }

            half4 frag(v2f pIn ) : SV_Target
            {
                half4 finalColor = 0;


                half4 depthNormals = SAMPLE_TEXTURE2D(_CameraDepthNormalTexture, sampler_CameraDepthNormalTexture, pIn.uv);
                half linear01Depth;
                float3 normalVS;
                DecodeDepthNormal(depthNormals, linear01Depth, normalVS);


                // #TODO 从深度重建view space坐标
                float3 posVS;

                half3 viewDir = normalize(posVS);
                normalVS = normalize(normalVS);
                half3 
                
                // ray marching
                
                
                return finalColor;
            }
            
            ENDHLSL
        }
    }
}
