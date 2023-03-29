Shader "QxRP/QxLightPass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull off ZWrite on ZTest Always
        Tags { 
            }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
			#include "BRDF.cginc"
            #include "globalUniforms.cginc"
          

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };




            float4 _TestLightColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i, out float depthOut : SV_Depth) : SV_Target
            {
            	float2 uv = i.uv;
            	float4 GT2 = tex2D(_GT2, uv);
            	float4 GT3 = tex2D(_GT3, uv);

            	// 从GBuffer 解码数据
            	float3 albedo = tex2D(_GT0, uv).rgb;
            	float3 normalFromText = tex2D(_GT1, uv).xyz; 
            	float3 normal = tex2D(_GT1, uv).xyz * 2.0 - 1.0;
            	float2 motionVec = GT2.rg;
            	float roughness = GT2.b;
            	float metallic = GT2.a;
            	float3 emission = GT3.rgb;
            	float occlusion = GT3.a;

            	float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
            	float depthLinear = Linear01Depth(d);
            	

            	// 反投影重建世界坐标
            	float4 posNDC = float4(uv * 2 - 1, d, 1);
            	float4 worldPos = mul(_vpMatrixInv, posNDC);
            	worldPos /= worldPos.w;

				//
            	float3 N = normalize(normal);
            	float3 L = normalize(_WorldSpaceLightPos0.xyz);
            	float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos);
            	float3 irradiance = _LightColor0 ;// _TestLightColor.rgb;//unity_LightColor[0].rgb;
            	irradiance = saturate(irradiance);

            	// 计算光照
            	float3 color = PBR(N, V, L, albedo, irradiance, roughness, metallic);

            	float3 ambient = IBL(
            		N, V,
            		albedo, roughness, metallic,
            		_diffuseIBL, _specularIBL, _brdfLut
            		);

            	// color += ambient * occlusion;
            	color += emission;

				color = ambient;
            	
            	depthOut = d;
                return float4(color, 1);
            }
            ENDCG
        }
    }
}
