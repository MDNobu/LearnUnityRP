Shader "QxRP/QxShadowDepth"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "LightMode"="shadowDepthOnly" }

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			// make fog work
			#pragma multi_compile_fog
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float2 depth : TEXCOORD0;
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.depth = o.vertex.zw;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float d = i.depth.x / i.depth.y;
				#if defined (UNITY_REVERSED_Z)
				d = 1.0 - d;
				#endif

				fixed4 c = EncodeFloatRGBA(d);
				return c;
			}
			ENDCG
		}
	}
}