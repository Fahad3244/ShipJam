Shader "Custom/HeightToonUnlit"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (1,1,1,1)
        _MinY("Bottom Height", Float) = -1
        _MaxY("Top Height", Float) = 1
        _Falloff("Fade Sharpness", Float) = 1
        _Steps("Toon Steps", Range(1,20)) = 2
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "UnlitPass"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionOS : TEXCOORD0;
            };

            float4 _BaseColor;
            float _MinY;
            float _MaxY;
            float _Falloff;
            float _Steps;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionOS = IN.positionOS.xyz;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                return OUT;
            }

            float4 frag (Varyings IN) : SV_Target
            {
                float y = IN.positionOS.y;

                // Normalize Y position
                float t = (y - _MinY) / (_MaxY - _MinY);
                t = saturate(t);
                t = pow(t, _Falloff);

                // Quantize for toon steps
                t = floor(t * _Steps) / (_Steps - 1);

                // Lerp between black and base color
                float3 finalColor = lerp(float3(0,0,0), _BaseColor.rgb, t);
                return float4(finalColor, _BaseColor.a);
            }

            ENDHLSL
        }
    }
}
