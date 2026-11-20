Shader "RutaSegura/VolumetricLightHalo"
{
    Properties
    {
        [MainColor] _Color("Halo Color (RGBA)", Color) = (1.0, 0.78, 0.48, 0.16)
        _Intensity("Intensity", Range(0.0, 3.0)) = 1.0
        _FresnelPower("Cone Edge Softness", Range(0.5, 5.0)) = 1.8
        _SoftDepthDistance("Depth Soft Fade", Range(0.1, 4.0)) = 1.2
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
            Name "VolumetricHaloForward"
            Blend SrcAlpha One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Intensity;
                float _FresnelPower;
                float _SoftDepthDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                float  eyeDepth   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                OUT.eyeDepth = -TransformWorldToView(posInputs.positionWS).z;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 n = normalize(IN.normalWS);
                float3 v = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float fresnel = pow(abs(dot(n, v)), _FresnelPower);

                float vertFade = pow(saturate(IN.uv.y), 1.35) *
                                 smoothstep(0.0, 0.28, IN.uv.y) *
                                 smoothstep(1.0, 0.88, IN.uv.y);

                float2 screenUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneEyeZ = LinearEyeDepth(rawDepth, _ZBufferParams);
                float depthFade = saturate((sceneEyeZ - IN.eyeDepth) / max(0.01, _SoftDepthDistance));
                float camFade = smoothstep(0.35, 1.75, IN.eyeDepth);

                half alpha = saturate(_Color.a * (half)(fresnel * vertFade * depthFade * camFade * _Intensity));
                return half4(_Color.rgb * alpha, alpha);
            }
            ENDHLSL
        }
    }
}
