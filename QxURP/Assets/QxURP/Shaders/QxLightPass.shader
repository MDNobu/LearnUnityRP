// Upgrade NOTE: replaced 'defined DebugShadowMap' with 'defined (DebugShadowMap)'

Shader "QxRP/QxLightPass"
{
    Properties
    {
    }
    SubShader
    {
        Cull off ZWrite on ZTest Always


        Pass
        {
	        Tags { 
//        		"LightsMode"="ForwardBase"
            }
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma enable_d3d11_debug_symbols

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc"
            #include "Lighting.cginc"
			#include "BRDF.cginc"
            #include "globalUniforms.cginc"
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

            #define  DebugShadowMap  0

            uint Index3DTo1D(uint3 index3D)
			{
			    return index3D.z * _numClusterX * _numClusterY +
			        index3D.y * _numClusterX +
			            index3D.x;
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
            	// irradiance = saturate(irradiance);

            	// 计算光照
            	float3 color = float3(0, 0, 0);
            	float3 direct = PBR(N, V, L, albedo, irradiance, roughness, metallic);

            	// 计算shadow factor, 0表示在阴影中
            	float visbility = 1.0f;
            	visbility = tex2D(_shadowStrength, uv).r;

            	float3 ambient = IBL(
            		N, V,
            		albedo, roughness, metallic,
            		_diffuseIBL, _specularIBL, _brdfLut
            		);

            	{
					color += direct * visbility;
            		color += emission;
					// color += ambient * occlusion;
            	}


            	// 根据cluster based light实现多个点光源的渲染
            	{
            		// 计算所属cluster
            		uint x = floor(uv * _numClusterX);
            		uint y = floor(uv * _numClusterY);
            		uint z = floor((1 - depthLinear) * _numClusterZ); // 注意这个depthLiner是反的

            		uint3 clusterID_3D = uint3(x, y, z);
            		uint clusterID = Index3DTo1D(clusterID_3D);
            		LightIndex lightIndexRange = _assignTable[clusterID];

            		int startIndex = lightIndexRange.start;
            		int endIndex = lightIndexRange.count + lightIndexRange.start;
            		// 得到影响当前cluster indices
                    for (int i = startIndex; i < endIndex; ++i)
                    {
	                    uint lightIndex = _lightAssignBuffer[i];
                    	PointLight curLight = _lightBuffer[lightIndex];

                    	float curLightIrradiance = curLight.color;
                    	L = normalize(curLight.position - worldPos);
                    	
                    	// 计算灯光衰减
                    	float lightAttenuation = 1.0f;
                    	float distP2L = distance(worldPos, curLight.position);
                    	float d2 = distP2L * distP2L;
                    	float r2 = curLight.radius * curLight.radius;
                    	lightAttenuation = saturate(1 - (d2/r2) * (d2/r2));
                    	lightAttenuation *= lightAttenuation;
                    	
                    	color += PBR(N, V, L, albedo, curLightIrradiance, roughness, metallic) * curLight.intensity;
                    }
            		

            		// 遍历光源进行shading 
            	}

            	color = direct;// * visbility;
            	// color = visbility;
            	
            	depthOut = d;

            	#if DebugShadowMap
            	// 根据当前像素的相机空间 linear depth 选择不同的split shadow factor
                if (depthLinear < _split0)
                {
                    color *= float3(1, 0, 0);
                }
            	else if (depthLinear < _split0 + _split1)
                {
                    color *= float3(0, 1, 0);
                }
            	else if (depthLinear < _split0 + _split1 + _split2)
                {
                    color *= float3(0, 0, 1);
                }
            	else if (depthLinear < _split0 + _split1 + _split2 + _split3)
                {
                    color *= float3(1, 1, 0);
                } 
            	#endif
                return float4(color, 1);
            }
            ENDCG
        }
    }
}
