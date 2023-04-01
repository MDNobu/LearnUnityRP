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
			#include "random.cginc"

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
			#define BASE_BIAS 0.000
			
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

				// 计算生成随机旋转角度，这个的用途看起来是对pcss 的blocker search区域进行旋转
				// 这个rotate angle的目的是为了防止后面采样时所有像素使用相同的sample parttern导致出现条带的artifact
				float rotateAngle  = 0.0f;
				{
					uint seed = RandomSeed(uv, float2(_screenWidth, _screenHeight));
					float2 uvNoise = uv * float2(_screenWidth, _screenHeight) / _noiseTexResolution;
					rotateAngle = rand(seed) * 2.0 * UNITY_PI;
					// 这里用unNoise * 0.5主要目的是减轻转相机时噪点的抖动, 另外还会造成噪点比例放大
					rotateAngle = tex2D(_noiseTex, uvNoise * 0.5).r * 2.0 * UNITY_PI;
					// rotateAngle = tex2D(_noiseTex, uvNoise).r * 2.0 * UNITY_PI;
				}
				// rotateAngle = 50;

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
                	float bias = _depthNormalBias0 * (1 - NoL) + BASE_BIAS;
                	bias *= _orthoWidth0/_shadowmapResolution;
                    visbility *= ShadowMapPCSS(
                    	worldPosIncludOffset, _shadowTex0,
                    	_shadowVpMatrix0, _orthoWidth0,
                    	_orthoDistance, _shadowmapResolution,
                    	rotateAngle, _pcssSearchRadius0,
                    	_pcssFilterRadius0, bias
                    	);
                }
            	else if (depthLinear < _split0 + _split1)
                {
            		worldPosIncludOffset += normal * _shadowNormalBias1;
            		float bias = _depthNormalBias1 * (1 - NoL) + BASE_BIAS;
                	bias *= _orthoWidth1/_shadowmapResolution;
                    // visbility *= ShadowMap01(worldPosIncludOffset, _shadowTex1, _shadowVpMatrix1);
            		visbility *= ShadowMapPCSS(
                    	worldPosIncludOffset, _shadowTex1,
                    	_shadowVpMatrix1, _orthoWidth1,
                    	_orthoDistance, _shadowmapResolution,
                    	rotateAngle, _pcssSearchRadius1,
                    	_pcssFilterRadius1, bias
                    	);
                }
            	else if (depthLinear < _split0 + _split1 + _split2)
                {
            		worldPosIncludOffset += normal * _shadowNormalBias2;
            		float bias = _depthNormalBias2 * (1 - NoL) + BASE_BIAS;
                	bias *= _orthoWidth2/_shadowmapResolution;
                    // visbility *= ShadowMap01(worldPosIncludOffset, _shadowTex2, _shadowVpMatrix2);
            		visbility *= ShadowMapPCSS(
                    	worldPosIncludOffset, _shadowTex2,
                    	_shadowVpMatrix2, _orthoWidth2,
                    	_orthoDistance, _shadowmapResolution,
                    	rotateAngle, _pcssSearchRadius2,
                    	_pcssFilterRadius2, bias
                    	);
                }
            	else if (depthLinear < _split0 + _split1 + _split2 + _split3)
                {
            		worldPosIncludOffset += normal * _shadowNormalBias3;
            		float bias = _depthNormalBias3 * (1 - NoL) + BASE_BIAS;
                	bias *= _orthoWidth3/_shadowmapResolution;
                    // visbility *= ShadowMap01(worldPosIncludOffset, _shadowTex3, _shadowVpMatrix3);
            		visbility *= ShadowMapPCSS(
                    	worldPosIncludOffset, _shadowTex3,
                    	_shadowVpMatrix3, _orthoWidth3,
                    	_orthoDistance, _shadowmapResolution,
                    	rotateAngle, _pcssSearchRadius3,
                    	_pcssFilterRadius3, bias
                    	);
                } 

				// visbility = 1.0;
				return visbility;
			}
			ENDCG
		}
	}
}