Shader "RutaSegura/UrbanFoliageWind"
{
    Properties
    {
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Base Color", Color) = (0.16, 0.34, 0.17, 1.0)
        [Normal] _BumpMap("Normal Map", 2D) = "bump" {}
        _BumpScale("Normal Scale", Float) = 0.65
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.18
        _WindSpeed("Wind Speed", Float) = 1.6
        _WindAmplitude("Wind Sway Amplitude", Float) = 0.08
        _TranslucencyColor("SSS Translucency Color", Color) = (0.55, 0.72, 0.24, 1.0)
        _TranslucencyStrength("SSS Strength", Range(0.0, 2.0)) = 0.55
        _AmbientLift("Shadow Ambient Lift", Range(0.0, 0.5)) = 0.16
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
                float _Smoothness;
                float _WindSpeed;
                float _WindAmplitude;
                half4 _TranslucencyColor;
                float _TranslucencyStrength;
                float _AmbientLift;
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
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS   : TEXCOORD2;
                half4  tangentWS  : TEXCOORD3;
                half   fogFactor  : TEXCOORD5;
                float  canopyMask : TEXCOORD6;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash31(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float Noise3D(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                float3 u = f * f * (3.0 - 2.0 * f);
                float n00 = lerp(Hash31(i), Hash31(i + float3(1, 0, 0)), u.x);
                float n10 = lerp(Hash31(i + float3(0, 1, 0)), Hash31(i + float3(1, 1, 0)), u.x);
                float n01 = lerp(Hash31(i + float3(0, 0, 1)), Hash31(i + float3(1, 0, 1)), u.x);
                float n11 = lerp(Hash31(i + float3(0, 1, 1)), Hash31(i + float3(1, 1, 1)), u.x);
                return lerp(lerp(n00, n10, u.y), lerp(n01, n11, u.y), u.z);
            }

            float3 ApplyWindWS(float3 positionOS)
            {
                float3 posWS = TransformObjectToWorld(positionOS);
                float weight = saturate((positionOS.y - 1.5) * 0.35);
                float phase = _Time.y * _WindSpeed + posWS.x * 0.45 + posWS.z * 0.38;
                posWS.x += sin(phase) * _WindAmplitude * weight;
                posWS.z += cos(phase * 0.83) * (_WindAmplitude * 0.72) * weight;
                return posWS;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS = ApplyWindWS(IN.positionOS.xyz);
                VertexNormalInputs normInputs = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

                OUT.positionWS = posWS;
                OUT.positionCS = TransformWorldToHClip(posWS);
                OUT.normalWS = normInputs.normalWS;
                real sign = IN.tangentOS.w * GetOddNegativeScale();
                OUT.tangentWS = half4(normInputs.tangentWS.xyz, sign);
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(OUT.positionCS.z);
                OUT.canopyMask = saturate((IN.positionOS.y - 1.5) * 0.35);
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
                float3 normalWS = NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tbn));
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);

                // Variacion organica por racimo de hojas para evitar bloque liso
                float leafBreak = Noise3D(IN.positionWS * 2.4);
                half leafShade = lerp(0.84h, 1.18h, (half)leafBreak);
                half3 finalAlbedo = texCol.rgb * lerp(1.0h, leafShade, (half)IN.canopyMask);

                float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                Light mainLight = GetMainLight(shadowCoord);

                // Translucidez SSS simulada en copa iluminada por contraluz + elevacion en sombra
                half backLit = pow(saturate(dot(-viewDirWS, mainLight.direction)), 3.0h);
                half rim = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 2.2h);
                half3 sssEmission = _TranslucencyColor.rgb * (backLit * (half)_TranslucencyStrength * (half)IN.canopyMask);
                half3 shadowLift = finalAlbedo * ((half)_AmbientLift + rim * 0.14h) * (half)IN.canopyMask;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.positionCS = IN.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = shadowCoord;
                inputData.fogCoord = InitializeInputDataFog(float4(IN.positionWS, 1.0), IN.fogFactor);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = finalAlbedo;
                surfaceData.metallic = 0.0h;
                surfaceData.specular = half3(0.04, 0.04, 0.04);
                surfaceData.smoothness = (half)_Smoothness;
                surfaceData.normalTS = normalTS;
                surfaceData.emission = sssEmission + shadowLift;
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
                float _Smoothness;
                float _WindSpeed;
                float _WindAmplitude;
                half4 _TranslucencyColor;
                float _TranslucencyStrength;
                float _AmbientLift;
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
                float weight = saturate((IN.positionOS.y - 1.5) * 0.35);
                float phase = _Time.y * _WindSpeed + positionWS.x * 0.45 + positionWS.z * 0.38;
                positionWS.x += sin(phase) * _WindAmplitude * weight;
                positionWS.z += cos(phase * 0.83) * (_WindAmplitude * 0.72) * weight;
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
                float _Smoothness;
                float _WindSpeed;
                float _WindAmplitude;
                half4 _TranslucencyColor;
                float _TranslucencyStrength;
                float _AmbientLift;
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
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float weight = saturate((IN.positionOS.y - 1.5) * 0.35);
                float phase = _Time.y * _WindSpeed + positionWS.x * 0.45 + positionWS.z * 0.38;
                positionWS.x += sin(phase) * _WindAmplitude * weight;
                positionWS.z += cos(phase * 0.83) * (_WindAmplitude * 0.72) * weight;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                return OUT;
            }

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
                float _Smoothness;
                float _WindSpeed;
                float _WindAmplitude;
                half4 _TranslucencyColor;
                float _TranslucencyStrength;
                float _AmbientLift;
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
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float weight = saturate((IN.positionOS.y - 1.5) * 0.35);
                float phase = _Time.y * _WindSpeed + positionWS.x * 0.45 + positionWS.z * 0.38;
                positionWS.x += sin(phase) * _WindAmplitude * weight;
                positionWS.z += cos(phase * 0.83) * (_WindAmplitude * 0.72) * weight;
                OUT.positionCS = TransformWorldToHClip(positionWS);
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
