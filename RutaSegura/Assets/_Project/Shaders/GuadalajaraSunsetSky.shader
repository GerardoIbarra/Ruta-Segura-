Shader "RutaSegura/GuadalajaraSunsetSky"
{
    Properties
    {
        _ZenithColor("Zenith Color", Color) = (0.09, 0.14, 0.28, 1.0)
        _MidSkyColor("Mid Sky Color", Color) = (0.28, 0.34, 0.48, 1.0)
        _HorizonColor("Horizon Color", Color) = (0.82, 0.48, 0.28, 1.0)
        _GroundColor("Ground Bounce Color", Color) = (0.14, 0.13, 0.14, 1.0)
        _SunColor("Sun Glow Color", Color) = (1.0, 0.76, 0.44, 1.0)
        _CloudColorLit("Cloud Lit Rim", Color) = (0.98, 0.68, 0.44, 1.0)
        _CloudColorShadow("Cloud Shadow Belly", Color) = (0.22, 0.24, 0.34, 1.0)
        _CloudCoverage("Cloud Coverage", Range(0.0, 1.0)) = 0.48
        _CloudSpeed("Cloud Wind Speed", Float) = 0.008
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Background"
            "RenderType" = "Background"
            "PreviewType" = "Skybox"
        }

        Cull Off
        ZWrite Off

        Pass
        {
            Name "GuadalajaraSunsetSky"

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half4 _MidSkyColor;
                half4 _HorizonColor;
                half4 _GroundColor;
                half4 _SunColor;
                half4 _CloudColorLit;
                half4 _CloudColorShadow;
                float _CloudCoverage;
                float _CloudSpeed;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 rayDir     : TEXCOORD0;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            float Noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(
                    lerp(Hash21(i), Hash21(i + float2(1.0, 0.0)), u.x),
                    lerp(Hash21(i + float2(0.0, 1.0)), Hash21(i + float3(1.0, 1.0, 0.0).xy), u.x),
                    u.y);
            }

            float FbmClouds(float2 p)
            {
                float v = 0.0;
                float amp = 0.52;
                for (int i = 0; i < 4; i++)
                {
                    v += Noise2D(p) * amp;
                    p = p * 2.05 + float2(13.7, 9.1);
                    amp *= 0.5;
                }
                return v;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.rayDir = IN.positionOS.xyz;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 dir = normalize(IN.rayDir);
                float up = dir.y;

                // Gradiente atmosferico crepuscular
                half3 upperSky = lerp(_HorizonColor.rgb, _MidSkyColor.rgb, smoothstep(0.0, 0.26, saturate(up)));
                upperSky = lerp(upperSky, _ZenithColor.rgb, smoothstep(0.20, 0.78, saturate(up)));
                half3 sky = lerp(_GroundColor.rgb, upperSky, smoothstep(-0.08, 0.03, up));

                // Sol bajo y dispersion Mie hacia el oeste
                float3 sunDir = length(_MainLightPosition.xyz) > 0.001
                    ? normalize(_MainLightPosition.xyz)
                    : normalize(float3(0.55, 0.36, 0.75));
                float sunDot = saturate(dot(dir, sunDir));
                half sunGlow = (half)(pow(sunDot, 12.0) * 0.42 + pow(sunDot, 180.0) * 1.15);
                sky += _SunColor.rgb * sunGlow * smoothstep(-0.05, 0.05, up);

                // Dos capas suaves de nubes con desplazamiento en perspectiva
                if (up > 0.005)
                {
                    float2 projUV = dir.xz / (up + 0.14);
                    float drift = _Time.y * _CloudSpeed;
                    float c1 = FbmClouds(projUV * 0.85 + float2(drift, drift * 0.35));
                    float c2 = FbmClouds(projUV * 1.65 - float2(drift * 0.6, -drift * 0.25) + 4.2);
                    float density = c1 * 0.68 + c2 * 0.32;

                    float threshold = 1.0 - _CloudCoverage * 0.65;
                    float cloudAlpha = smoothstep(threshold - 0.12, threshold + 0.16, density);
                    cloudAlpha *= smoothstep(0.005, 0.18, up);

                    half rimMix = saturate((half)(pow(sunDot, 3.0) * 0.75 + (c1 - 0.35) * 0.8));
                    half3 cloudCol = lerp(_CloudColorShadow.rgb, _CloudColorLit.rgb, rimMix);
                    sky = lerp(sky, cloudCol, (half)cloudAlpha * 0.88h);
                }

                return half4(sky, 1.0h);
            }
            ENDHLSL
        }
    }
}
