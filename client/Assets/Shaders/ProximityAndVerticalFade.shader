Shader "Custom/ProximityAndVerticalFade"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}

        _FadeAlpha ("Fade Alpha", Range(0,1)) = 0.1

        _FadeStartY ("Fade Start Y", Float) = -0.2
        _FadeEndY ("Fade End Y", Float) = -1.2

        _TargetPosA ("Target A (World)", Vector) = (0,0,0,0)
        _TargetPosB ("Target B (World)", Vector) = (0,0,0,0)
        _ProximityRadius ("Proximity Radius", Float) = 0.2
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 200
        // Cull Off
        Cull Back       // 关闭背面剔除，避免褶皱的地方发生不透明重叠
        ZWrite Off
        // ZWrite On       // 关闭深度写入，开启深度测试，避免褶皱的地方发生不透明重叠
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;

            float4 _TargetPosA;
            float4 _TargetPosB;
            float _ProximityRadius;

            float _FadeAlpha;

            float _FadeStartY;
            float _FadeEndY;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD1;
                float3 localPos : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.localPos = v.vertex.xyz;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float alphaFactor = 1.0;

                // ✅ 距离衰减（越靠近越透明）
                float dA = distance(i.worldPos, _TargetPosA.xyz);
                float dB = distance(i.worldPos, _TargetPosB.xyz);
                float proximity = min(dA, dB);

                // if (proximity < _ProximityRadius)
                // {
                //     float t = saturate(proximity / _ProximityRadius); // 越近越小
                //     t = pow(t, 20.0);                                   // 快速下降
                //     alphaFactor *= lerp(_FadeAlpha, 1.0, t);           // 越近越透明
                // }

                // ✅ 距离衰减：小于 1×半径全隐，大于 1.5×半径全显，中间渐变
                float inner = _ProximityRadius;
                float outer = _ProximityRadius * 1.2;

                float t = saturate(smoothstep(inner, outer, proximity));
                alphaFactor *= lerp(_FadeAlpha, 1.0, t);


                // ✅ Y轴渐隐（使用局部坐标）
                float y = i.localPos.y;
                if (y < _FadeStartY)
                {
                    float t = saturate((_FadeStartY - y) / (_FadeStartY - _FadeEndY));
                    alphaFactor *= lerp(1.0, _FadeAlpha, t);
                }

                fixed4 col = tex2D(_MainTex, i.uv) * _Color;
                col.a *= alphaFactor;
                return col;
            }
            ENDCG
        }
    }
}
