Shader "Custom/VerticalFade"
{
    Properties
    {
        _Color ("Main Color", Color) = (1,1,1,0.2)
        _MainTex ("Texture", 2D) = "white" {}
        _FadeStart ("Fade Start Y (Relative)", Float) = 1.5
        _FadeEnd ("Fade End Y (Relative)", Float) = 0.75
        _BaseY ("Base Y (World)", Float) = -2.2
    }

    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        LOD 200
        Cull Off
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            fixed4 _Color;
            float _FadeStart;
            float _FadeEnd;
            float _BaseY;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 pos : SV_POSITION;
                float worldY : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.worldY = worldPos.y;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;

                float relativeY = i.worldY - _BaseY;
                float t = saturate((relativeY - _FadeEnd) / (_FadeStart - _FadeEnd));
                texColor.a *= t;

                return texColor;
            }
            ENDCG
        }
    }
}



// Shader "Custom/VerticalFade"
// {
//     Properties
//     {
//         _Color ("Main Color", Color) = (1,1,1,0.5)
//         _MainTex ("Texture", 2D) = "white" {}
//         _FadeStart ("Fade Start Y", Float) = 1.5
//         _FadeEnd ("Fade End Y", Float) = 0.75
//     }

//     SubShader
//     {
//         Tags {"Queue"="Transparent" "RenderType"="Transparent"}
//         LOD 200
//         Cull Off
//         Blend SrcAlpha OneMinusSrcAlpha
//         ZWrite Off

//         Pass
//         {
//             CGPROGRAM
//             #pragma vertex vert
//             #pragma fragment frag
//             #include "UnityCG.cginc"

//             sampler2D _MainTex;
//             fixed4 _Color;
//             float _FadeStart;
//             float _FadeEnd;

//             struct appdata
//             {
//                 float4 vertex : POSITION;
//                 float2 uv : TEXCOORD0;
//             };

//             struct v2f
//             {
//                 float2 uv : TEXCOORD0;
//                 float4 pos : SV_POSITION;
//                 float localY : TEXCOORD1;
//             };

//             v2f vert (appdata v)
//             {
//                 v2f o;
//                 o.pos = UnityObjectToClipPos(v.vertex);
//                 o.uv = v.uv;
//                 o.localY = v.vertex.y; // ✅ 使用 localY 而非 worldY
//                 return o;
//             }

//             fixed4 frag (v2f i) : SV_Target
//             {
//                 fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;

//                 // ✅ 使用 localY 计算透明度
//                 float t = saturate((i.localY - _FadeEnd) / (_FadeStart - _FadeEnd));
//                 texColor.a *= t;

//                 return texColor;
//             }

// //             struct v2f
// //             {
// //                 float2 uv : TEXCOORD0;
// //                 float4 pos : SV_POSITION;
// //                 float worldY : TEXCOORD1;
// //             };

// //             v2f vert (appdata v)
// //             {
// //                 v2f o;
// //                 float4 worldPos = mul(unity_ObjectToWorld, v.vertex);
// //                 o.pos = UnityObjectToClipPos(v.vertex);
// //                 o.uv = v.uv;
// //                 o.worldY = worldPos.y;
// //                 return o;
// //             }

// //             fixed4 frag (v2f i) : SV_Target
// //             {
// //                 fixed4 texColor = tex2D(_MainTex, i.uv) * _Color;

// //                 // 计算透明度因子
// //                 float t = saturate((i.worldY - _FadeEnd) / (_FadeStart - _FadeEnd));
// //                 texColor.a *= t;

// //                 return texColor;
// //             }

//             ENDCG
//         }
//     }
// }

