// 这个shader 用来实现一个简单的subsurface scattering，
// 其实主要是模拟transmission部分, 这个实现其实非常不物理


Shader "Custom/QxSimpleSSS" {
	
	Properties {
		_MainTex ("Texture", 2D) = "white" {}
		_Tint ("Tint", Color) = (1,1,1,1)
		_FrontSubsurfaceDistortion("FrontSubsurfaceDistortion", float) = 1
		_BackSubsurfaceDistortion("BackSubsurfaceDistortion", float) = 1
		_SubsurfaceColor("SubsurfaceColor", Color) = (1, 1, 1, 1)
		_SubsurfaceColorPower("SubsurfaceColorPower", float) = 1
		_FrontSSSIntensity("FrontSSSIntensity", float) = 1
		_Gloss("Gloss", float) = 1
		_RimPower("RimPower", float) = 1
		_RimIntensity("RimIntensity", float) = 1
	}
	SubShader {
		Tags {"RenderType"= "Opaque"}		
		
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#include "UnityCG.cginc"
			#include "UnityLightingCommon.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 posClipS : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 posWS : TEXCOORD1;
				float3 normalWS : TEXCOORD2;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			float4 _Tint;
			float4 _SubsurfaceColor;
			float _FrontSubsurfaceDistortion;
			float _BackSubsurfaceDistortion;
			float _SubsurfaceColorPower;
			float _Gloss;
			float _RimPower;
			float _RimIntensity;
			float _FrontSSSIntensity;

			float SimpleSubsurfaceScattering(
				float3 viewDirWS,
				float3 tolightWS,
				float3 normalWS,
				float frontSSSDistortion,
				float backSSDistortion,
				float frontSSSIntensity 
				)
			{
				// 计算模式散射的光方向
				float3 frontLitDir = normalWS * frontSSSDistortion - tolightWS;
				float3 backLitDir = normalWS * backSSDistortion + tolightWS;
				float frontSSS = saturate(dot(viewDirWS, -frontLitDir));
				float backSSS = saturate(dot(viewDirWS, -backLitDir));
				float result = saturate(frontSSS * frontSSSIntensity + backSSS);
				return result;
			}

			v2f vert (appdata v)
			{
				v2f o;
				o.posClipS = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.posWS = mul(unity_ObjectToWorld, v.vertex);
				o.normalWS = UnityObjectToWorldNormal(v.normal);
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float4 texColor = tex2D(_MainTex, i.uv) * _Tint;
				float3 viewDirWS = normalize(UnityWorldSpaceViewDir(i.posWS).xyz);
				float3 normalWS = normalize(i.normalWS.xyz);
				float3 toLightDir = normalize(-_WorldSpaceLightPos0.xyz);
				
				
				// SSS
				float sss = SimpleSubsurfaceScattering(viewDirWS, toLightDir, normalWS,
					_FrontSubsurfaceDistortion,_BackSubsurfaceDistortion,
					_FrontSSSIntensity);
				float3 sssColor = lerp(_SubsurfaceColor, _LightColor0, saturate(pow(sss, _SubsurfaceColorPower))).rgb * sss;

				//Diffuse
				float4 unLitCol = texColor * _SubsurfaceColor*0.5;
				float diffuse = dot(normalWS, toLightDir);
				float4 diffuseCol = lerp(unLitCol,texColor,diffuse);
				//Specular
				float specularPow = exp2((1 - _Gloss) * 10 + 1);
				float3 halfDir = normalize(toLightDir + viewDirWS);
				float3 specular = pow(max(0, dot(halfDir, normalWS)), specularPow);
				specular *= _LightColor0.rgb;
				//Rim
				float rim = 1.0 - max(0, dot(normalWS, viewDirWS));
				float rimValue = lerp(rim, 0, sss);
				float3 rimCol = lerp(_SubsurfaceColor, _LightColor0.rgb, rimValue)*pow(rimValue, _RimPower)*_RimIntensity;

				float3 final = sssColor + diffuseCol.rgb+specular+rimCol;
				return float4(final,1);
			}
			
			ENDCG
		}
	}
}