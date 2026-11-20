Shader "RutaSegura/WetAsphaltPuddles"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 1.4
        _DrySmoothness("Dry Smoothness", Range(0.0, 1.0)) = 0.28
        _WetSmoothness("Puddle Smoothness", Range(0.0, 1.0)) = 0.95
        _PuddleScale("Puddle World Scale", Float) = 0.11
        _PuddleThreshold("Puddle Threshold", Range(0.1, 0.9)) = 0.54
        _PuddleSoftness("Puddle Edge Softness", Range(0.01, 0.35)) = 0.10
        _WetDarkening("Wet Albedo Darkening", Range(0.2, 1.0)) = 0.48
        _RippleStrength("Wind Ripple Strength", Range(0.0, 0.1)) = 0.025
        _Metallic("Metallic", Range(0.0, 1.0)) = 0.04
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
        }
        LOD 300

        Pass
        {
            Name "UniversalForward"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            TEXTURE2D(_BumpMap); SAMPLER(sampler_BumpMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _BumpScale;
                float _DrySmoothness;
                float _WetSmoothness;
                float _PuddleScale;
                float _PuddleThreshold;
                float _PuddleSoftness;
                float _WetDarkening;
                float _RippleStrength;
                float _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float4 tangentOS  : TANGENT;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float3 positionWS  : TEXCOORD1;
                float3 normalWS    : TEXCOORD2;
                half4  tangentWS   : TEXCOORD3;
                half   fogFactor   : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                VertexPositionInputs posInputs = GetVertexPositionInputs(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normInputs.normalWS;
                real sign = IN.tangentOS.w * GetOddNegativeScale();
                OUT.tangentWS = half4(normInputs.tangentWS.xyz, sign);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                half4 texCol = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv) * _BaseColor;
                half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, IN.uv), _BumpScale);

                float3 bitangent = IN.tangentWS.w * cross(IN.normalWS, IN.tangentWS.xyz);
                half3x3 tbn = half3x3(IN.tangentWS.xyz, bitangent, IN.normalWS);
                float3 dryNormalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tbn));

                // Charcos organicos multi-octava sobre plano horizontal del asfalto
                float2 pCoord = IN.positionWS.xz * _PuddleScale;
                float n1 = ValueNoise2D(pCoord);
                float n2 = ValueNoise2D(pCoord * 2.35 + float2(17.4, -9.2));
                float puddleField = n1 * 0.68 + n2 * 0.32;

                // Ligera acumulacion natural junto a cunetas
                float gutterBias = saturate(1.0 - abs(abs(IN.positionWS.x) - 3.9) * 0.55) * 0.08;
                float upMask = saturate(IN.normalWS.y);
                float wetnessVal = (puddleField + gutterBias) * upMask;

                float dampMask = smoothstep(_PuddleThreshold - _PuddleSoftness * 2.4, _PuddleThreshold + _PuddleSoftness * 0.4, wetnessVal);
                float puddleMask = smoothstep(_PuddleThreshold - _PuddleSoftness, _PuddleThreshold + _PuddleSoftness, wetnessVal);

                // Micro-ondas por viento sobre espejos de agua
                float ripX = sin(IN.positionWS.x * 19.0 + _Time.y * 2.4 + n2 * 4.0);
                float ripZ = cos(IN.positionWS.z * 23.0 - _Time.y * 1.9 + n1 * 4.0);
                float3 rippleNormal = normalize(float3(ripX * _RippleStrength, 1.0, ripZ * _RippleStrength));
                float3 finalNormalWS = NormalizeNormalPerPixel(lerp(dryNormalWS, rippleNormal, puddleMask));

                half dampDarken = lerp(1.0h, (half)_WetDarkening, saturate(dampMask * 0.65h + puddleMask * 0.45h));
                half3 finalAlbedo = texCol.rgb * dampDarken;

                half dampSmooth = lerp((half)_DrySmoothness, saturate((half)_DrySmoothness + 0.24h), dampMask);
                half finalSmooth = lerp(dampSmooth, (half)_WetSmoothness, puddleMask);

                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                half fresnelBoost = pow(1.0h - saturate(dot(finalNormalWS, viewDirWS)), 4.0h) * puddleMask * 0.20h;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.positionCS = IN.positionCS;
                inputData.normalWS = finalNormalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = InitializeInputDataFog(float4(IN.positionWS, 1.0), IN.fogFactor);
                inputData.bakedGI = SampleSH(finalNormalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalAlbedo;
                surfaceData.metallic = saturate((half)_Metallic + fresnelBoost);
                surfaceData.specular = half3(0.04, 0.04, 0.04);
                surfaceData.smoothness = finalSmooth;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = half3(0, 0, 0);
                surfaceData.occlusion = 1.0h;
                surfaceData.alpha = 1.0h;

                half4 col = UniversalFragmentPBR(inputData, surfaceData);
                col.rgb = MixFog(col.rgb, inputData.fogCoord);
                return col;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            ColorMask 0
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex ShadowVert
            #pragma fragment ShadowFrag
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _BumpScale;
                float _DrySmoothness;
                float _WetSmoothness;
                float _PuddleScale;
                float _PuddleThreshold;
                float _PuddleSoftness;
                float _WetDarkening;
                float _RippleStrength;
                float _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings ShadowVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);
                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDirectionWS = normalize(_LightPosition - positionWS);
                #else
                    float3 lightDirectionWS = _LightDirection;
                #endif
                float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
                OUT.positionCS = ApplyShadowClamping(positionCS);
                return OUT;
            }

            half4 ShadowFrag(Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }

            ZWrite On
            ColorMask R
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _BumpScale;
                float _DrySmoothness;
                float _WetSmoothness;
                float _PuddleScale;
                float _PuddleThreshold;
                float _PuddleSoftness;
                float _WetDarkening;
                float _RippleStrength;
                float _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            half ShadowDepthZero() { return 0; }

            half DepthFrag(Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return 0;
            }
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }

            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex DepthNormVert
            #pragma fragment DepthNormFrag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float _BumpScale;
                float _DrySmoothness;
                float _WetSmoothness;
                float _PuddleScale;
                float _PuddleThreshold;
                float _PuddleSoftness;
                float _WetDarkening;
                float _RippleStrength;
                float _Metallic;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings DepthNormVert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                return OUT;
            }

            half4 DepthNormFrag(Varyings IN) : SV_TARGET
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                return half4(NormalizeNormalPerPixel(IN.normalWS), 0.0);
            }
            ENDHLSL
        }
    }
}
