Shader "QxRP/QxSubsurfaceScattering"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		// No culling or depth
//		Cull Off ZWrite Off ZTest Always
		Tags {"RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True"}

		HLSLINCLUDE


		 // Note: Screenspace shadow resolve is only performed when shadow cascades are enabled
        // Shadow cascades require cascade index and shadowCoord to be computed on pixel.
		// 下面的一部分计算假定了OpenGL, #TODO 改成兼容DX的实现
        // #pragma prefer_hlslcc gles
        // #pragma exclude_renderers d3d11_9x
		
		//Keep compiler quiet about Shadows.hlsl.
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/EntityLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ImageBasedLighting.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

		TEXTURE2D_FLOAT(_SkinDepth);
		SAMPLER(sampler_SkinDepth);
		float4 _SkinDepth_TexelSize;

		TEXTURE2D(_SkinDiffuse);
		SAMPLER(sampler_SkinDiffuse);

		float4 _ShapeParamsAndMaxScatterDists;
		float4 _WorldScalesAndFilterRadiiAndThicknessRemaps;
		float4x4 _vpMatrixInv;
		float _WorldScale;

		struct Attributes
		{
			float4 positionOS : POSITION;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};
		
		struct Varyings
		{
			float4 positionCS : SV_POSITION;
			float4 uv : TEXCOORD0;
			UNITY_VERTEX_OUTPUT_STEREO
		};

		void SampleBurleyDiffusionProfile(float u, float rcpS, out float r, out float rcpPdf)
        {
            u = 1 - u; // Convert CDF to CCDF

            float g = 1 + (4 * u) * (2 * u + sqrt(1 + (4 * u) * u));
            float n = exp2(log2(g) * (-1.0 / 3.0));                    // g^(-1/3)
            float p = (g * n) * n;                                   // g^(+1/3)
            float c = 1 + p + n;                                     // 1 + g^(+1/3) + g^(-1/3)
            float d = (3 / LOG2_E * 2) + (3 / LOG2_E) * log2(u);     // 3 * Log[4 * u]
            float x = (3 / LOG2_E) * log2(c) - d;                    // 3 * Log[c / (4 * u)]

            // x      = s * r
            // exp_13 = Exp[-x/3] = Exp[-1/3 * 3 * Log[c / (4 * u)]]
            // exp_13 = Exp[-Log[c / (4 * u)]] = (4 * u) / c
            // exp_1  = Exp[-x] = exp_13 * exp_13 * exp_13
            // expSum = exp_1 + exp_13 = exp_13 * (1 + exp_13 * exp_13)
            // rcpExp = rcp(expSum) = c^3 / ((4 * u) * (c^2 + 16 * u^2))
            float rcpExp = ((c * c) * c) * rcp((4 * u) * ((c * c) + (4 * u) * (4 * u)));

            r = x * rcpS;
            rcpPdf = (8 * PI * rcpS) * rcpExp; // (8 * Pi) / s / (Exp[-s * r / 3] + Exp[-s * r])
        }

		float2 QxSampleDiskGolden(uint i, uint sampleCount)
		{
			float2 f = Golden2dSeq(i, sampleCount);
			return float2(sqrt(f.x), TWO_PI * f.y);
		}

		 // Performs sampling of the Normalized Burley diffusion profile in polar coordinates.
        // The result must be multiplied by the albedo.
        float3 EvalBurleyDiffusionProfile(float r, float3 S)
        {
            float3 exp_13 = exp2(((LOG2_E * (-1.0 / 3.0)) * r) * S); // Exp[-S * r / 3]
            float3 expSum = exp_13 * (1 + exp_13 * exp_13);        // Exp[-S * r / 3] + Exp[-S * r]

            return (S * rcp(8 * PI)) * expSum; // S / (8 * Pi) * (Exp[-S * r / 3] + Exp[-S * r])
        }
        // Computes f(r, s)/p(r, s), s.t. r = sqrt(xy^2 + z^2).
        // Rescaling of the PDF is handled by 'totalWeight'.
        float3 ComputeBilateralWeight(float xy2, float z, float mmPerUnit, float3 S, float rcpPdf)
        {
            //如果想简化计算z可以不考虑
            // z = 0;
            float r = sqrt(xy2 + (z * mmPerUnit) * (z * mmPerUnit));
            float area = rcpPdf;
            return saturate(EvalBurleyDiffusionProfile(r, S) * area);
        }

		Varyings Vertex(Attributes input)
		{
			Varyings output;
			UNITY_SETUP_INSTANCE_ID(input);
			UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

			output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
			float4 cs = output.positionCS / output.positionCS.w;
			output.uv = ComputeScreenPos(cs);

			return output;
		}

		float _FrameCount;
		float _FilterRadii;

		// 4个像素一个采样点
		#define  SSS_PIXELS_PER_SAMPLE = 4

		half4 Fragment(Varyings psInput) : SV_Target
		{
			UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(psInput)
			float2 uv = psInput.uv.xy / psInput.uv.w;
			float2 posSS = uv * _SkinDepth_TexelSize.zw;
			float depth = SAMPLE_TEXTURE2D_X(_SkinDepth, sampler_SkinDepth, uv).r;
			float linearDepth = LinearEyeDepth( depth, _ZBufferParams);
			float2 cornerPosNDC = uv + 0.5 * _SkinDepth_TexelSize.xy;
			#if UNITY_REVERSED_Z
				depth = 1 - depth;
			#endif

			// UNITY_NEAR_CLIP_VALUE == -1是OpenGL的情况
			// #if UNITY_NEAR_CLIP_VALUE < 0
			// 	depth = 2 * depth - 1;
			// #endif
			

			float3 centerPosVS = ComputeViewSpacePosition(uv, depth, _InvProjMatrix);
			float3 cornerPosVS = ComputeViewSpacePosition(cornerPosNDC, depth, _InvProjMatrix);
			float mmPerUnit = 1000.0f;
			float unitsPerMm = rcp(mmPerUnit);
			float worldScale = _WorldScale;
			// 一个像素覆盖多少米
			float unitsPerPixel = max(0.001f, 2.0 * abs(cornerPosVS.x - centerPosVS.x)) * worldScale;
			float pixelsPerMm =  rcp(unitsPerPixel) * unitsPerMm;

			// SSS 散射最大距离,单位毫米
			// 已经预先在C#计算好
			float filterRadius = _FilterRadii;
			// 圆盘上覆盖多少个像素
			float filterArea = PI * Sq(filterRadius * pixelsPerMm);

			uint sampleCount = (uint)(filterArea/SSS_PIXELS_PER_SAMPLE);// 圆盘上有多少个采样点
			uint sampleBudget = 32;
			uint n = min(sampleCount, sampleBudget);
			float3 S = _ShapeParamsAndMaxScatterDists.rgb;
			float d = _ShapeParamsAndMaxScatterDists.a;
			float2 pixelCoord =  posSS;
			float3 totalIrradiance = 0;
			float3 totalWeight = 0;

			// 根据屏幕坐标生成一个随机角度
			float phase = TWO_PI * GenerateHashedRandomFloat(uint3(posSS, (uint)(depth * 16777216)));
			for (uint i = 0; i < n; ++i)
			{
				float scale = rcp(n);
				float offset = rcp(n) * 0.5;

				float sinPhase,cosPhase;
				sincos(phase, sinPhase, cosPhase);

				float r, rcpPdf;
				//通过i* scale + offset [0,1]的均匀递增数作为随机数计算出重要性采样的采样距离r和1/PDF
				SampleBurleyDiffusionProfile(i * scale + offset, d, r, rcpPdf);
				float phi = QxSampleDiskGolden(i, n).y;
				float sinPhi, cosPhi;
				sincos(phi, sinPhi, cosPhi);

				float sinPsi = cosPhase * sinPhi + sinPhase * cosPhi; // sin(phase + phi);
				float cosPsi = cosPhase * cosPhi - sinPhase * sinPhi; // cos(phase + phi);
				float2 vec = r * float2(cosPsi, sinPsi);
				// 根据采样距离r, 在圆盘上随机角度采样
				float2 position = pixelCoord + round((pixelsPerMm * r) * float2(cosPsi, sinPsi));

				float xy2 = r * r;
				float2 sampleUV = position * _SkinDepth_TexelSize.xy;
				float3 irrandiance = SAMPLE_TEXTURE2D_X(_SkinDiffuse,
					sampler_SkinDiffuse, sampleUV);
				//因为没有使用模板测试,在Diffuse计算时需要通过"diffuse.b = max(diffuse.b, HALF_MIN) 来表示"
				if (irrandiance.b > 0)
				{
					float sampleDevZ = SAMPLE_TEXTURE2D_X(_SkinDepth, sampler_SkinDepth, sampleUV).r;
					float samplerLinearZ = LinearEyeDepth(sampleDevZ, _ZBufferParams);
					float relZ = samplerLinearZ - linearDepth;
					// 根据r计算DiffusionProfile和权重
					float3 weight = ComputeBilateralWeight(xy2, relZ, mmPerUnit,
						S, rcpPdf);
					totalIrradiance += weight * irrandiance;
					totalWeight += weight;
				}
				
			}

			if (dot(totalIrradiance, float3(1, 1, 1)) == 0)
			{
				return SAMPLE_TEXTURE2D_X(_SkinDiffuse, sampler_SkinDiffuse, uv);
			}

			totalWeight = max(totalWeight, FLT_MIN);
			return float4(totalIrradiance/totalWeight, 1.0);
		}
		
		
		ENDHLSL
		
		Pass
		{
			Name "Subsurface Scattering Pass"
			ZTest Always
			ZWrite Off
			Cull Off
			
			HLSLPROGRAM
			#pragma vertex Vertex
			#pragma fragment Fragment
			ENDHLSL
		}
	}
}