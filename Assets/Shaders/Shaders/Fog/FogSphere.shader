Shader "Runner/FogSphere"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.7, 0.8, 0.9, 1.0)
        _FogStart ("Fog Start Distance", Float) = 10.0
        _FogEnd ("Fog End Distance", Float) = 100.0
        _FogDensity ("Fog Density", Float) = 1.0
        _FogHeightFalloff ("Height Falloff", Float) = 0.1
        _FogBaseHeight ("Base Height", Float) = 0.0
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+100"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Front

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };

            fixed4 _FogColor;
            float _FogStart;
            float _FogEnd;
            float _FogDensity;
            float _FogHeightFalloff;
            float _FogBaseHeight;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.viewDir = o.worldPos - _WorldSpaceCameraPos.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float dist = length(i.viewDir);

                // Distance fog factor
                float distFog = saturate((dist - _FogStart) / max(_FogEnd - _FogStart, 0.001));

                // Smooth curve
                distFog = distFog * distFog;

                // Height fog
                float heightDiff = i.worldPos.y - _FogBaseHeight;
                float heightFog = exp(-max(heightDiff, 0.0) * _FogHeightFalloff);

                // Below base height = full fog
                float belowFactor = saturate((_FogBaseHeight - i.worldPos.y) * 0.1);
                heightFog = max(heightFog, belowFactor);

                // Combine
                float fogAlpha = distFog * heightFog * _FogDensity;
                fogAlpha = saturate(fogAlpha);

                return fixed4(_FogColor.rgb, fogAlpha);
            }
            ENDCG
        }
    }
    FallBack Off
}