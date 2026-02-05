Shader "Runner/ProceduralSky"
{
    Properties
    {
        [Header(Sky Colors)]
        _DaySkyColorTop ("Day Sky Top", Color) = (0.4, 0.7, 1.0, 1.0)
        _DaySkyColorHorizon ("Day Sky Horizon", Color) = (0.8, 0.9, 1.0, 1.0)
        _NightSkyColorTop ("Night Sky Top", Color) = (0.02, 0.02, 0.08, 1.0)
        _NightSkyColorHorizon ("Night Sky Horizon", Color) = (0.1, 0.1, 0.2, 1.0)
        
        [Header(Sunset Colors)]
        _SunsetColorTop ("Sunset Top", Color) = (0.5, 0.3, 0.5, 1.0)
        _SunsetColorHorizon ("Sunset Horizon", Color) = (1.0, 0.5, 0.2, 1.0)
        _SunsetColorGlow ("Sunset Glow", Color) = (1.0, 0.3, 0.1, 1.0)
        
        [Header(Sun)]
        _SunTex ("Sun Texture", 2D) = "white" {}
        _SunColor ("Sun Color", Color) = (1.0, 0.95, 0.8, 1.0)
        _SunSize ("Sun Size", Range(0.01, 0.2)) = 0.05
        _SunIntensity ("Sun Intensity", Range(0, 5)) = 2.0
        _SunGlowSize ("Sun Glow Size", Range(0.1, 1.0)) = 0.3
        _SunGlowIntensity ("Sun Glow Intensity", Range(0, 2)) = 0.5
        
        [Header(Moon)]
        _MoonTex ("Moon Texture", 2D) = "white" {}
        _MoonColor ("Moon Color", Color) = (0.8, 0.85, 1.0, 1.0)
        _MoonSize ("Moon Size", Range(0.01, 0.15)) = 0.04
        _MoonIntensity ("Moon Intensity", Range(0, 3)) = 1.0
        _MoonGlowSize ("Moon Glow Size", Range(0.1, 0.5)) = 0.15
        _MoonGlowIntensity ("Moon Glow Intensity", Range(0, 1)) = 0.3
        
        [Header(Stars)]
        _StarsCubemap ("Stars Cubemap", Cube) = "black" {}
        _StarsIntensity ("Stars Intensity", Range(0, 2)) = 1.0
        _StarsTwinkleSpeed ("Stars Twinkle Speed", Range(0, 5)) = 1.0
        _StarsTwinkleAmount ("Stars Twinkle Amount", Range(0, 1)) = 0.3
        
        [Header(Clouds)]
        _CloudsTex ("Clouds Texture", 2D) = "black" {}
        _CloudsColor ("Clouds Color", Color) = (1.0, 1.0, 1.0, 1.0)
        _CloudsSpeed ("Clouds Speed", Range(0, 0.1)) = 0.01
        _CloudsScale ("Clouds Scale", Range(0.1, 5)) = 1.0
        _CloudsOpacity ("Clouds Opacity", Range(0, 1)) = 0.5
        
        [Header(Atmosphere)]
        _HorizonSharpness ("Horizon Sharpness", Range(1, 10)) = 3.0
        _HorizonOffset ("Horizon Offset", Range(-0.5, 0.5)) = 0.0
        _AtmosphereThickness ("Atmosphere Thickness", Range(0, 2)) = 1.0
        
        [Header(Time Control)]
        _TimeOfDay ("Time of Day", Range(0, 1)) = 0.5
        
        [Header(Biome Tint)]
        _BiomeTintDay ("Biome Tint Day", Color) = (1.0, 1.0, 1.0, 1.0)
        _BiomeTintNight ("Biome Tint Night", Color) = (1.0, 1.0, 1.0, 1.0)
        _BiomeTintStrength ("Biome Tint Strength", Range(0, 1)) = 0.0
    }
    
    SubShader
    {
        Tags 
        { 
            "Queue" = "Background" 
            "RenderType" = "Background" 
            "PreviewType" = "Skybox" 
        }
        
        Cull Off 
        ZWrite Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            
            #include "UnityCG.cginc"
            
            // Sky Colors
            float4 _DaySkyColorTop;
            float4 _DaySkyColorHorizon;
            float4 _NightSkyColorTop;
            float4 _NightSkyColorHorizon;
            
            // Sunset
            float4 _SunsetColorTop;
            float4 _SunsetColorHorizon;
            float4 _SunsetColorGlow;
            
            // Sun
            sampler2D _SunTex;
            float4 _SunColor;
            float _SunSize;
            float _SunIntensity;
            float _SunGlowSize;
            float _SunGlowIntensity;
            float3 _SunDirection;
            
            // Moon
            sampler2D _MoonTex;
            float4 _MoonColor;
            float _MoonSize;
            float _MoonIntensity;
            float _MoonGlowSize;
            float _MoonGlowIntensity;
            float3 _MoonDirection;
            
            // Stars
            samplerCUBE _StarsCubemap;
            float _StarsIntensity;
            float _StarsTwinkleSpeed;
            float _StarsTwinkleAmount;
            
            // Clouds
            sampler2D _CloudsTex;
            float4 _CloudsColor;
            float _CloudsSpeed;
            float _CloudsScale;
            float _CloudsOpacity;
            
            // Atmosphere
            float _HorizonSharpness;
            float _HorizonOffset;
            float _AtmosphereThickness;
            
            // Time
            float _TimeOfDay;
            
            // Biome
            float4 _BiomeTintDay;
            float4 _BiomeTintNight;
            float _BiomeTintStrength;
            
            struct appdata
            {
                float4 vertex : POSITION;
                float3 texcoord : TEXCOORD0;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
            };
            
            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldPos = v.texcoord;
                o.viewDir = normalize(v.texcoord);
                return o;
            }
            
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }
            
            float GetDayFactor()
            {
                // 0.25 = dawn, 0.5 = noon, 0.75 = dusk, 0/1 = midnight
                float t = _TimeOfDay;
                
                if (t < 0.2 || t > 0.8)
                    return 0.0;
                if (t < 0.3)
                    return smoothstep(0.2, 0.3, t);
                if (t > 0.7)
                    return 1.0 - smoothstep(0.7, 0.8, t);
                return 1.0;
            }
            
            float GetSunsetFactor()
            {
                float t = _TimeOfDay;
                float dawn = smoothstep(0.2, 0.25, t) * (1.0 - smoothstep(0.3, 0.35, t));
                float dusk = smoothstep(0.65, 0.75, t) * (1.0 - smoothstep(0.8, 0.85, t));
                return max(dawn, dusk);
            }
            
            float3 GetSkyGradient(float3 viewDir)
            {
                float horizon = pow(1.0 - saturate(viewDir.y + _HorizonOffset), _HorizonSharpness);
                float dayFactor = GetDayFactor();
                float sunsetFactor = GetSunsetFactor();
                
                // Day colors
                float3 dayColor = lerp(_DaySkyColorTop.rgb, _DaySkyColorHorizon.rgb, horizon);
                
                // Night colors
                float3 nightColor = lerp(_NightSkyColorTop.rgb, _NightSkyColorHorizon.rgb, horizon);
                
                // Sunset colors
                float3 sunsetColor = lerp(_SunsetColorTop.rgb, _SunsetColorHorizon.rgb, horizon);
                
                // Blend day/night
                float3 baseColor = lerp(nightColor, dayColor, dayFactor);
                
                // Add sunset
                baseColor = lerp(baseColor, sunsetColor, sunsetFactor);
                
                // Apply biome tint
                float3 biomeTint = lerp(_BiomeTintNight.rgb, _BiomeTintDay.rgb, dayFactor);
                baseColor = lerp(baseColor, baseColor * biomeTint, _BiomeTintStrength);
                
                return baseColor;
            }
            
            float3 GetSun(float3 viewDir)
            {
                float3 sunDir = normalize(_SunDirection);
                float sunDot = dot(viewDir, sunDir);
                
                // Sun disk
                float sunDisk = smoothstep(1.0 - _SunSize * 0.1, 1.0 - _SunSize * 0.05, sunDot);
                
                // Sun glow
                float sunGlow = pow(saturate(sunDot), 8.0 / _SunGlowSize) * _SunGlowIntensity;
                
                // Sun texture UV
                float2 sunUV = float2(0.5, 0.5);
                if (sunDot > 0.9)
                {
                    float3 perpX = normalize(cross(sunDir, float3(0, 1, 0)));
                    float3 perpY = normalize(cross(sunDir, perpX));
                    float2 localUV;
                    localUV.x = dot(viewDir - sunDir * sunDot, perpX);
                    localUV.y = dot(viewDir - sunDir * sunDot, perpY);
                    sunUV = localUV / (_SunSize * 0.2) + 0.5;
                }
                
                float3 sunTex = tex2D(_SunTex, sunUV).rgb;
                float3 sunColor = _SunColor.rgb * _SunIntensity;
                
                float dayFactor = GetDayFactor();
                float sunVisible = saturate(sunDir.y + 0.1) * dayFactor;
                
                return (sunDisk * sunTex * sunColor + sunGlow * _SunColor.rgb) * sunVisible;
            }
            
            float3 GetMoon(float3 viewDir)
            {
                float3 moonDir = normalize(_MoonDirection);
                float moonDot = dot(viewDir, moonDir);
                
                // Moon disk
                float moonDisk = smoothstep(1.0 - _MoonSize * 0.1, 1.0 - _MoonSize * 0.05, moonDot);
                
                // Moon glow
                float moonGlow = pow(saturate(moonDot), 16.0 / _MoonGlowSize) * _MoonGlowIntensity;
                
                // Moon texture UV
                float2 moonUV = float2(0.5, 0.5);
                if (moonDot > 0.9)
                {
                    float3 perpX = normalize(cross(moonDir, float3(0, 1, 0)));
                    float3 perpY = normalize(cross(moonDir, perpX));
                    float2 localUV;
                    localUV.x = dot(viewDir - moonDir * moonDot, perpX);
                    localUV.y = dot(viewDir - moonDir * moonDot, perpY);
                    moonUV = localUV / (_MoonSize * 0.2) + 0.5;
                }
                
                float3 moonTex = tex2D(_MoonTex, moonUV).rgb;
                float3 moonColor = _MoonColor.rgb * _MoonIntensity;
                
                float nightFactor = 1.0 - GetDayFactor();
                float moonVisible = saturate(moonDir.y + 0.1) * nightFactor;
                
                return (moonDisk * moonTex * moonColor + moonGlow * _MoonColor.rgb * 0.5) * moonVisible;
            }
            
            float3 GetStars(float3 viewDir)
            {
                float nightFactor = 1.0 - GetDayFactor();
                
                if (nightFactor < 0.01)
                    return float3(0, 0, 0);
                
                float3 stars = texCUBE(_StarsCubemap, viewDir).rgb;
                
                // Twinkle
                float twinkle = sin(_Time.y * _StarsTwinkleSpeed + hash(viewDir.xz) * 100.0) * 0.5 + 0.5;
                twinkle = lerp(1.0, twinkle, _StarsTwinkleAmount);
                
                // Fade near horizon
                float horizonFade = saturate(viewDir.y * 2.0);
                
                return stars * _StarsIntensity * nightFactor * twinkle * horizonFade;
            }
            
            float3 GetClouds(float3 viewDir)
            {
                if (_CloudsOpacity < 0.01)
                    return float3(0, 0, 0);
                
                // Project onto dome
                float2 cloudUV = viewDir.xz / (viewDir.y + 0.5) * _CloudsScale;
                cloudUV += _Time.x * _CloudsSpeed;
                
                float clouds = tex2D(_CloudsTex, cloudUV).r;
                
                // Fade at horizon
                float horizonFade = saturate(viewDir.y * 3.0);
                
                float dayFactor = GetDayFactor();
                float3 cloudColor = _CloudsColor.rgb * lerp(0.3, 1.0, dayFactor);
                
                // Sunset tint on clouds
                float sunsetFactor = GetSunsetFactor();
                cloudColor = lerp(cloudColor, _SunsetColorGlow.rgb, sunsetFactor * 0.5);
                
                return cloudColor * clouds * _CloudsOpacity * horizonFade;
            }
            
            float4 frag(v2f i) : SV_Target
            {
                float3 viewDir = normalize(i.viewDir);
                
                // Base sky gradient
                float3 skyColor = GetSkyGradient(viewDir);
                
                // Add stars (behind everything)
                float3 stars = GetStars(viewDir);
                skyColor += stars;
                
                // Add sun
                float3 sun = GetSun(viewDir);
                skyColor += sun;
                
                // Add moon
                float3 moon = GetMoon(viewDir);
                skyColor += moon;
                
                // Add clouds (on top)
                float3 clouds = GetClouds(viewDir);
                skyColor = lerp(skyColor, skyColor + clouds, _CloudsOpacity);
                
                // Horizon glow during sunset
                float sunsetFactor = GetSunsetFactor();
                float horizonGlow = pow(1.0 - saturate(abs(viewDir.y)), 8.0);
                skyColor += _SunsetColorGlow.rgb * horizonGlow * sunsetFactor * 0.5;
                
                return float4(skyColor, 1.0);
            }
            
            ENDCG
        }
    }
    
    Fallback Off
}