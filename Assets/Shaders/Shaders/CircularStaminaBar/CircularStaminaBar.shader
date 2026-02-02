Shader "Unlit/CircularStaminaBar"
{
    Properties
    {
        [Header(Ring Settings)]
        _Radius ("Radius", Range(0.1, 0.5)) = 0.4
        _Thickness ("Thickness", Range(0.01, 0.2)) = 0.08
        _Smoothness ("Edge Smoothness", Range(0.001, 0.05)) = 0.01
        
        [Header(Segments)]
        _SegmentCount ("Segment Count", Range(1, 10)) = 3
        _SegmentGap ("Segment Gap", Range(0, 0.1)) = 0.02
        _SegmentValues ("Segment Values", Vector) = (1, 1, 1, 1)
        _SegmentValues2 ("Segment Values 2", Vector) = (1, 1, 1, 1)
        _RechargeProgress ("Recharge Progress", Range(0, 1)) = 0
        _RechargeSegment ("Recharge Segment Index", Range(0, 9)) = 0
        
        [Header(Use Effect)]
        _UseEffectValues ("Use Effect Values", Vector) = (1, 1, 1, 1)
        _UseEffectValues2 ("Use Effect Values 2", Vector) = (1, 1, 1, 1)
        
        [Header(Colors)]
        _ReadyColor ("Ready Color", Color) = (0.2, 0.8, 0.2, 1)
        _RechargeColor ("Recharge Fill Color", Color) = (0.8, 0.8, 0.2, 1)
        _RechargeBackgroundColor ("Recharge Background Color", Color) = (0.3, 0.3, 0.1, 0.8)
        _EmptyColor ("Empty Color", Color) = (0.2, 0.2, 0.2, 0.5)
        _UseEffectColor ("Use Effect Color", Color) = (1, 1, 1, 0.5)
        _OutlineColor ("Outline Color", Color) = (0, 0, 0, 0.8)
        
        [Header(Effects)]
        _PulseSpeed ("Pulse Speed", Range(0, 10)) = 3
        _PulseIntensity ("Pulse Intensity", Range(0, 1)) = 0.3
        _PulseActive ("Pulse Active", Range(0, 1)) = 0
        _GlowIntensity ("Glow Intensity", Range(0, 1)) = 0.2
        
        [Header(Outline)]
        _OutlineThickness ("Outline Thickness", Range(0, 0.02)) = 0.005
    }
    
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        
        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            
            float _Radius;
            float _Thickness;
            float _Smoothness;
            
            int _SegmentCount;
            float _SegmentGap;
            float4 _SegmentValues;
            float4 _SegmentValues2;
            float _RechargeProgress;
            int _RechargeSegment;
            
            float4 _UseEffectValues;
            float4 _UseEffectValues2;
            
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
            
            #define PI 3.14159265359
            #define TAU 6.28318530718
            
            float GetSegmentValue(int index)
            {
                if (index < 4)
                {
                    if (index == 0) return _SegmentValues.x;
                    if (index == 1) return _SegmentValues.y;
                    if (index == 2) return _SegmentValues.z;
                    return _SegmentValues.w;
                }
                else
                {
                    int i = index - 4;
                    if (i == 0) return _SegmentValues2.x;
                    if (i == 1) return _SegmentValues2.y;
                    if (i == 2) return _SegmentValues2.z;
                    return _SegmentValues2.w;
                }
            }
            
            float GetUseEffectValue(int index)
            {
                if (index < 4)
                {
                    if (index == 0) return _UseEffectValues.x;
                    if (index == 1) return _UseEffectValues.y;
                    if (index == 2) return _UseEffectValues.z;
                    return _UseEffectValues.w;
                }
                else
                {
                    int i = index - 4;
                    if (i == 0) return _UseEffectValues2.x;
                    if (i == 1) return _UseEffectValues2.y;
                    if (i == 2) return _UseEffectValues2.z;
                    return _UseEffectValues2.w;
                }
            }
            
            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv - 0.5;
                
                return o;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);
                
                float2 uv = i.uv;
                float dist = length(uv);
                float angle = atan2(uv.x, uv.y);
                float normalizedAngle = (angle + PI) / TAU;
                
                float innerRadius = _Radius - _Thickness * 0.5;
                float outerRadius = _Radius + _Thickness * 0.5;
                
                float ringMask = smoothstep(innerRadius - _Smoothness, innerRadius + _Smoothness, dist);
                ringMask *= 1.0 - smoothstep(outerRadius - _Smoothness, outerRadius + _Smoothness, dist);
                
                if (ringMask < 0.01)
                {
                    return float4(0, 0, 0, 0);
                }
                
                float outlineInner = smoothstep(innerRadius - _OutlineThickness - _Smoothness, innerRadius - _OutlineThickness, dist);
                float outlineOuter = 1.0 - smoothstep(outerRadius + _OutlineThickness, outerRadius + _OutlineThickness + _Smoothness, dist);
                float outlineMask = (outlineInner * outlineOuter) - ringMask;
                outlineMask = saturate(outlineMask);
                
                float segmentSize = 1.0 / _SegmentCount;
                float gapSize = _SegmentGap / TAU;
                
                int segmentIndex = floor(normalizedAngle / segmentSize);
                segmentIndex = clamp(segmentIndex, 0, _SegmentCount - 1);
                
                float segmentStart = segmentIndex * segmentSize;
                float segmentEnd = (segmentIndex + 1) * segmentSize;
                float segmentProgress = (normalizedAngle - segmentStart) / segmentSize;
                
                float gapMask = 1.0;
                if (_SegmentCount > 1)
                {
                    float distToStart = normalizedAngle - segmentStart;
                    float distToEnd = segmentEnd - normalizedAngle;
                    gapMask = smoothstep(0, gapSize, distToStart) * smoothstep(0, gapSize, distToEnd);
                }
                
                float segmentValue = GetSegmentValue(segmentIndex);
                float useEffectValue = GetUseEffectValue(segmentIndex);
                
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
                
                return color;
            }
            ENDCG
        }
    }
    
    FallBack "Sprites/Default"
}