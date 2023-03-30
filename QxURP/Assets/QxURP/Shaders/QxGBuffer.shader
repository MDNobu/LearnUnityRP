Shader "QxRP/QxGBuffer"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		
		_Metallic_global("Metallic", Range(0, 1)) = 0.5
		_Roughness_global("Roughness", Range(0, 1)) = 0.5
		[Toggle] _Use_Metal_Map("Use Metal Map", Float) = 1
		_MetallicGlossMap("Metallic Map", 2D) = "white" {}
		
		_EmissionMap("Emission Map", 2D) = "black" {}
		_OcclusionMap("Occlusion Map", 2D) = "white" {}
		[Toggle] _Use_Normal_Map("Use Normal Map", Float) = 1
		[Normal] _NormalMap ("Normal Map", 2D) = "bump" {}
	}
	SubShader
	{

		Pass
		{
			Tags {  "LightMode" = "shadowDepthOnly"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"

			struct v2f
			{
				float4 vertex : SV_Position;
				float2 dpeth : TEXCOORD0;
			};

			v2f vert(appdata_base vIn)
			{
				v2f vOut;
				vOut.vertex = UnityObjectToClipPos(vIn.vertex);
				vOut.dpeth = vOut.vertex.zw;
				return vOut;
			}

			half4 frag(v2f psIn) : SV_Target0
			{
				float d = psIn.dpeth.x / psIn.dpeth.y;
				#if defined (UNITY_REVERSED_Z)
				d = 1.0 -d;
				#endif

				half4 c = EncodeFloatRGBA(d);
				return c;
			}
			ENDCG
		}
		
		
		// GBuffer/Base pass
		Pass
		{
			Tags {"LightMode"="gbuffer"}
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			#pragma enable_d3d11_debug_symbols
			
			#include "UnityCG.cginc"
			
			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
				float3 normal : NORMAL;
			};

			sampler2D _MainTex;
			sampler2D _MetallicGlossMap;
			sampler2D _EmissionMap;
			sampler2D _OcclusionMap;
			sampler2D _NormalMap;

			float _Use_Metal_Map;
			float _Use_Normal_Map;
			float _Metallic_global;
			float _Roughness_global;
			
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				o.normal = UnityObjectToWorldNormal(v.normal);
				return o;
			}
			
			void frag (v2f i,
				out float4 GT0 : SV_Target0,
				out float4 GT1 : SV_Target1,
				out float4 GT2 : SV_Target2,
				out float4 GT3 : SV_Target3
				)
			{
				// sample the texture
				float4 baseColor = tex2D(_MainTex, i.uv);
				float3 emission = tex2D(_EmissionMap, i.uv).rgb;
				float3 normal = i.normal;
				float metallic = _Metallic_global;
				float roughness = _Roughness_global;
				float ao = tex2D(_OcclusionMap, i.uv).g;

				if (_Use_Metal_Map)
				{
					float4 metal = tex2D(_MetallicGlossMap, i.uv);
					metallic = metal.r;
					roughness = 1 - metal.a;
				}
				// if (_Use_Normal_Map)
				// {
				// 	normal = UnpackNormal(tex2D(_NormalMap, i.uv));
				// }
				GT0 = baseColor;
				GT1 = float4(normal * 0.5 + 0.5, 0);
				GT2 = float4(0, 0, roughness, metallic);
				GT3 = float4(emission, ao);
			}
			ENDCG
		
//			HLSLPROGRAM
//			#pragma vertex QxBasePassVertex
//			#pragma fragment QxBassPassFragment
//			#include "QxBasePass.hlsl"
//			ENDHLSL
		}
	}
}