Shader "Lpk/LightModel/ToonLightBase"
{
    Properties
    {
        [Enum(Opaque,0,Transparent,1)] _SurfaceType ("Surface Type", Float) = 0
        [Enum(UnityEngine.Rendering.BlendMode)] _SrcBlend ("Src Blend", Float) = 1
        [Enum(UnityEngine.Rendering.BlendMode)] _DstBlend ("Dst Blend", Float) = 0
        [Enum(Off,0,On,1)] _ZWrite ("ZWrite", Float) = 1

        _BaseMap            ("Texture", 2D)                       = "white" {}
        _BaseColor          ("Color", Color)                      = (0.5,0.5,0.5,1)
        _Alpha              ("Alpha", Range(0, 1))                = 1

        [Space]
        _EmissionMap        ("Emission Map", 2D)                  = "black" {}
        [HDR]_EmissionColor ("Emission Color", Color)             = (1,1,1,1)
        _EmissionStrength   ("Emission Strength", Range(0, 20))   = 0

        [Space]
        _ShadowStep         ("ShadowStep", Range(0, 1))           = 0.5
        _ShadowStepSmooth   ("ShadowStepSmooth", Range(0, 1))     = 0.04

        [Space]
        _SpecularStep       ("SpecularStep", Range(0, 1))         = 0.6
        _SpecularStepSmooth ("SpecularStepSmooth", Range(0, 1))   = 0.05
        [HDR]_SpecularColor ("SpecularColor", Color)              = (1,1,1,1)

        [Space]
        _RimStep            ("RimStep", Range(0, 1))              = 0.65
        _RimStepSmooth      ("RimStepSmooth",Range(0,1))          = 0.4
        _RimColor           ("RimColor", Color)                   = (1,1,1,1)

        [Space]
        [Toggle(_OUTLINE_ON)] _OutlineEnabled ("Enable Outline", Float) = 1
        _OutlineWidth       ("OutlineWidth", Range(0.0, 1.0))     = 0.15
        _OutlineColor       ("OutlineColor", Color)               = (0.0, 0.0, 0.0, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "ForwardBase" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fwdbase
            #pragma multi_compile_instancing
            #pragma shader_feature_local _SURFACE_TRANSPARENT

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"

            sampler2D _BaseMap;
            sampler2D _EmissionMap;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _Alpha)
                UNITY_DEFINE_INSTANCED_PROP(float4, _EmissionColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _EmissionStrength)
                UNITY_DEFINE_INSTANCED_PROP(float, _ShadowStep)
                UNITY_DEFINE_INSTANCED_PROP(float, _ShadowStepSmooth)
                UNITY_DEFINE_INSTANCED_PROP(float, _SpecularStep)
                UNITY_DEFINE_INSTANCED_PROP(float, _SpecularStepSmooth)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SpecularColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _RimStep)
                UNITY_DEFINE_INSTANCED_PROP(float, _RimStepSmooth)
                UNITY_DEFINE_INSTANCED_PROP(float4, _RimColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float2 uv       : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos          : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
                float3 positionWS   : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                SHADOW_COORDS(5)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.viewDirWS = _WorldSpaceCameraPos.xyz - o.positionWS;
                o.uv = v.uv;

                UNITY_TRANSFER_FOG(o, o.pos);
                TRANSFER_SHADOW(o);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                float alpha = UNITY_ACCESS_INSTANCED_PROP(Props, _Alpha);
                float4 emissionColor = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionColor);
                float emissionStrength = UNITY_ACCESS_INSTANCED_PROP(Props, _EmissionStrength);
                float shadowStep = UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowStep);
                float shadowStepSmooth = UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowStepSmooth);
                float specularStep = UNITY_ACCESS_INSTANCED_PROP(Props, _SpecularStep);
                float specularStepSmooth = UNITY_ACCESS_INSTANCED_PROP(Props, _SpecularStepSmooth);
                float4 specularColor = UNITY_ACCESS_INSTANCED_PROP(Props, _SpecularColor);
                float rimStep = UNITY_ACCESS_INSTANCED_PROP(Props, _RimStep);
                float rimStepSmooth = UNITY_ACCESS_INSTANCED_PROP(Props, _RimStepSmooth);
                float4 rimColor = UNITY_ACCESS_INSTANCED_PROP(Props, _RimColor);

                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewDirWS);
                float3 L = normalize(_WorldSpaceLightPos0.xyz);
                float3 H = normalize(V + L);

                float NV = dot(N, V);
                float NH = dot(N, H);
                float NL = dot(N, L);

                NL = NL * 0.5 + 0.5;

                float4 baseMap = tex2D(_BaseMap, i.uv);

                float finalAlpha = baseMap.a * baseColor.a * alpha;

                float shadow = SHADOW_ATTENUATION(i);

                float shadowNL = smoothstep(shadowStep - shadowStepSmooth,
                                            shadowStep + shadowStepSmooth, NL);

                float specularNH = smoothstep((1 - specularStep * 0.05) - specularStepSmooth * 0.05,
                                              (1 - specularStep * 0.05) + specularStepSmooth * 0.05, NH);

                float rim = smoothstep((1 - rimStep) - rimStepSmooth * 0.5,
                                       (1 - rimStep) + rimStepSmooth * 0.5, 0.5 - NV);

                float3 ambient = ShadeSH9(float4(N, 1.0)) * baseColor.rgb * baseMap.rgb;

                float3 diffuse = _LightColor0.rgb * baseMap.rgb * baseColor.rgb * shadowNL * shadow;
                float3 specular = specularColor.rgb * shadow * shadowNL * specularNH;
                float3 rimFinal = rim * rimColor.rgb;

                float3 emissionTex = tex2D(_EmissionMap, i.uv).rgb;
                float3 emission = emissionTex * emissionColor.rgb * emissionStrength;

                float3 finalColor = diffuse + ambient + rimFinal + specular + emission;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return float4(finalColor, finalAlpha);
            }
            ENDCG
        }

        Pass
        {
            Name "ForwardAdd"
            Tags { "LightMode" = "ForwardAdd" }

            Blend One One
            ZWrite Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma multi_compile_fwdadd_fullshadows
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "AutoLight.cginc"
            #include "Lighting.cginc"

            sampler2D _BaseMap;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _Alpha)
                UNITY_DEFINE_INSTANCED_PROP(float, _ShadowStep)
                UNITY_DEFINE_INSTANCED_PROP(float, _ShadowStepSmooth)
                UNITY_DEFINE_INSTANCED_PROP(float, _SpecularStep)
                UNITY_DEFINE_INSTANCED_PROP(float, _SpecularStepSmooth)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SpecularColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct appdata
            {
                float4 vertex   : POSITION;
                float3 normal   : NORMAL;
                float2 uv       : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos          : SV_POSITION;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 viewDirWS    : TEXCOORD2;
                float3 positionWS   : TEXCOORD3;
                UNITY_FOG_COORDS(4)
                SHADOW_COORDS(5)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.pos = UnityObjectToClipPos(v.vertex);
                o.positionWS = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.normalWS = UnityObjectToWorldNormal(v.normal);
                o.viewDirWS = _WorldSpaceCameraPos.xyz - o.positionWS;
                o.uv = v.uv;

                UNITY_TRANSFER_FOG(o, o.pos);
                TRANSFER_SHADOW(o);

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                float alpha = UNITY_ACCESS_INSTANCED_PROP(Props, _Alpha);
                float shadowStep = UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowStep);
                float shadowStepSmooth = UNITY_ACCESS_INSTANCED_PROP(Props, _ShadowStepSmooth);
                float specularStep = UNITY_ACCESS_INSTANCED_PROP(Props, _SpecularStep);
                float specularStepSmooth = UNITY_ACCESS_INSTANCED_PROP(Props, _SpecularStepSmooth);
                float4 specularColor = UNITY_ACCESS_INSTANCED_PROP(Props, _SpecularColor);

                float3 N = normalize(i.normalWS);
                float3 V = normalize(i.viewDirWS);

                float3 lightDir;
                float attenuation;

                #if defined(POINT) || defined(SPOT) || defined(POINT_COOKIE)
                    float3 toLight = _WorldSpaceLightPos0.xyz - i.positionWS;
                    lightDir = normalize(toLight);
                    float distSqr = dot(toLight, toLight);
                    attenuation = 1.0 / (1.0 + distSqr * unity_4LightAtten0.x);
                #else
                    lightDir = normalize(_WorldSpaceLightPos0.xyz);
                    attenuation = 1.0;
                #endif

                float3 H = normalize(V + lightDir);

                float NL = dot(N, lightDir) * 0.5 + 0.5;
                float NH = dot(N, H);

                float shadow = SHADOW_ATTENUATION(i);

                float4 baseMap = tex2D(_BaseMap, i.uv);

                float finalAlpha = baseMap.a * baseColor.a * alpha;

                float shadowNL = smoothstep(shadowStep - shadowStepSmooth,
                                            shadowStep + shadowStepSmooth, NL);

                float specularNH = smoothstep((1 - specularStep * 0.05) - specularStepSmooth * 0.05,
                                              (1 - specularStep * 0.05) + specularStepSmooth * 0.05, NH);

                float3 diffuse = _LightColor0.rgb * baseMap.rgb * baseColor.rgb * shadowNL * shadow * attenuation;
                float3 specular = specularColor.rgb * shadow * shadowNL * specularNH * attenuation;

                float3 finalColor = diffuse + specular;

                UNITY_APPLY_FOG(i.fogCoord, finalColor);

                return float4(finalColor, finalAlpha);
            }
            ENDCG
        }

        Pass
        {
            Name "Outline"
            Cull Front
            Tags { "LightMode" = "Always" }

            Blend [_SrcBlend] [_DstBlend]
            ZWrite [_ZWrite]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #pragma shader_feature_local _OUTLINE_ON
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                UNITY_FOG_COORDS(0)
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            float _OutlineWidth;
            float4 _OutlineColor;

            UNITY_INSTANCING_BUFFER_START(OutlineProps)
                UNITY_DEFINE_INSTANCED_PROP(float, _Alpha)
            UNITY_INSTANCING_BUFFER_END(OutlineProps)

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_INITIALIZE_OUTPUT(v2f, o);

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                #ifdef _OUTLINE_ON
                    float3 expandedPos = v.vertex.xyz + v.normal * _OutlineWidth * 0.1;
                    o.pos = UnityObjectToClipPos(float4(expandedPos, 1));
                    UNITY_TRANSFER_FOG(o, o.pos);
                #else
                    o.pos = float4(0, 0, 0, 0);
                #endif

                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                #ifdef _OUTLINE_ON
                    float alpha = UNITY_ACCESS_INSTANCED_PROP(OutlineProps, _Alpha);
                    float3 finalColor = _OutlineColor.rgb;
                    UNITY_APPLY_FOG(i.fogCoord, finalColor);
                    return float4(finalColor, _OutlineColor.a * alpha);
                #else
                    discard;
                    return float4(0, 0, 0, 0);
                #endif
            }
            ENDCG
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_shadowcaster
            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            sampler2D _BaseMap;

            UNITY_INSTANCING_BUFFER_START(ShadowProps)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _Alpha)
            UNITY_INSTANCING_BUFFER_END(ShadowProps)

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                V2F_SHADOW_CASTER;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                o.uv = v.uv;
                TRANSFER_SHADOW_CASTER_NORMALOFFSET(o);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(i);

                float4 baseColor = UNITY_ACCESS_INSTANCED_PROP(ShadowProps, _BaseColor);
                float alpha = UNITY_ACCESS_INSTANCED_PROP(ShadowProps, _Alpha);
                float texAlpha = tex2D(_BaseMap, i.uv).a;
                float finalAlpha = texAlpha * baseColor.a * alpha;

                clip(finalAlpha - 0.5);

                SHADOW_CASTER_FRAGMENT(i);
            }
            ENDCG
        }
    }

    Fallback "Diffuse"

    CustomEditor "ToonLightBaseInspector"
}