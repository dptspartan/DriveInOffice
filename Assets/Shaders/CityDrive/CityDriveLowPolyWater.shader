Shader "CityDrive/LowPolyWater"
{
    Properties
    {
        [Header(Colors)]
        _ShallowColor ("Shallow Color", Color) = (0.32, 0.72, 0.78, 0.82)
        _DeepColor ("Deep Color", Color) = (0.05, 0.16, 0.30, 0.92)
        [Header(Low Poly Facets)]
        _FacetScale ("Facet Scale", Float) = 10
        _BandStrength ("Color Band Strength", Range(0, 1)) = 0.55
        _WaveSpeed ("Wave Speed", Float) = 0.1
        _Smoothness ("Smoothness", Range(0, 1)) = 0.22
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float _FacetScale;
                float _BandStrength;
                float _WaveSpeed;
                float _Smoothness;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float fogFactor : TEXCOORD2;
            };

            float LowPolyHash(float2 cell)
            {
                return frac(sin(dot(cell, float2(127.1, 311.7))) * 43758.5453);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float wave = sin((positionWS.x + positionWS.z) * 0.08 + _Time.y * _WaveSpeed) * 0.04;
                positionWS.y += wave;
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 facetCoord = floor(input.positionWS.xz / max(_FacetScale, 0.001));
                float facet = LowPolyHash(facetCoord);
                float wave = sin((input.positionWS.x + input.positionWS.z) * 0.06 + _Time.y * _WaveSpeed + facet * 6.28) * 0.5 + 0.5;

                half4 waterColor = lerp(_DeepColor, _ShallowColor, saturate(wave * _BandStrength + facet * (1.0 - _BandStrength)));

                Light mainLight = GetMainLight();
                float3 normalWS = normalize(input.normalWS);
                float ndotl = saturate(dot(normalWS, mainLight.direction));
                float3 viewDir = normalize(GetWorldSpaceViewDir(input.positionWS));
                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDir)), 3.0);
                float3 lit = waterColor.rgb * (0.55 + ndotl * 0.45) + fresnel * 0.12;

                half alpha = waterColor.a;
                lit = MixFog(lit, input.fogFactor);
                return half4(lit, alpha);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
