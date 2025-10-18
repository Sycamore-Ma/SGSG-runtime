Shader "Custom/UnlitRing"
{
    Properties
    {
        _OuterColor("Outer Ring Color", Color) = (0, 0.6, 0, 1)
        _InnerColor("Inner Ring Color", Color) = (0, 0.6, 0, 0.4)
        _Thickness("Ring Thickness", Range(0.0, 1.0)) = 0.3
        _InnerRatio("Inner Ring Ratio", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            fixed4 _OuterColor;
            fixed4 _InnerColor;
            float _Thickness;
            float _InnerRatio;

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

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 centeredUV = i.uv - 0.5;
                float dist = length(centeredUV);

                float outer = 0.5;
                float inner = outer - _Thickness;
                float middle = inner + (outer - inner) * _InnerRatio;

                // 完全透明区域
                if (dist < inner || dist > outer)
                    discard;

                // 内环区域
                if (dist < middle)
                    return _InnerColor;

                // 外环区域
                return _OuterColor;
            }
            ENDCG
        }
    }
}
