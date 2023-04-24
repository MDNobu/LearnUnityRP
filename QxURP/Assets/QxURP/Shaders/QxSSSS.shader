Shader "QxRP/QxSSSS"
{
	Properties
	{
	}
	SubShader
	{
		
		
		CGINCLUDE
		#include "QxSSSSCommon.cginc" 
		#pragma target 3.0
		ENDCG
		
		// No culling or depth
		Cull Off ZWrite Off ZTest Always
		Stencil
		{
			Ref 5
			comp equal
			pass keep
		}

		Pass
		{
			Name "XBlur"
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			#include "UnityCG.cginc"

			float4 frag(VertexOutput i) : SV_Target
			{
				float4 sceneColor = tex2D(_MainTex, i.uv);
				float sssIntensity = _SSSScale * _CameraDepthTexture_TexelSize.x;
				float3 xBlur = SSS(sceneColor, i.uv, float2(sssIntensity, 0)).rgb;
				return float4(xBlur, sceneColor.a);
			}
			
			ENDCG
		}
		
		Pass
		{
			Name "YBlur"
			
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase

			float4 frag(VertexOutput psIn) : SV_Target
			{
				float4 sceneColor = tex2D(_MainTex, psIn.uv);
				float sssIntensity = _SSSScale * _CameraDepthTexture_TexelSize.y;
				float3 yBlur = SSS(sceneColor, psIn.uv, float2(0, sssIntensity)).rgb;
				return float4(yBlur, sceneColor.a);
			}
			ENDCG
		}
	}
}