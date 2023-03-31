Shader "QxRP/shadowProjectionPass"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			
			#include "UnityCG.cginc"
			#include "globalUniforms.cginc"
			#include "UnityLightingCommon.cginc"
			#include "shadow.cginc"

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

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			
			sampler2D _MainTex;

			float frag (v2f i) : SV_Target
			{
				// 解码GBuffer数据
				float2 uv = i.uv;
				float3 normal = tex2D(_GT1, uv).rgb * 2 - 1;
				float d = UNITY_SAMPLE_DEPTH(tex2D(_gdepth, uv));
				float depthLinear = Linear01Depth(d);

				// 反投影重建世界坐标
				float4 ndcPos = float4(uv * 2 - 1, d, 1);
				float4 worldPos = mul(_vpMatrixInv, ndcPos);
				worldPos /= worldPos.w;

				// 这个之后会加上沿法线的offset目的是消除self shadowing
				float3 worldPosIncludOffset = worldPos;

				float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
				float NoL = dot(normal, lightDir);

				UNITY_BRANCH
				if (NoL < 0.005)
				{
					return 0;
				}
				float bias = max(0.001, 0.001 * (1.0 - NoL));

				// 是否开启shadowMask优化
				if (_usingShadowMask)
				{
					// float mask = tex2D(_shadoMask, uv).r;
					// UNITY_BRANCH
					// if (mask < 0.0000005)
					// {
					// 	return 0;
					// }
					// UNITY_BRANCH
					// if (mask > 0.9999995)
					// {
					// 	return 1;
					// }
				}

				float visbility = 1.0f;
            	// 根据当前像素的相机空间 linear depth 选择不同的split shadow factor
                if (depthLinear < _split0)
                {
                	worldPosIncludOffset += normal * _shadowNormalBias0;
                    visbility *= ShadowMap01(worldPosIncludOffset, _shadowTex0, _shadowVpMatrix0);
                }
            	else if (depthLinear < _split0 + _split1)
                {
            		worldPosIncludOffset += normal * _shadowNormalBias1;
                    visbility *= ShadowMap01(worldPosIncludOffset, _shadowTex1, _shadowVpMatrix1);
                }
            	else if (depthLinear < _split0 + _split1 + _split2)
                {
            		worldPosIncludOffset += normal * _shadowNormalBias2;
                    visbility *= ShadowMap01(worldPosIncludOffset, _shadowTex2, _shadowVpMatrix2);
                }
            	else if (depthLinear < _split0 + _split1 + _split2 + _split3)
                {
            		worldPosIncludOffset += normal * _shadowNormalBias3;
                    visbility *= ShadowMap01(worldPosIncludOffset, _shadowTex3, _shadowVpMatrix3);
                } 
				
				return visbility;
			}
			ENDCG
		}
	}
}