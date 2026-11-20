Shader "RutaSegura/HologramRouteRing"
{
    Properties
    {
        _Color ("Hologram Color", Color) = (0.18, 0.90, 1.0, 1.0)
        _Alpha ("Global Alpha", Range(0.0, 1.0)) = 1.0
        _PulseSpeed ("Pulse Speed", Float) = 2.6
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "RenderType" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "HologramForward"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Alpha;
                half _PulseSpeed;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv * 2.0 - 1.0;
                float dist = length(p);
                if (dist > 1.0) discard;

                float pulse = 0.80 + 0.20 * sin(_Time.y * _PulseSpeed);
                float ringOuter = smoothstep(0.06, 0.0, abs(dist - 0.84));
                float ringMid   = smoothstep(0.045, 0.0, abs(dist - 0.54));
                float ringInner = smoothstep(0.04, 0.0, abs(dist - (0.18 + 0.10 * pulse)));

                // Barrido radial giratorio
                float angle = atan2(p.y, p.x);
                float sweepAngle = frac((angle / 6.2831853) + _Time.y * 0.45);
                float radarWedge = smoothstep(0.55, 1.0, sweepAngle) * 0.45 * step(0.16, dist) * (1.0 - smoothstep(0.84, 0.98, dist));

                // Nucleo suave
                float centerDot = smoothstep(0.14, 0.02, dist) * 0.75;
                float edgeFade = 1.0 - smoothstep(0.86, 1.0, dist);

                float intensity = (ringOuter * 1.1 + ringMid * 0.75 + ringInner * 0.9 + radarWedge + centerDot) * edgeFade * pulse;
                half3 col = _Color.rgb * intensity;
                half alpha = saturate(intensity) * _Color.a * _Alpha;

                return half4(col, alpha);
            }
            ENDHLSL
        }
    }
}
