Shader "Hidden/CustomFog"
{
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
        }
        
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            Name "CustomFog"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float4 _FogColor;
            float _FogStart;
            float _FogEnd;
            float _FogDensity;
            float _FogHeightFalloff;
            float _FogBaseHeight;

            float4 Frag(Varyings input) : SV_Target
            {
                float4 sceneColor = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, input.texcoord);

                float rawDepth = SampleSceneDepth(input.texcoord);

                #if UNITY_REVERSED_Z
                    bool isSky = rawDepth <= 0.0001;
                #else
                    bool isSky = rawDepth >= 0.9999;
                #endif

                if (isSky)
                {
                    return sceneColor;
                }

                float linearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);

                float2 posNDC = input.texcoord * 2.0 - 1.0;
                
                #if UNITY_UV_STARTS_AT_TOP
                    posNDC.y = -posNDC.y;
                #endif

                float4 posCS = float4(posNDC, rawDepth, 1.0);
                float4 posWS = mul(UNITY_MATRIX_I_VP, posCS);
                float3 worldPos = posWS.xyz / posWS.w;

                float distanceFog = saturate((linearDepth - _FogStart) / max(_FogEnd - _FogStart, 0.001));

                float heightFactor = 1.0;
                if (_FogHeightFalloff > 0.001)
                {
                    heightFactor = exp(-max(0, worldPos.y - _FogBaseHeight) * _FogHeightFalloff);
                }

                float fogFactor = saturate(distanceFog * heightFactor * _FogDensity);

                float3 finalColor = lerp(sceneColor.rgb, _FogColor.rgb, fogFactor);

                return float4(finalColor, sceneColor.a);
            }
            ENDHLSL
        }
    }

    Fallback Off
}