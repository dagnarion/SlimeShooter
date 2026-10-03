// Nước chảy dọc theo UV.x (quãng đường trên băng chuyền), không cần texture.
Shader "SlimeShooter/WaterFlowUnlit"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.16, 0.45, 0.92, 1)
        _StripeColor ("Stripe Color", Color) = (0.62, 0.85, 1, 1)
        _StripeScale ("Stripe Scale", Float) = 0.8
        _Speed ("Speed", Float) = 1.2
        _StripeSharpness ("Stripe Sharpness", Range(1, 20)) = 8
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        Pass
        {
            Name "Unlit"
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
                half4 _StripeColor;
                float _StripeScale;
                float _Speed;
                float _StripeSharpness;
            CBUFFER_END

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

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float u = input.uv.x * _StripeScale - _Time.y * _Speed;
                float across = input.uv.y;
                float wave = sin((u + sin(across * 6.2831) * 0.15) * 6.2831) * 0.5 + 0.5;
                float stripe = pow(wave, _StripeSharpness);
                float edge = smoothstep(0.0, 0.2, across) * smoothstep(1.0, 0.8, across);

                half3 color = lerp(_StripeColor.rgb, _BaseColor.rgb, 0.65 + 0.35 * edge);
                color = lerp(color, _StripeColor.rgb, stripe * edge * 0.8);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
}
