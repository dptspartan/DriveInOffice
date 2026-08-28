Shader "CityDrive/LowPolyGrass"
{
    Properties
    {
        [MainTexture] _BaseMap ("Grass Texture", 2D) = "white" {}
        [MainColor] _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _ColorA ("Grass Color A", Color) = (0.40, 0.58, 0.28, 1)
        _ColorB ("Grass Color B", Color) = (0.28, 0.45, 0.18, 1)
        _ColorC ("Dry / Highlight", Color) = (0.55, 0.58, 0.30, 1)
        _FacetScale ("Low Poly Facet Size", Float) = 2.5
        _VariationStrength ("Color Variation", Range(0, 1)) = 0.75
        _TextureStrength ("Texture Strength", Range(0, 1)) = 0.55
        _Smoothness ("Smoothness", Range(0, 1)) = 0.05
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                half4 _BaseColor;
                half4 _ColorA;
                half4 _ColorB;
                half4 _ColorC;
                float _FacetScale;
                float _VariationStrength;
                float _TextureStrength;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float fogFactor : TEXCOORD3;
            };

            float LowPolyHash(float2 cell)
            {
                return frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs posInputs = GetVertexPositionInputs(input.positionOS.xyz);
                VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS);

                output.positionCS = posInputs.positionCS;
                output.positionWS = posInputs.positionWS;
                output.normalWS = normalInputs.normalWS;
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                output.fogFactor = ComputeFogFactor(posInputs.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float facetSize = max(_FacetScale, 0.001);
                float2 cell = floor(input.positionWS.xz / facetSize);
                float h0 = LowPolyHash(cell);
                float h1 = LowPolyHash(cell + float2(17.0, 9.0));

                // Faceted low-poly blend between three grass tones
                half3 grassTint = lerp(_ColorA.rgb, _ColorB.rgb, h0);
                grassTint = lerp(grassTint, _ColorC.rgb, h1 * 0.45);
                grassTint = lerp(_ColorA.rgb, grassTint, _VariationStrength);

                half4 tex = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv);
                half3 albedo = lerp(grassTint, tex.rgb * grassTint, _TextureStrength) * _BaseColor.rgb;

                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS);
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                // Flat-ish lighting for low-poly feel
                float wrap = ndotl * 0.65 + 0.35;
                half3 lit = albedo * mainLight.color * wrap;
                lit += albedo * 0.22; // ambient fill

                lit = MixFog(lit, input.fogFactor);
                return half4(lit, 1.0);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
