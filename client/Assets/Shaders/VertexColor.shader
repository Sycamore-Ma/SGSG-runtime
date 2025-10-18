Shader "Unlit/VertexColor"
{
    Properties
    {
        _PointSize("Point Size", Range(0.1, 50)) = 4.0
        _Alpha("Alpha", Range(0,1)) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="AlphaTest" "ForceNoShadowCasting"="True" }
        Blend SrcAlpha OneMinusSrcAlpha // 启用透明度混合
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            struct g2f
            {
                float4 pos : SV_POSITION;
                float4 color : COLOR;
            };

            float _PointSize;
            float _Alpha;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                return o;
            }

            // DX11 不支持 gl_PointSize
            // ✅ 让 DX11 渲染大点：用 Geometry Shader 生成 Quad
            [maxvertexcount(4)]
            void geom(point v2f input[1], inout TriangleStream<g2f> stream)
            {
                float size = _PointSize * 0.002;  // **转换为合适的屏幕单位**
                float4 center = input[0].pos;
                float4 color = input[0].color;
                color.a *= _Alpha; // 乘以透明度

                g2f p;

                p.color = color;

                p.pos = center + float4(-size, -size, 0, 0);
                stream.Append(p);

                p.pos = center + float4(size, -size, 0, 0);
                stream.Append(p);

                p.pos = center + float4(-size, size, 0, 0);
                stream.Append(p);

                p.pos = center + float4(size, size, 0, 0);
                stream.Append(p);
            }

            fixed4 frag(g2f i) : SV_Target
            {
                return i.color;  // 返回颜色，已包含透明度
            }
            ENDCG
        }
    }
}
