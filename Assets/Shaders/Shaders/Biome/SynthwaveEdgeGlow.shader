Shader "Custom/SynthwaveEdgeGlow"
{
    Properties
    {
        [Header(Colors)]
        [HDR] _EdgeColor1    ("Edge Color Low",    Color)         = (1.0, 0.0, 0.6, 1)
        [HDR] _EdgeColor2    ("Edge Color High",   Color)         = (0.0, 0.8, 1.0, 1)
        _FaceColor           ("Face Color",        Color)         = (0.02, 0.0, 0.05, 1)

        [Header(Wire)]
        _WireWidth           ("Wire Width  (px)",  Range(0.2, 6)) = 1.4
        _GlowWidth           ("Glow Spread (px)",  Range(0.5,40)) = 8.0
        _GlowIntensity       ("Glow Intensity",    Range(0, 20))  = 5.0

        [Header(Height Gradient)]
        _GradientScale       ("Gradient Scale",    Float)         = 1.0
        _GradientOffset      ("Gradient Offset",   Float)         = 0.0

        [Header(Animation)]
        [Toggle(_PULSE)] _Pulse ("Enable Pulse",   Float)        = 1
        _PulseSpeed          ("Pulse Speed",        Range(0, 10)) = 2.0
        _PulseMin            ("Pulse Min",          Range(0, 1))  = 0.55

        [Header(Scanlines)]
        [Toggle(_SCANLINES)] _Scanlines ("Scanlines", Float)     = 0
        _ScanDensity         ("Density",            Range(1,300)) = 100
        _ScanSpeed           ("Scroll Speed",       Range(0, 10))= 1.5
        _ScanStrength        ("Strength",           Range(0, 1)) = 0.12

        [Header(Face Inner Glow)]
        _InnerGlow           ("Face Emission",      Range(0, 0.5))= 0.03
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 200

        Pass
        {
            Name "SYNTHWAVE_WIRE"
            Tags { "LightMode" = "Always" }
            Cull Back
            ZWrite On

            CGPROGRAM
            #pragma vertex   vert
            #pragma geometry geom
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature_local _PULSE
            #pragma shader_feature_local _SCANLINES
            #pragma target 4.0

            #include "UnityCG.cginc"

            /* ─────────────── properties ─────────────────── */
            half4 _EdgeColor1;
            half4 _EdgeColor2;
            half4 _FaceColor;
            half  _WireWidth;
            half  _GlowWidth;
            half  _GlowIntensity;
            half  _GradientScale;
            half  _GradientOffset;
            half  _PulseSpeed;
            half  _PulseMin;
            half  _ScanDensity;
            half  _ScanSpeed;
            half  _ScanStrength;
            half  _InnerGlow;

            /* ─────────────── structures ─────────────────── */

            // vertex → geometry
            struct v2g
            {
                float4 clipPos  : SV_POSITION;
                float4 objPos   : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                half3  worldN   : TEXCOORD2;
                float  objY     : TEXCOORD3;
                float  fogCoord : TEXCOORD4;   // manual fog
            };

            // geometry → fragment
            struct g2f
            {
                float4 pos      : SV_POSITION;
                float3 edgeDist : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
                half3  worldN   : TEXCOORD2;
                float  objY     : TEXCOORD3;
                float  fogCoord : TEXCOORD4;   // manual fog
                UNITY_VERTEX_OUTPUT_STEREO
            };

            /* ─────────────── vertex ─────────────────────── */
            v2g vert(appdata_base v)
            {
                v2g o;
                UNITY_SETUP_INSTANCE_ID(v);

                o.clipPos  = UnityObjectToClipPos(v.vertex);
                o.objPos   = v.vertex;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldN   = UnityObjectToWorldNormal(v.normal);
                o.objY     = v.vertex.y;

                // compute fog factor once per vertex
                UNITY_CALC_FOG_FACTOR_RAW(o.clipPos.z);
                o.fogCoord = unityFogFactor;

                return o;
            }

            /* ─────────────── geometry ───────────────────── */
            [maxvertexcount(3)]
            void geom(triangle v2g inp[3],
                      inout TriangleStream<g2f> stream)
            {
                float4 cp0 = inp[0].clipPos;
                float4 cp1 = inp[1].clipPos;
                float4 cp2 = inp[2].clipPos;

                // NDC → screen pixels
                float2 sp0 = (cp0.xy / cp0.w) * _ScreenParams.xy * 0.5;
                float2 sp1 = (cp1.xy / cp1.w) * _ScreenParams.xy * 0.5;
                float2 sp2 = (cp2.xy / cp2.w) * _ScreenParams.xy * 0.5;

                // triangle area × 2
                float area2 = abs((sp1.x - sp0.x) * (sp2.y - sp0.y)
                                - (sp2.x - sp0.x) * (sp1.y - sp0.y));

                // perpendicular height from each vertex to opposite edge
                float h0 = area2 / (length(sp1 - sp2) + 0.0001);
                float h1 = area2 / (length(sp2 - sp0) + 0.0001);
                float h2 = area2 / (length(sp0 - sp1) + 0.0001);

                g2f o;
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                // vertex 0
                o.pos      = cp0;
                o.edgeDist = float3(h0, 0.0, 0.0);
                o.worldPos = inp[0].worldPos;
                o.worldN   = inp[0].worldN;
                o.objY     = inp[0].objY;
                o.fogCoord = inp[0].fogCoord;
                stream.Append(o);

                // vertex 1
                o.pos      = cp1;
                o.edgeDist = float3(0.0, h1, 0.0);
                o.worldPos = inp[1].worldPos;
                o.worldN   = inp[1].worldN;
                o.objY     = inp[1].objY;
                o.fogCoord = inp[1].fogCoord;
                stream.Append(o);

                // vertex 2
                o.pos      = cp2;
                o.edgeDist = float3(0.0, 0.0, h2);
                o.worldPos = inp[2].worldPos;
                o.worldN   = inp[2].worldN;
                o.objY     = inp[2].objY;
                o.fogCoord = inp[2].fogCoord;
                stream.Append(o);

                stream.RestartStrip();
            }

            /* ─────────────── fragment ───────────────────── */
            half4 frag(g2f i) : SV_Target
            {
                // closest edge distance in pixels
                float d = min(i.edgeDist.x, min(i.edgeDist.y, i.edgeDist.z));

                // hard wire core
                half wire = 1.0h - smoothstep(0.0, _WireWidth, d);

                // soft glow falloff
                half glow = exp2(-d / max(_GlowWidth, 0.01)) * _GlowIntensity;

                // height gradient color
                half  t       = saturate(i.objY * _GradientScale + _GradientOffset);
                half3 edgeRGB = lerp(_EdgeColor1.rgb, _EdgeColor2.rgb, t);

                // pulse
                #ifdef _PULSE
                    half pulse = lerp(_PulseMin, 1.0h,
                                      sin(_Time.y * _PulseSpeed) * 0.5h + 0.5h);
                    wire *= pulse;
                    glow *= pulse;
                #endif

                // compose: face + glow halo + solid wire
                half3 face = _FaceColor.rgb + edgeRGB * _InnerGlow;
                half3 col  = face;
                col += edgeRGB * saturate(glow);    // additive glow
                col  = lerp(col, edgeRGB, wire);    // solid wire on top

                // scanlines
                #ifdef _SCANLINES
                    half sy   = i.pos.y / _ScreenParams.y;
                    half scan = sin((sy + _Time.y * _ScanSpeed) *
                                     _ScanDensity * UNITY_PI) * 0.5h + 0.5h;
                    col *= 1.0h - _ScanStrength * scan;
                #endif

                half4 c = half4(col, 1.0h);

                // apply fog manually using the interpolated factor
                #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
                    c.rgb = lerp(unity_FogColor.rgb, c.rgb, saturate(i.fogCoord));
                #endif

                return c;
            }
            ENDCG
        }
    }

    FallBack "Mobile/VertexLit"
}