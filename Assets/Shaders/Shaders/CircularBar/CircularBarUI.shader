Shader "UI/CircularBar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Radius ("Radius", Range(0.1, 0.5)) = 0.4
        _Thickness ("Thickness", Range(0.01, 0.3)) = 0.1
        _Smoothness ("Edge Smoothness", Range(0.001, 0.05)) = 0.01

        _Fill ("Fill", Range(0, 1)) = 1

        _FillColor ("Fill Color", Color) = (0.2, 0.8, 0.2, 1)
        _BackgroundColor ("Background Color", Color) = (0.2, 0.2, 0.2, 0.5)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 0.8)
        _OutlineThickness ("Outline Thickness", Range(0, 0.03)) = 0.006

        _ClipRect ("Clip Rect", Vector) = (-10000,-10000,10000,10000)
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref 0
            Comp Always
            Pass Keep
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _Color;
            float4 _ClipRect;

            float _Radius;
            float _Thickness;
            float _Smoothness;
            float _Fill;
            float _OutlineThickness;

            float4 _FillColor;
            float4 _BackgroundColor;
            float4 _OutlineColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                return o;
            }

            #define PI 3.14159265
            #define TWO_PI 6.28318530

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv - 0.5;
                float dist = length(uv);

                float innerR = _Radius - _Thickness * 0.5;
                float outerR = _Radius + _Thickness * 0.5;

                float ringMask = smoothstep(innerR - _Smoothness, innerR, dist)
                               - smoothstep(outerR, outerR + _Smoothness, dist);

                float outlineInner = smoothstep(innerR - _OutlineThickness - _Smoothness, innerR - _OutlineThickness, dist);
                float outlineOuter = 1.0 - smoothstep(outerR + _OutlineThickness, outerR + _OutlineThickness + _Smoothness, dist);
                float outlineMask = outlineInner * outlineOuter - ringMask;
                outlineMask = saturate(outlineMask);

                float angle = atan2(uv.x, uv.y);
                float normalizedAngle = (angle + PI) / TWO_PI;

                float fillMask = step(1.0 - normalizedAngle, _Fill);

                float4 col = lerp(_BackgroundColor, _FillColor, fillMask);
                col = lerp(float4(0,0,0,0), col, ringMask);
                col = col + _OutlineColor * outlineMask * _OutlineColor.a;
                col.a = saturate(col.a);

                col *= i.color;

                #ifdef UNITY_UI_CLIP_RECT
                col.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                clip(col.a - 0.001);

                return col;
            }
            ENDCG
        }
    }
}
