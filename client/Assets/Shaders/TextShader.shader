Shader "Custom/TextShader"
{

    Properties
    {
        _FaceColor ("Face Color", Color) = (1,1,1,1)
        _MainTex ("Font Atlas", 2D) = "white" {}
        _AlphaVisible ("Visible Alpha", Range(0,1)) = 1.0
        _AlphaHidden ("Hidden Alpha", Range(0,1)) = 0.15
        _OutlineSoftness ("Outline Softness", Range(0,1)) = 0.0
        _FaceDilate ("Face Dilate", Range(-1,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        // Visible
        Pass
        {
            Name "Visible"
            ZTest LEqual
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _FaceColor;
            float _AlphaVisible;
            float _OutlineSoftness;
            float _FaceDilate;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = tex2D(_MainTex, i.uv).a;
                float smoothing = 0.25 * fwidth(d) + _OutlineSoftness;
                float alpha = smoothstep(0.5 - smoothing - _FaceDilate, 0.5 + smoothing - _FaceDilate, d);

                alpha *= _AlphaVisible * _FaceColor.a;

                return float4(_FaceColor.rgb, alpha);
            }
            ENDCG
        }

        // Hidden
        Pass
        {
            Name "Hidden"
            ZTest Greater
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float4 _FaceColor;
            float _AlphaHidden;
            float _OutlineSoftness;
            float _FaceDilate;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float d = tex2D(_MainTex, i.uv).a;
                float smoothing = 0.25 * fwidth(d) + _OutlineSoftness;
                float alpha = smoothstep(0.5 - smoothing - _FaceDilate, 0.5 + smoothing - _FaceDilate, d);

                alpha *= _AlphaHidden * _FaceColor.a;

                return float4(_FaceColor.rgb, alpha);
            }
            ENDCG
        }
    }

    FallBack "TextMeshPro/Mobile/Distance Field"
}
