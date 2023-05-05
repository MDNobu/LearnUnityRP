Shader "Custom/QxHair"
{
    // 参照UE4实现基于Marschnner hair model的头发渲染
    Properties
    {
        _MainTex("PropertyMap R-Depth G-ID B-Root A-Alpha", 2D) = "white" {}
        _Flowmap("Flowmap", 2D) = "white" {}
        _RootColor("RootColor", Color) = (0, 0, 0, 1)
        _TipColor("TipColor", Color) = (0, 0, 0, 1)
        _ColorVariationRange("ColorVariation-HSV", Vector) = (0, 0, 0, 1)
        _EccentricityMean("EccentricityMean", Float) = 0.07 //这个的目的是用来控制折射率的
        _RoughnessRange("RoughnessRange", Vector) = (0.3, 0.5, 0.0)
    }
    SubShader
    {
        Tags { "RenderType"="TransparentCutout"  "Queue"="AlphaTest"}
        LOD 200
        Cull off

        CGPROGRAM
        #include "HairBxDF.cginc"
        // Physically based Standard lighting model, and enable shadows on all light types fullforwardshadows
        #pragma surface surf QxHair fullforwardshadows  vertex:vert
        #pragma target 3.0

        sampler2D _MainTex;
        sampler2D _Flowmap;
        float3 _RootColor;
        float3 _TipColor;
        float4 _ColorVariationRange;
        float _EccentricityMean;
        float4 _RoughnessRange;

        float3 rgb2hsv(float3 c)
		{
			float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
			float4 p = lerp(float4(c.bg, K.wz), float4(c.gb, K.xy), step(c.b, c.g));
			float4 q = lerp(float4(p.xyw, c.r), float4(c.r, p.yzx), step(p.x, c.r));

			float d = q.x - min(q.w, q.y);
			float e = 1.0e-10;
			return float3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
		}

		float3 hsv2rgb(float3 c)
		{
			float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
			float3 p = abs(frac(c.xxx + K.xyz) * 6.0 - K.www);
			return c.z * lerp(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
		}

        // 这里的作用应该相当于UE4中的dither aa,通过dither 模拟半透明效果
        float ScreenDitherToAlpha(float x, float y, float c0)
        {
	        const float dither[64] = {
				0, 32, 8, 40, 2, 34, 10, 42,
				48, 16, 56, 24, 50, 18, 58, 26 ,
				12, 44, 4, 36, 14, 46, 6, 38 ,
				60, 28, 52, 20, 62, 30, 54, 22,
				3, 35, 11, 43, 1, 33, 9, 41,
				51, 19, 59, 27, 49, 17, 57, 25,
				15, 47, 7, 39, 13, 45, 5, 37,
				63, 31, 55, 23, 61, 29, 53, 21 };
        	int xMat = int(x) & 7;
        	int yMat = int(y) & 7;

        	float limit = (dither[yMat * 8 + xMat] + 11.0) / 64.0;
        	return lerp(limit * c0, 1.0, c0);
        }
        
        struct Input
        {
            float2 uv_MainTex;
            float4 screenUV;
            float3 normalWS;
        };

        void vert(inout  appdata_full v, out Input o)
        {
            UNITY_INITIALIZE_OUTPUT(Input, o);
            o.uv_MainTex = v.texcoord;
            o.normalWS = UnityWorldToObjectDir(v.normal); // 这里应该是假设了uniform scale
            o.screenUV = ComputeScreenPos(UnityObjectToClipPos(v.vertex));
        }


        // Add instancing support for this shader. You need to check 'Enable Instancing' on materials that use the shader.
        // See https://docs.unity3d.com/Manual/GPUInstancing.html for more information about instancing.
        // #pragma instancing_options assumeuniformscaling
        UNITY_INSTANCING_BUFFER_START(Props)
            // put more per-instance properties here
        UNITY_INSTANCING_BUFFER_END(Props)

        void surf (Input IN, inout SurfaceOutputHair o)
        {
            half4 property = tex2D(_MainTex, IN.uv_MainTex);
            half4 flowmap = tex2D(_Flowmap, IN.uv_MainTex);

            float3 baseColor = lerp(_RootColor, _TipColor, property.b);

        	// #TODO 下面这段的意图是，校色???
        	// 好像目的是为了校色，有必要吗??
            float3 baseColorHSV = rgb2hsv(baseColor);
        	baseColorHSV += (property.g  - 0.5) * _ColorVariationRange.rgb * _ColorVariationRange.a;
        	o.Albedo = hsv2rgb(baseColorHSV);
        	// o.Albedo = baseColor;

        	o.Eccentric = lerp(0.0f, _EccentricityMean * 2.0, property.r);
        	o.Normal = flowmap * 2.0 - 1.0;
        	o.Roughness = lerp(_RoughnessRange.x, _RoughnessRange.y, property.g);

        	// animating dither, 这里的目的应该是通过dither模拟半透明效果
        	float2 screenPixel = (IN.screenUV.xy/IN.screenUV.w) * _ScreenParams.xy; // + _Time.yz * 100;
        	float dither = ScreenDitherToAlpha(screenPixel.x, screenPixel.y, property.a);
        	// 这里dither的目的是为了整体更自然的过渡，对比测试下面这行注释可以看到
        	// dither = property.a;
        	o.Alpha = dither;

        	o.VNormal = IN.normalWS;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
