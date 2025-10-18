Shader "Custom/UnlitCross"
{
    Properties
    {
        _Color("Cross Color", Color) = (1, 0, 0, 0.5)   // 默认半透明红色
        _Thickness("Line Thickness", Range(0.001, 0.2)) = 0.05
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

            fixed4 _Color;
            float _Thickness;

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
                float2 uv = i.uv - 0.5;

                // X 斜线：y = x 和 y = -x
                float d1 = abs(uv.y - uv.x);
                float d2 = abs(uv.y + uv.x);

                float minDist = min(d1, d2);

                if (minDist > _Thickness)
                    discard;

                return _Color;
            }
            ENDCG
        }
    }
}
