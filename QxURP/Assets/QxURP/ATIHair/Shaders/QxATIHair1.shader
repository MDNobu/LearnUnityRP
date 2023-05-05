Shader "QxCustom/QxATIHair1"
{
    Properties
    {
        _MainColor ("Main Color", Color) = (1, 1, 1, 1)
        _MainTex("Diffuse(RGB) Alpha(A)",2D) = "white" {}
        
        _NormalTex("Normal Map", 2D) = "Black" {}
        _NormalScale("Normal Scale", Range(0, 10)) = 1
        _Specular("Specular Amount", Range(0, 5)) = 1.0
        _SpecularColor1("Specular Color1", Color) = (1, 1, 1, 1)
        _SpecularColor2("Specular Color2", Color) = (1,1,1,1)
        _SpecularMultiplier1("Specular Power1", float) = 100.0
        _SpecularMultiplier2("Specular Power2", float) = 100.0
        
        _PrimaryShift("Specular Primary Shift", float) = 0.0
        _SecondaryShift("Specular Secondary Shift", float) = 0.7
        _AnisoDir("SpecShift(G), Spec Mask(B)", 2D) = "white" {}
        _Cutoff("Alpha Cut-off Threshold", float) = 0.3
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull Mode", Float) = 2
    }
    SubShader
    {
        Tags {"Queue" = "Transparent-10" "IgnoreProjector"="True" "RenderType"="TransparentCutout"}
        
        // 渲染头发的不透明部分，写深度
        Pass
        {
            ZWrite On
            Cull [_Cull]
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Lighting.cginc"
            #include "AutoLight.cginc"

            sampler2D _MainTex;
            float4 _MaintTex_ST;
            half4 _MainColor;

            half _Cutoff;
            half _NormalScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                float3 worldNormal : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            v2f vert(appdata_full vIn)
            {
                v2f vOut;
                UNITY_INITIALIZE_OUTPUT(v2f, vOut);
                vOut.vertex = UnityObjectToClipPos(vIn.vertex);
                vOut.uv.xy = TRANSFORM_TEX(vIn.texcoord, _MaintTex);

                vOut.worldPos = mul(unity_ObjectToWorld, vIn.vertex).xyz;
                vOut.worldNormal = UnityObjectToWorldNormal(vIn.normal);
                return  vOut;
            }

            fixed4 frag(v2f pIn) : SV_Target0
            {
                fixed4 alboedo  = tex2D(_MainTex, pIn.uv);
                clip(alboedo.a - _Cutoff);

                fixed3 worldNormal = normalize(pIn.worldNormal);
                float3 worldPos = pIn.worldPos;
                fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(worldPos));
                half NoL = saturate(dot(worldNormal, worldLightDir));

                UNITY_LIGHT_ATTENUATION(atten, pIn, pIn.worldPos)
                half4 finalColor = half4(0, 0, 0, alboedo.a);
                finalColor.rgb += (alboedo.rgb * _MainColor.rgb) * _LightColor0.rgb;
                // finalColor.rgb = float3(0, 0, 1);
                return finalColor;
            }
            
            ENDCG
        }
        
        Pass
        {
            Tags {"LightMode" = "ForwardBase"}
            ZWrite Off
            Cull [_Cull]
            Blend SrcAlpha OneMinusSrcAlpha
//            Blend One Zero
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Lighting.cginc"
            #pragma target 3.0

            sampler2D _MainTex,_AnisoDir, _NormalTex;
            float4 _MainTex_ST, _AnisoDir_ST, _NormalTex_ST;

            half _SpecularMultiplier1, _PrimaryShift,_Specular, _SecondaryShift, _SpecularMultiplier2;
            half4 _SpecularColor1, _MainColor, _SpecularColor2;

            half _Cutoff;
            half _NormalScale;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 uv : TEXCOORD0;
                float4 TtoW0 : TEXCOORD1;
                float4 TtoW1 : TEXCOORD2;
                float4 TtoW2 : TEXCOORD3;
                float4 vertex : SV_POSITION;
            };

            fixed3 ShiftTagent(fixed3 T, fixed3 N, fixed shift)
            {
                return normalize(T + shift * N);
            }

            fixed StrandSpecular(fixed3 T, fixed3 V, fixed3 L, fixed exponent)
            {
                fixed H = normalize(L + V);
                fixed ToH = dot(T, H);
                fixed sinTH = sqrt(1 - ToH * ToH);
                fixed dirAtten = smoothstep(-1, 0, ToH);
                return dirAtten * saturate(pow(sinTH, exponent));
            }

            v2f vert(appdata_full vIn)
            {
                v2f vOut;
                UNITY_INITIALIZE_OUTPUT(v2f, vOut);

                vOut.vertex = UnityObjectToClipPos(vIn.vertex);
                vOut.uv.xy = TRANSFORM_TEX(vIn.texcoord, _MainTex);
                vOut.uv.zw = TRANSFORM_TEX(vIn.texcoord, _NormalTex);

                float3 worldPos = mul(unity_ObjectToWorld, vIn.vertex).xyz;
                fixed3 worldNormal = UnityObjectToWorldNormal(vIn.normal);
                fixed3 worldTangent = UnityObjectToWorldDir(vIn.tangent.xyz);
                fixed3 worldBinormal = cross(worldNormal, worldTangent) * vIn.tangent.w;

                vOut.TtoW0 = float4(worldTangent.x, worldBinormal.x, worldNormal.x, worldPos.x);
                vOut.TtoW1 = float4(worldTangent.y, worldBinormal.y, worldNormal.y, worldPos.y);
                vOut.TtoW2 = float4(worldTangent.z, worldBinormal.z, worldNormal.z, worldPos.z);
                return vOut;
            }

            fixed4 frag(v2f pIn) : SV_Target
            {
                fixed4 albedo = tex2D(_MainTex, pIn.uv);
                half3 diffuseColor = albedo.rgb * _MainColor.rgb;

                fixed3 bump = UnpackScaleNormal(tex2D(_NormalTex, pIn.uv.zw), _NormalScale);
                fixed3 worldNormal = normalize(half3(dot(pIn.TtoW0.xyz, bump), dot(pIn.TtoW1.xyz, bump), dot(pIn.TtoW2.xyz, bump)));
                float3 worldPos = float3(pIn.TtoW0.w, pIn.TtoW1.w, pIn.TtoW2.w);
                fixed3 worldTangent = normalize(half3(pIn.TtoW0.x, pIn.TtoW1.x, pIn.TtoW2.x));
                fixed3 worldBinormal = normalize(half3(pIn.TtoW0.y, pIn.TtoW1.y, pIn.TtoW2.y));

                fixed3 worldViewDir = normalize(UnityWorldSpaceViewDir(worldPos));
                fixed3 worldLightDir = normalize(UnityWorldSpaceLightDir(worldPos));

                fixed3 spec = tex2D(_AnisoDir, pIn.uv).rgb;
                
                half shiftTex  = spec.g;
                // 这里为什么用binormal而不用tangent
                // 注意： 计算hair shading用的tangent切线表示的是从发根到发梢的Tip，这个和模型的tangent方向未必一致（方向大致相同），
                // 使用tangent和binormal进行偏移应该和模型上的走向相关，这里用binormal应该是因为模型中的binormal方向和头发方向相似
                half3 t1 = ShiftTagent(worldBinormal, worldNormal, _PrimaryShift + shiftTex);
                half3 t2 = ShiftTagent(worldBinormal, worldNormal, _SecondaryShift + shiftTex);

                half3 spec1 = StrandSpecular(t1, worldViewDir, worldLightDir, _SpecularMultiplier1) * _SpecularColor1;
                half3 spec2 = StrandSpecular(t2, worldViewDir, worldLightDir, _SpecularMultiplier2)  * _SpecularColor2;

                fixed4 finalColor = 0;
                finalColor.rgb  = diffuseColor + spec1 * _Specular;
                finalColor.rgb += spec2 * _SpecularColor2 * spec.b * _Specular;
                finalColor.rgb *= _LightColor0.rgb;
                finalColor.a = albedo.a;

                return finalColor;
            }
            ENDCG
            
        }
    }
}
