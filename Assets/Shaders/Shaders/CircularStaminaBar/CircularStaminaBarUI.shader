Shader "UI/CircularStaminaBar"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        _Radius ("Radius", Range(0.1, 0.5)) = 0.4
        _Thickness ("Thickness", Range(0.01, 0.3)) = 0.08
        _Smoothness ("Edge Smoothness", Range(0.001, 0.05)) = 0.01

        _SegmentCount ("Segment Count", Range(1, 10)) = 3
        _SegmentGap ("Segment Gap (UV units)", Range(0, 0.45)) = 0.08

        _SegmentValues ("Segment Values", Vector) = (1, 1, 1, 1)
        _SegmentValues2 ("Segment Values 2", Vector) = (1, 1, 1, 1)
        _SegmentValues3 ("Segment Values 3", Vector) = (1, 1, 1, 1)

        _UseEffectValues ("Use Effect Values", Vector) = (1, 1, 1, 1)
        _UseEffectValues2 ("Use Effect Values 2", Vector) = (1, 1, 1, 1)
        _UseEffectValues3 ("Use Effect Values 3", Vector) = (1, 1, 1, 1)

        _RechargeProgress ("Recharge Progress", Range(0, 1)) = 0
        _RechargeSegment ("Recharge Segment Index", Range(0, 9)) = 0

        _ReadyColor ("Ready Color", Color) = (0.2, 0.8, 0.2, 1)
        _RechargeColor ("Recharge Fill Color", Color) = (0.8, 0.8, 0.2, 1)
        _RechargeBackgroundColor ("Recharge Background Color", Color) = (0.3, 0.3, 0.1, 0.8)
        _EmptyColor ("Empty Color", Color) = (0.2, 0.2, 0.2, 0.5)
        _UseEffectColor ("Use Effect Color", Color) = (1, 1, 1, 0.5)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 0.8)

        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.3
        _PulseActive ("Pulse Active", Range(0, 1)) = 0
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.2

        _OutlineThickness ("Outline Thickness", Range(0, 0.03)) = 0.008

        _ClipRect ("Clip Rect", Vector) = (-10000,-10000,10000,10000)
        _UIMaskSoftnessX ("Mask Softness X", Float) = 0
        _UIMaskSoftnessY ("Mask Softness Y", Float) = 0
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
            Ref 1
            Comp Always
            Pass Keep
        }

        Cull Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata_t
            {
                float4 vertex : POSITION;
                float4 color  : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _Radius;
            float _Thickness;
            float _Smoothness;

            int _SegmentCount;
            float _SegmentGap;

            float4 _SegmentValues;
            float4 _SegmentValues2;
            float4 _SegmentValues3;

            float4 _UseEffectValues;
            float4 _UseEffectValues2;
            float4 _UseEffectValues3;

            float _RechargeProgress;
            int _RechargeSegment;

            float4 _ReadyColor;
            float4 _RechargeColor;
            float4 _RechargeBackgroundColor;
            float4 _EmptyColor;
            float4 _UseEffectColor;
            float4 _OutlineColor;

            float _PulseSpeed;
            float _PulseIntensity;
            float _PulseActive;
            float _GlowIntensity;
            float _OutlineThickness;

            float4 _ClipRect;

            #define PI 3.14159265359
            #define TAU 6.28318530718

            float GetValue10(int index, float4 v1, float4 v2, float4 v3)
            {
                if (index < 4)
                {
                    if (index == 0) return v1.x;
                    if (index == 1) return v1.y;
                    if (index == 2) return v1.z;
                    return v1.w;
                }
                if (index < 8)
                {
                    int i = index - 4;
                    if (i == 0) return v2.x;
                    if (i == 1) return v2.y;
                    if (i == 2) return v2.z;
                    return v2.w;
                }
                int j = index - 8;
                if (j == 0) return v3.x;
                if (j == 1) return v3.y;
                if (j == 2) return v3.z;
                return v3.w;
            }

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                #ifdef UNITY_UI_CLIP_RECT
                i.color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                float2 uv = i.uv - 0.5;
                float dist = length(uv);
                float angle = atan2(uv.x, uv.y);
                float normalizedAngle = (angle + PI) / TAU;

                float innerRadius = _Radius - _Thickness * 0.5;
                float outerRadius = _Radius + _Thickness * 0.5;

                float ringMask = smoothstep(innerRadius - _Smoothness, innerRadius + _Smoothness, dist);
                ringMask *= 1.0 - smoothstep(outerRadius - _Smoothness, outerRadius + _Smoothness, dist);

                if (ringMask < 0.01)
                    return 0;

                float outlineInner = smoothstep(innerRadius - _OutlineThickness - _Smoothness, innerRadius - _OutlineThickness, dist);
                float outlineOuter = 1.0 - smoothstep(outerRadius + _OutlineThickness, outerRadius + _OutlineThickness + _Smoothness, dist);
                float outlineMask = (outlineInner * outlineOuter) - ringMask;
                outlineMask = saturate(outlineMask);

                int segCount = max(1, _SegmentCount);
                float segmentSize = 1.0 / segCount;

                int segmentIndex = (int)floor(normalizedAngle / segmentSize);
                segmentIndex = clamp(segmentIndex, 0, segCount - 1);

                float segmentStart = segmentIndex * segmentSize;
                float segmentEnd = (segmentIndex + 1) * segmentSize;
                float segmentProgress = (normalizedAngle - segmentStart) / segmentSize;

                float gapMask = 1.0;
                if (segCount > 1 && _SegmentGap > 0.0)
                {
                    float thetaStart = segmentStart * TAU - PI;
                    float thetaEnd = segmentEnd * TAU - PI;

                    float2 d0 = float2(sin(thetaStart), cos(thetaStart));
                    float2 d1 = float2(sin(thetaEnd), cos(thetaEnd));

                    float distToStart = abs(d0.x * uv.y - d0.y * uv.x);
                    float distToEnd = abs(d1.x * uv.y - d1.y * uv.x);

                    float maxGap = dist * sin(segmentSize * TAU * 0.5) * 0.98;
                    float gap = min(_SegmentGap, maxGap);

                    gapMask = step(gap, distToStart) * step(gap, distToEnd);
                }

                float segmentValue = GetValue10(segmentIndex, _SegmentValues, _SegmentValues2, _SegmentValues3);
                float useEffectValue = GetValue10(segmentIndex, _UseEffectValues, _UseEffectValues2, _UseEffectValues3);

                float4 color = _EmptyColor;

                float useEffectMask = step(segmentProgress, useEffectValue);
                color = lerp(color, _UseEffectColor, useEffectMask * (1.0 - segmentValue));

                if (segmentValue >= 1.0)
                {
                    color = _ReadyColor;
                }
                else if (segmentValue > 0.0)
                {
                    color = _RechargeBackgroundColor;

                    if (segmentIndex == _RechargeSegment)
                    {
                        float rechargeMask = step(segmentProgress, _RechargeProgress);
                        color = lerp(color, _RechargeColor, rechargeMask);
                    }
                }

                float pulse = 1.0;
                if (_PulseActive > 0.5)
                {
                    pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseIntensity;
                    color.rgb *= pulse;
                    color.a *= 0.7 + sin(_Time.y * _PulseSpeed * 0.5) * 0.3;
                }

                if (segmentValue >= 1.0)
                {
                    float glow = 1.0 + _GlowIntensity * (0.5 + 0.5 * sin(_Time.y * 2.0));
                    color.rgb *= glow;
                }

                color.a *= ringMask * gapMask;

                float4 outlineColor = _OutlineColor;
                outlineColor.a *= outlineMask * gapMask;

                color.rgb = lerp(color.rgb, outlineColor.rgb, outlineMask * gapMask * outlineColor.a);
                color.a = max(color.a, outlineColor.a * gapMask);

                color *= i.color;

                #ifdef UNITY_UI_ALPHACLIP
                clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}