Shader "QxRP/QxLightPass"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Cull off ZWrite off ZTest Always
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

            #define PI 3.14159265358

            // Normal Distribution
			float Throwbridge_Reitz_GGX(float NoH, float a)
			{
				float a2 = a * a;
				float NoH2 = NoH * NoH;

				float nom = a2;
				float denom = (NoH2 * (a2 - 1.0) + 1.0);
				denom = PI * denom * denom;
				return nom / denom;
			}

			// Fresnel
			float3 SchlickFresnel(float HoV, float3 F0)
			{
				float m = clamp(1 - HoV, 0, 1);
				float m2 = m * m;
				float m5 = m2 * m2 * m;
				return F0 + (1.0 - F0) * m5;
			}

			// Geometry term (shadow mask term)
			float SchlickGGX(float NoV, float k)
			{
				float nom = NoV;
				float denom = NoV * (1.0 - k) + k;

				return nom / denom;
			}

			float PBR(float3 N, float3 V, float3 L, float3 albedo, float3 irradiance,
				float roughness, float metallic)
            {
	            roughness = max(roughness, 0.05);

            	float3 H = normalize(L + V);
            	float NoL = max(dot(N, L), 0);
            	float NoV = max(dot(N, V), 0);
            	float NoH = max(dot(N, H), 0);
            	float HoV = max(dot(H, V), 0);
            	float roughness2 = roughness * roughness;
            	float k = ((roughness2 + 1.0) * (roughness2 + 1.0)) / 8.0;
            	float3 F0 = lerp(float3(0.04, 0.04, 0.04), albedo, metallic);

            	float D = Throwbridge_Reitz_GGX(NoH, roughness2);
            	float3 F = SchlickFresnel(HoV, F0);
            	float G  = SchlickGGX(NoV, k) * SchlickGGX(NoL, k);

            	float3 k_s = F;
            	float3 k_d = (1.0 - k_s) * (1.0 - metallic);
            	float3 f_diffuse = albedo / PI;
            	float3 f_specular = (D * F * G) / (4.0 * NoV * NoL + 0.0001);

            	
            	float3 color = (k_d * f_diffuse + f_specular) * irradiance * NoL;
            	color = NoL * irradiance *  albedo;
            	color = N;
            	return color;
            }

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

            sampler2D _GT0;
            sampler2D _MainTex;
            sampler2D _GT1;
            sampler2D _GT2;
            sampler2D _GT3;
            sampler2D _gdepth;

            float4x4 _vpMatrix;
            float4x4 _vpMatrixInv;
            float4 _TestLightColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
            	float2 uv = i.uv;
            	float4 GT2 = tex2D(_GT2, uv);
            	float4 GT3 = tex2D(_GT3, uv);

            	// 从GBuffer 解码数据
            	float3 albedo = tex2D(_GT0, uv).rgb;
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
            	float N = normalize(normal);
            	float L = normalize(_WorldSpaceLightPos0.xyz);
            	float3 V = normalize(_WorldSpaceCameraPos.xyz - worldPos);
            	float3 irradiance = _LightColor0 ;// _TestLightColor.rgb;//unity_LightColor[0].rgb;

            	// 计算光照
            	float3 color = PBR(N, V, L, albedo, irradiance, roughness, metallic);
            	// color += emission;


            	color = N;
                return float4(color, 1);
            }
            ENDCG
        }
    }
}
