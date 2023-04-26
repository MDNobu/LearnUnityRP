Shader "Custom/QxCloud" {
	Properties {
		[Enum_Switch(RealTime, No3DTex, Bake)]
		_RenderMode("渲染模式", float) = 0
		
		
		[Foldout]
		_Shape("形状_Foldout", float) = 1
		[HideInInspector]
		_WeatherTexTiling("WeatherTexTiling", Range(0.1, 30)) = 1
		[Switch(Realtime, No3DTex)]
		_WeatherTexOffset("WeatherTexOffset", vector) = (0, 0, 0, 0)
		[Tex(_BaseShapeTexTiling, RealTime, Bake)][NoScaleOffset]
		_BaseShapeTex("BaseShapeTex", 3D) = "white"{}
		[HideInInspector]
		_BaseShapeTexTiling("BaseShapeTexTiling", Range(0.1, 5)) = 1
		[Switch(RealTime)]
		_BaseShapeDetailEffect("BaseShapeDetailEffect", Range(0, 1)) = 0.5
		[Switch(Bake)]
		_BaseShapeRatio("BaseShapeRatio", vector) = (1, 1, 1, 1)
		
		[Foldout(2, 2, 1, 0, Realtime)]
		_Shape_Detail("ShapeDetail_Foldout", float) = 1
		[Tex(_DetailShapeTexTiling)][NoScaleOffset]
		_DetailShapeTex("DetailShapeTex", 3D) = "white"{}
		[HideInInspector]
		_DetailShapeTexTiling("DetailShapeTexTiling", Range(0.1, 3)) = 1
		_DetailEffect("DetailEffectStrength", Range(0, 1)) = 1
		
		[Foldout(2, 2, 0, 0, RealTime, No3DTex)]
		_Shape_Weather("WeatherSetting_Foldout", float) = 1
		[Range]
		_CloudHeightRange("云层高度 最大/最小范围", vector)  = (1500, 4000, 0, 8000)
		[Range(RealTime)]
		_StratusRange("层云范围", vector) = (0.1, 0.4, 0, 1)
		[Switch(Realtime)]
		_StratusFethear("层云边缘羽化", Range(0, 1)) = 0.2
		[Range(RealTime)]
		_CumulusRange("积云范围", vector) = (0.15, 0.8, 0, 1)
		[Switch(RealTime)]
		_CumulusFeather("积云边缘羽化", Range(0, 1)) = 0.2
		[PowerSlider(0.7)]
		_CloudCover("云层覆盖率", Range(0, 1)) = -0.5
		[Switch(No3DTex)]
		_CloudOffsetLower("云底层偏移", Range(-1, 1)) = 0
		[Switch(No3DTex)]
		_CloudOffsetUppper("云顶层偏移", Range(-1, 1)) = 0
		[Switch(No3DTex)]
		_CloudFether("云层边缘羽化", Range(0, 1)) = 0.2
		
		[Folderout(2, 2, 0, 0)]
		_Shape_EFfect("性能_Foldout", float)=1
		[Switch(Bake)]
		_SDFScale("SDFScale", Range(0, 1)) = 1
		_ShapeMarchLength("形状单次步进长度", Range(0.001, 800)) = 300
		_ShapeMarchMax("形状最大步进次数", Range(3, 100)) = 30
		
		[Foldout(2, 2, 0, 0)]
		_Shape_Other("形状其它设置_Foldout", float) = 1
		_BlueNoiseEffect("蓝噪声影响程度", Range(0, 1)) = 1
		[Vector3(Realtime, No3DTex)]
		_WindDirection("WindDirection", vector) = (1, 0, 0, 0)
		[Switch(RealTime, No3DTex)]
		_WindSpeed("WindSpeed", Range(0, 5)) = 1
		_Density_Scale("密度缩放", Range(0, 2)) = 1
		
		        [Foldout]_Lighting ("光照_Foldout", float) = 1
        [Foldout(2, 2, 0, 0)]_Lighting_Convention ("常规_Foldout", float) = 1
        _CloudAbsorb ("云层吸收率", Range(0, 4)) = 0.5
        [Space]
        _ScatterForward ("向前散射", Range(0, 0.99)) = 0.5
        _ScatterForwardIntensity ("向前散射强度", Range(0, 1)) = 1
        [Space]
        _ScatterBackward ("向后散射", Range(0, 0.99)) = 0.4
        _ScatterBackwardIntensity ("向后散射强度", Range(0, 1)) = 0.4
        [Space]
        _ScatterBase ("基础散射", Range(0, 1)) = 0.2
        _ScatterMultiply ("散射乘数", Range(0, 1)) = 0.7
        
        [Foldout(2, 2, 0, 0)]_Lighting_Color ("颜色_Foldout", float) = 1
        [HDR]_ColorBright ("亮面颜色", color) = (1, 1, 1, 1)
        [HDR]_ColorCentral ("中间颜色", color) = (0.5, 0.5, 0.5, 1)
        [HDR]_ColorDark ("暗面颜色", color) = (0.1, 0.1, 0.1, 1)
        _ColorCentralOffset ("中间颜色偏移", Range(0, 1)) = 0.5
        [Space]
        _DarknessThreshold ("暗部阈值", Range(0, 1)) = 0.3
        
        [Foldout(2, 2, 0, 0, RealTime, No3DTex)]_Lighting_Effect ("性能_Foldout", float) = 1
        _LightingMarchMax ("光照最大步进次数", Range(1, 15)) = 8
        
        
        [Foldout_Out(1)]_FoldoutOut ("跳出折叠页_Foldout", float) = 1
        
        [HideInInspector]_MainTex ("Texture", 2D) = "white" { }
        [HideInInspector]_BoundBoxMin ("_BoundBoxMin", vector) = (-1, -1, -1, -1)
        [HideInInspector]_BoundBoxMax ("_BoundBoxMax", vector) = (1, 1, 1, 1)
		}
	SubShader {
		
		Tags {"RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline"}
		LOD 100
		
		HLSLINCLUDE
		#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
		#include "QxCloudHelp.hlsl"

		CBUFFER_START(UnityPerMaterial)

		float _WeatherTexTilling;
		float2 _WeatherTexOffset;
		float _BaseShapeTexTiling;
		float _BaseShapeDetailEffect;
		float3 _BaseShapeRatio;

		float _DetailShapeTexTilling;
		float _DetailEffect;

		float4 _CloudHieghtRange;
		float4 _StratusRange;
		float _StratusFeather;
		float4 _CumulusRange;
		float _CumulusFeather;
		float _CloudCover;
		float _CloudOffsetLower;
		float _CloudOffsetUpper;
		float _CloudFeather;

		float _SDFScale;
		float _ShapeMarchLength;
		int _ShapeMarchMax;

		float _BlueNoiseEffect;
		float3 _WindDirection;
		float _WindSpeed;
		float _DensityScale;

		float _CloudAbsorb;
		float _ScatterForward;
		float _ScatterForwardIntensity;
		float _ScatterBackward;
		float _ScatterBackwardIntensity;
		float _ScatterBase;
		float _ScatterMultiply;

		half4 _ColorBright;
		half4 _ColorCentral;
		half4 _ColorDark;
		float _ColorCentralOffset;
		float _DarknessThreshold;

		int _LightingMarchMax;

		// 蓝噪声uv
		float2 _BlueNoiseTexUV;
		// 当前帧数
		int _FrameCount;

		// 纹理宽度
		int _Width;
		int _Height;

		float3 _BoundBoxMin;
		float3 _BoundBoxMax;
		CBUFFER_END
		
		ENDHLSL
		
		Pass
		{
			HLSLPROGRAM

			#pragma vertex vert
			#pragma fragment frag

			#pragma shader_feature _RENDERMODE_REALTIME _RENDERMODE_NO3DTEX _RENDERMODE_BAKE
            #pragma shader_feature _SHAPE_DETAIL_ON
            #pragma multi_compile _OFF _2X2 _4X4

			TEXTURE2D(_MainTex);
			SAMPLER(sampler_MainTex);
			TEXTURE2D(_CameraDepthTexture);
			SAMPLER(sampler_CameraDepthTexture);
			TEXTURE2D(_BlueNoiseTex);
			SAMPLER(sampler_BlueNoiseTex);

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 viewDir : TEXCOORD1;
			};

			v2f vert(appdata vIn)
			{
				v2f vOut;

				VertexPositionInputs vertexPos = GetVertexPositionInputs(vIn.vertex.xyz);
				vOut.vertex = vertexPos.positionCS;
				vOut.uv = vIn.uv;

				float3 viewDir = mul(unity_CameraInvProjection, float4(vIn.uv * 2.0 - 1.0, 0, -1)).xyz;
				vOut.viewDir = mul(unity_CameraToWorld, float4(viewDir, 0)).xyz;
				return vOut;
			}

			half4 frag(v2f psIn) : SV_Target
			{
				half4 backColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, psIn.uv);

				float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, psIn.uv).x;
				float dstToObj = LinearEyeDepth(depth, _ZBufferParams);

				// 获取灯光信息
				Light mainLight = GetMainLight();

				float3 viewDir = normalize(psIn.viewDir);
				float3 lightDir = normalize(mainLight.direction);
				float3 cameraPos = GetCameraPositionWS();

				 //地球半径在6,357km到6,378km
                float earthRadius = 6300000;

				float3 sphereCenter = float3(cameraPos.x, -earthRadius, cameraPos.z);
				// 包围盒缩放
				float boundBoxScaleMax = 1;
				// 包围盒位置
				float3 boundBoxPosition = (_BoundBoxMax + _BoundBoxMin) / 2.0;

				
			}
			
			ENDHLSL
			
		}
	}
	
}