Shader "Custom/SynthwaveSun"
{
    Properties
    {
        [Header(Sun Disc)]
        [HDR] _SunColorTop     ("Sun Top",          Color) = (1.5, 1.2, 0.2, 1)
        [HDR] _SunColorBottom  ("Sun Bottom",       Color) = (1.5, 0.1, 0.5, 1)
        _SunRadius             ("Sun Radius",       Range(0.01, 0.5)) = 0.22
        _SunSharpness          ("Sun Edge Sharp",   Range(1, 200))    = 80

        [Header(Horizontal Slices)]
        _SliceCount            ("Slice Count",      Range(0, 20))  = 8
        _SliceStart            ("Slice Start Y",    Range(0, 1))   = 0.45
        _SliceGrowth           ("Slice Growth",     Range(0.5, 4)) = 1.8
        _SliceBaseWidth        ("Slice Min Width",  Range(0.001, 0.05)) = 0.006

        [Header(Glow)]
        [HDR] _GlowColor      ("Glow Color",       Color) = (1.0, 0.3, 0.6, 1)
        _GlowRadius            ("Glow Size",        Range(0, 1))   = 0.5
        _GlowIntensity         ("Glow Intensity",   Range(0, 10))  = 2.5
        _GlowFalloff           ("Glow Falloff",     Range(0.5, 8)) = 2.0

        [Header(Outer Ring Glow)]
        [HDR] _RingColor       ("Ring Color",       Color) = (1.0, 0.0, 0.6, 1)
        _RingWidth             ("Ring Width",        Range(0.001, 0.2)) = 0.02
        _RingSharpness         ("Ring Sharpness",    Range(1, 100)) = 30
        _RingIntensity         ("Ring Intensity",    Range(0, 10))  = 3.0

        [Header(Biome Fade)]
        _FadeAlpha             ("Master Fade",      Range(0, 1))   = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue"      = "Transparent"
            "IgnoreProjector" = "True"
        }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            half4 _SunColorTop, _SunColorBottom;
            half  _SunRadius, _SunSharpness;

            half  _SliceCount, _SliceStart, _SliceGrowth, _SliceBaseWidth;

            half4 _GlowColor;
            half  _GlowRadius, _GlowIntensity, _GlowFalloff;

            half4 _RingColor;
            half  _RingWidth, _RingSharpness, _RingIntensity;

            half  _FadeAlpha;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            half4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // sun centered on the quad
                float2 sunCenter = float2(0.5, 0.5);
                float2 delta     = uv - sunCenter;
                float  dist      = length(delta);

                // start fully transparent
                half3 col   = half3(0, 0, 0);
                half  alpha = 0.0h;
                
                half glowFactor = 1.0h - saturate(dist / _GlowRadius);
                glowFactor = pow(glowFactor, _GlowFalloff) * _GlowIntensity;
                glowFactor = saturate(glowFactor);

                col   += _GlowColor.rgb * glowFactor;
                alpha  = max(alpha, glowFactor);
                
                half ringDist   = abs(dist - _SunRadius);
                half ringFactor = saturate(1.0h - ringDist / _RingWidth);
                ringFactor      = pow(ringFactor, _RingSharpness) * _RingIntensity;
                ringFactor      = saturate(ringFactor);

                col   += _RingColor.rgb * ringFactor;
                alpha  = max(alpha, ringFactor);
                
                half sunMask = saturate((_SunRadius - dist) * _SunSharpness);

                // vertical gradient across the disc
                half sunGradientT = saturate((uv.y - (sunCenter.y - _SunRadius))
                                              / (_SunRadius * 2.0));
                half3 sunCol = lerp(_SunColorBottom.rgb, _SunColorTop.rgb, sunGradientT);
                
                half sliceMask = 0.0h;

                if (uv.y < sunCenter.y && sunMask > 0.001h)
                {
                    half belowT = saturate((sunCenter.y - uv.y) / _SunRadius);

                    // slices begin after _SliceStart
                    half sliceZone = smoothstep(_SliceStart - 0.1, _SliceStart, belowT);

                    // repeating bands that grow thicker
                    half lineY    = belowT * _SliceCount;
                    half lineIdx  = floor(lineY);
                    half lineFrac = frac(lineY);

                    half width = _SliceBaseWidth * pow(_SliceGrowth, lineIdx + 1.0);
                    width = min(width, 0.48);

                    half band = step(lineFrac, width) + step(1.0 - width, lineFrac);
                    band = saturate(band);

                    sliceMask = band * sliceZone;
                }

                // disc with slices cut out
                half finalSunMask = sunMask * (1.0h - sliceMask);

                // blend disc on top of glow
                col   = lerp(col, sunCol, finalSunMask);
                alpha = max(alpha, finalSunMask);
                
                alpha *= _FadeAlpha;

                // discard fully transparent pixels (saves fill rate on mobile)
                clip(alpha - 0.004h);

                return half4(col, alpha);
            }
            ENDCG
        }
    }

    FallBack Off
}