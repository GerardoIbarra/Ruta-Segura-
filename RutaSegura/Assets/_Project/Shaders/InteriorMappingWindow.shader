Shader "RutaSegura/InteriorMappingWindow"
{
    Properties
    {
        [MainTexture] _BaseMap("Glass Map", 2D) = "white" {}
        [MainColor] _BaseColor("Glass Tint", Color) = (0.08, 0.12, 0.16, 1.0)
        _RoomSize("Room Grid Size (XYZ)", Vector) = (3.2, 3.0, 3.2, 0.0)
        _LitRatio("Lit Rooms Ratio", Range(0.0, 1.0)) = 0.50
        _InteriorBrightness("Interior Brightness", Range(0.0, 3.0)) = 1.25
        _GlassSmoothness("Glass Smoothness", Range(0.5, 1.0)) = 0.94
        _WarmLightColor("Warm Interior Light", Color) = (1.0, 0.76, 0.45, 1.0)
        _CoolLightColor("Cool Interior Light", Color) = (0.58, 0.76, 1.0, 1.0)
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

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                float4 _RoomSize;
                float _LitRatio;
                float _InteriorBrightness;
                float _GlassSmoothness;
                half4 _WarmLightColor;
                half4 _CoolLightColor;
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
                half   fogFactor  : TEXCOORD5;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float Hash31(float3 p)
            {
                p = frac(p * float3(0.1031, 0.1030, 0.0973));
                p += dot(p, p.yxz + 33.33);
                return frac((p.x + p.y) * p.z);
            }

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
                OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
                OUT.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(IN);

                float3 normalWS = NormalizeNormalPerPixel(IN.normalWS);
                half3 viewDirWS = GetWorldSpaceNormalizeViewDir(IN.positionWS);
                float3 rayDir = -viewDirWS;

                // Rejilla analitica de cuartos en world-space sin bucles
                float3 roomSize = max(float3(0.5, 0.5, 0.5), _RoomSize.xyz);
                float3 scaledPos = IN.positionWS / roomSize;
                float3 roomCell = floor(scaledPos);
                float3 localPos = frac(scaledPos);

                float3 safeRay = (abs(rayDir) < 1e-4) ? sign(rayDir + 1e-4) * 1e-4 : rayDir;
                float3 walls = step(0.0, safeRay);
                float3 tMax = (walls - localPos) / safeRay;
                float tMin = min(tMax.x, min(tMax.y, tMax.z));
                float3 hitPos = localPos + safeRay * tMin;

                float roomRand = Hash31(roomCell);
                float tempRand = Hash31(roomCell + float3(19.3, 7.1, 41.7));
                float isLit = step(1.0 - _LitRatio, roomRand);

                // Iluminacion de plafon en techo y sombreado de pared/piso
                float ceilLamp = 0.0;
                if (abs(hitPos.y - 1.0) < 0.02 || hitPos.y > 0.96)
                {
                    float lampDist = length(hitPos.xz - 0.5);
                    ceilLamp = smoothstep(0.32, 0.05, lampDist) * 1.35 + 0.40;
                }
                else if (hitPos.y < 0.04)
                {
                    ceilLamp = 0.22;
                }
                else
                {
                    ceilLamp = lerp(0.30, 0.88, saturate(hitPos.y));
                }

                // Cortinas sutiles a los lados de la ventana
                float curtain = smoothstep(0.18, 0.04, min(IN.uv.x, 1.0 - IN.uv.x)) * 0.45;

                half3 lightPalette = lerp(_WarmLightColor.rgb, _CoolLightColor.rgb, smoothstep(0.60, 0.85, tempRand));
                half3 roomAlbedo = lerp(half3(0.75, 0.72, 0.68), half3(0.45, 0.50, 0.58), step(0.5, tempRand));
                half3 litColor = roomAlbedo * lightPalette * (ceilLamp * (1.0 - curtain * 0.6)) * (half)_InteriorBrightness;
                half3 unlitColor = half3(0.03, 0.04, 0.06) * (0.6 + hitPos.y * 0.6);

                half3 interiorCol = lerp(unlitColor, litColor, (half)isLit);

                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirWS)), 4.0h);
                half3 glassTint = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, IN.uv).rgb * _BaseColor.rgb;

                InputData inputData = (InputData)0;
                inputData.positionWS = IN.positionWS;
                inputData.positionCS = IN.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = viewDirWS;
                inputData.shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
                inputData.fogCoord = InitializeInputDataFog(float4(IN.positionWS, 1.0), IN.fogFactor);
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(IN.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surfaceData = (SurfaceData)0;
                surfaceData.albedo = glassTint;
                surfaceData.metallic = 0.12h;
                surfaceData.specular = half3(0.08, 0.08, 0.08);
                surfaceData.smoothness = (half)_GlassSmoothness;
                surfaceData.normalTS = half3(0, 0, 1);
                surfaceData.emission = interiorCol * (1.0h - fresnel * 0.75h);
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
                float4 _RoomSize;
                float _LitRatio;
                float _InteriorBrightness;
                float _GlassSmoothness;
                half4 _WarmLightColor;
                half4 _CoolLightColor;
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
                float4 _RoomSize;
                float _LitRatio;
                float _InteriorBrightness;
                float _GlassSmoothness;
                half4 _WarmLightColor;
                half4 _CoolLightColor;
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
                float4 _RoomSize;
                float _LitRatio;
                float _InteriorBrightness;
                float _GlassSmoothness;
                half4 _WarmLightColor;
                half4 _CoolLightColor;
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
