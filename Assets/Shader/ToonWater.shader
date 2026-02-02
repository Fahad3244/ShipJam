Shader "Custom/ToonWater"
{
    Properties
    {
        _ShallowColor ("Shallow Color", Color) = (0.325, 0.807, 0.971, 0.725)
        _DeepColor ("Deep Color", Color) = (0.086, 0.407, 1, 0.749)
        _FoamColor ("Foam Color", Color) = (1, 1, 1, 1)
        
        _DepthMaxDistance ("Depth Maximum Distance", Float) = 1
        _FoamDistance ("Foam Distance", Float) = 0.4
        _FoamSmoothness ("Foam Smoothness", Float) = 0.1
        
        _WaveHeight ("Wave Height", Float) = 0.2
        _WaveFrequency ("Wave Frequency", Float) = 2.0
        _WaveSpeed ("Wave Speed", Float) = 0.5
        
        _NormalStrength ("Normal Strength", Float) = 0.5
        _DistortionStrength ("Distortion Strength", Float) = 0.05
        
        _ToonSteps ("Toon Steps", Range(2, 10)) = 4
        _Glossiness ("Smoothness", Range(0, 1)) = 0.5
        _FresnelPower ("Fresnel Power", Range(0, 10)) = 2
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
            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };
            
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float4 screenPos : TEXCOORD2;
                float fogFactor : TEXCOORD3;
                float3 viewDirWS : TEXCOORD4;
            };
            
            CBUFFER_START(UnityPerMaterial)
                float4 _ShallowColor;
                float4 _DeepColor;
                float4 _FoamColor;
                float _DepthMaxDistance;
                float _FoamDistance;
                float _FoamSmoothness;
                float _WaveHeight;
                float _WaveFrequency;
                float _WaveSpeed;
                float _NormalStrength;
                float _DistortionStrength;
                float _ToonSteps;
                float _Glossiness;
                float _FresnelPower;
            CBUFFER_END
            
            // Procedural noise function
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)));
                return -1.0 + 2.0 * frac(sin(p) * 43758.5453123);
            }
            
            float noise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);
                
                return lerp(lerp(dot(hash2(i + float2(0.0, 0.0)), f - float2(0.0, 0.0)),
                                dot(hash2(i + float2(1.0, 0.0)), f - float2(1.0, 0.0)), u.x),
                           lerp(dot(hash2(i + float2(0.0, 1.0)), f - float2(0.0, 1.0)),
                                dot(hash2(i + float2(1.0, 1.0)), f - float2(1.0, 1.0)), u.x), u.y);
            }
            
            float fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                
                for(int i = 0; i < 4; i++)
                {
                    value += amplitude * noise(p * frequency);
                    frequency *= 2.0;
                    amplitude *= 0.5;
                }
                return value;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                
                // Multiple wave layers for more natural movement
                float time = _Time.y * _WaveSpeed;
                
                // Large waves
                float wave1 = sin((positionWS.x * 0.5 + positionWS.z * 0.3) * _WaveFrequency + time) * _WaveHeight;
                // Medium waves
                float wave2 = sin((positionWS.x * 0.8 - positionWS.z * 0.6) * _WaveFrequency * 1.5 + time * 1.3) * _WaveHeight * 0.6;
                // Small ripples
                float wave3 = sin((positionWS.x * 1.2 + positionWS.z * 1.1) * _WaveFrequency * 2.5 + time * 1.7) * _WaveHeight * 0.3;
                
                // Add noise-based disturbance
                float2 noiseUV = positionWS.xz * 0.5 + time * 0.1;
                float noiseWave = fbm(noiseUV) * _WaveHeight * 0.4;
                
                input.positionOS.y += wave1 + wave2 + wave3 + noiseWave;
                
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.screenPos = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.viewDirWS = GetWorldSpaceNormalizeViewDir(output.positionWS);
                
                return output;
            }
            
            float Posterize(float value, float steps)
            {
                return floor(value * steps) / steps;
            }
            
            // Calculate procedural normals for water surface
            float3 CalculateWaterNormal(float3 posWS, float time)
            {
                float offset = 0.1;
                
                // Sample multiple points to calculate tangent and bitangent
                float2 uv = posWS.xz;
                float2 uv1 = uv + float2(offset, 0);
                float2 uv2 = uv + float2(0, offset);
                
                // Multi-layer noise for normals
                float h = fbm(uv * 2.0 + time * 0.3);
                float h1 = fbm(uv1 * 2.0 + time * 0.3);
                float h2 = fbm(uv2 * 2.0 + time * 0.3);
                
                // Add wave contribution
                h += sin(uv.x * 3.0 + time) * 0.5;
                h1 += sin(uv1.x * 3.0 + time) * 0.5;
                h2 += sin(uv2.x * 3.0 + time) * 0.5;
                
                float3 va = float3(offset, (h1 - h) * _NormalStrength, 0);
                float3 vb = float3(0, (h2 - h) * _NormalStrength, offset);
                
                return normalize(cross(vb, va));
            }
            
            half4 frag(Varyings input) : SV_Target
            {
                // Calculate procedural normals
                float time = _Time.y * _WaveSpeed;
                float3 waterNormal = CalculateWaterNormal(input.positionWS, time);
                float3 normalWS = normalize(lerp(input.normalWS, waterNormal, 0.8));
                
                // Screen distortion for refraction effect
                float2 distortion = waterNormal.xz * _DistortionStrength;
                float2 screenUV = (input.screenPos.xy / input.screenPos.w) + distortion;
                
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float surfaceDepth = input.screenPos.w;
                float depthDifference = sceneDepth - surfaceDepth;
                
                // Water depth color with noise variation
                float waterDepthFactor = saturate(depthDifference / _DepthMaxDistance);
                
                // Add subtle color variation using noise
                float2 colorNoiseUV = input.positionWS.xz * 0.3 + time * 0.05;
                float colorNoise = fbm(colorNoiseUV) * 0.15 + 0.85;
                
                waterDepthFactor = Posterize(waterDepthFactor, _ToonSteps);
                float4 waterColor = lerp(_ShallowColor, _DeepColor, waterDepthFactor);
                waterColor.rgb *= colorNoise;
                
                // Foam with noise patterns
                float foamDepth = saturate(depthDifference / _FoamDistance);
                float foamBase = 1.0 - smoothstep(0, _FoamSmoothness, foamDepth);
                
                // Add noise to foam for more natural look
                float2 foamNoiseUV = input.positionWS.xz * 4.0 + time * 0.5;
                float foamNoise = fbm(foamNoiseUV) * 0.5 + 0.5;
                float foam = foamBase * foamNoise;
                foam = saturate(foam * 1.5);
                
                // Combine foam with water
                float4 finalColor = lerp(waterColor, _FoamColor, foam);
                
                // Toon lighting
                Light mainLight = GetMainLight();
                float NdotL = dot(normalWS, mainLight.direction);
                float lightIntensity = Posterize(saturate(NdotL), _ToonSteps);
                
                // Add specular highlights (toon style)
                float3 halfVector = normalize(mainLight.direction + input.viewDirWS);
                float NdotH = saturate(dot(normalWS, halfVector));
                float specular = pow(NdotH, 32.0 * _Glossiness);
                specular = step(0.5, specular) * 0.3;
                
                finalColor.rgb *= mainLight.color * (lightIntensity * 0.5 + 0.5);
                finalColor.rgb += specular * mainLight.color;
                
                // Fresnel effect for edge glow
                float fresnel = pow(1.0 - saturate(dot(normalWS, input.viewDirWS)), _FresnelPower);
                fresnel = Posterize(fresnel, 3);
                finalColor.rgb += fresnel * _ShallowColor.rgb * 0.3;
                
                // Apply fog
                finalColor.rgb = MixFog(finalColor.rgb, input.fogFactor);
                
                return finalColor;
            }
            ENDHLSL
        }
    }
    
    FallBack "Universal Render Pipeline/Lit"
}