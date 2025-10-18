Shader "Custom/EdgeShader"
{
    Properties
    {
        _Color ("Line Color", Color) = (1,1,1,1)
        _AlphaVisible ("Visible Alpha", Range(0,1)) = 0.3
        _AlphaHidden ("Hidden Alpha", Range(0,1)) = 0.1
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        Lighting Off

        // Pass for visible part
        Pass
        {
            Name "Visible"
            ZTest LEqual // 被摄像机看到的部分（不被遮挡）
            ColorMask RGB
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 _Color;
            float _AlphaVisible;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(_Color.rgb, _AlphaVisible);
            }
            ENDCG
        }

        // Pass for hidden part
        Pass
        {
            Name "Hidden"
            ZTest Greater // 被遮挡的部分
            ColorMask RGB
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            fixed4 _Color;
            float _AlphaHidden;

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 pos : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(_Color.rgb, _AlphaHidden);
            }
            ENDCG
        }
    }

    FallBack "Unlit/Transparent"
}
