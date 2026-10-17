// Low-lying fog drawn as a stack of see-through layers that follow the ground
// (the mesh and all settings come from GroundFog.cs). The layers share one
// height pattern, so together they form separate clumps of fog with clear
// ground between them. Kept unlit and cheap for Quest: one noise sample per
// vertex, two per pixel, plus one main-light shadow lookup, which darkens fog
// in the shade of trees and gives soft light shafts.
Shader "VR Camping/Ground Fog"
{
    Properties
    {
        _FogColor ("Fog Color", Color) = (0.74, 0.8, 0.87, 1)
        _Density ("Density", Float) = 0.35
        _Amount ("Amount", Range(0, 1)) = 1
        _Coverage ("Coverage", Range(0, 1)) = 0.75
        _Wispiness ("Wispiness", Range(0, 1.5)) = 0.55
        _NoiseScale ("Noise Scale", Float) = 0.07
        _Wind ("Wind (XY, m/s)", Vector) = (0.24, 0.07, 0, 0)
        _LayerThickness ("Layer Thickness", Float) = 0.14
        _FogHeight ("Fog Height", Float) = 1.1
        _Shading ("Clump Shading", Range(0, 1)) = 0.45
        _LayerWave ("Layer Wave", Float) = 0.08
        _SunScattering ("Sun Scattering", Range(0, 2)) = 0.6
        _ShadowStrength ("Shadow Strength", Range(0, 1)) = 0.35
        [NoScaleOffset] _NoiseTex ("Noise", 2D) = "gray" {}
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            // Before other transparents, so fire, smoke and highlights draw on top.
            "Queue" = "Transparent-50"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "GroundFog"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            // 3.0 for the noise lookup in the vertex shader.
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _FogColor;
                half _Density;
                half _Amount;
                half _Coverage;
                half _Wispiness;
                float _NoiseScale;
                float4 _Wind;
                half _LayerThickness;
                half _FogHeight;
                half _Shading;
                half _LayerWave;
                half _SunScattering;
                half _ShadowStrength;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 layerAndFade : TEXCOORD0; // x: 0 = bottom layer, 1 = top layer. y: edge fade.
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 viewWS : TEXCOORD1; // camera to fragment, not normalised
                half3 normalWS : TEXCOORD2;
                half2 layerAndFade : TEXCOORD3;
                half heightFraction : TEXCOORD4; // 0 = ground, 1 = top of the fog, after bending
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                half layer = input.layerAndFade.x;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);

                // Bend the layers up and down, more towards the top, so the stack
                // doesn't show as flat slices when you look across it. Neighbouring
                // layers bend almost the same way, so they don't cut through each other.
                float2 drift = _Wind.xy * _Time.y;
                half wave = SAMPLE_TEXTURE2D_LOD(_NoiseTex, sampler_NoiseTex,
                    (positionWS.xz - drift) * (_NoiseScale * 0.8) + layer * float2(0.37, 0.23), 1).r;
                float bend = (wave - 0.5) * 2.0 * _LayerWave * layer;
                positionWS.y += bend;

                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.viewWS = positionWS - GetCameraPositionWS();
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.layerAndFade = input.layerAndFade;
                output.heightFraction = layer + bend / max(_FogHeight, 0.01);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half layer = input.layerAndFade.x;
                float viewDistance = length(input.viewWS);
                float3 viewDir = input.viewWS / max(viewDistance, 1e-4);

                // Every layer reads the same large noise, so the layers line up into
                // clumps instead of being separate patchy sheets. The detail noise is
                // warped by the large one and differs per layer, which roughens the
                // clump tops so they billow instead of looking like smooth domes.
                float2 drift = _Wind.xy * _Time.y;
                float2 positionXZ = input.positionWS.xz;
                half large = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                    (positionXZ - drift) * _NoiseScale).r;
                half detail = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex,
                    (positionXZ - drift * 1.6 - float2(-drift.y, drift.x) * 0.5) * (_NoiseScale * 2.7)
                    + (large - 0.5) * 0.6 + layer * float2(0.13, 0.41)).r;

                // How tall the clump is here, as a fraction of the fog height: highest
                // in the middle of a patch and down to nothing at its edge, leaving clear
                // ground between clumps. As the fog burns off (_Amount -> 0) the clumps
                // shrink and sink, so it clears like real fog instead of just fading.
                half threshold = 1.0 - _Coverage * (0.35 + 0.65 * _Amount);
                half shape = saturate((large - threshold) / max(1.0 - threshold, 0.05));
                half top = sqrt(shape) + (detail - 0.5) * _Wispiness * saturate(shape * 4.0);

                half heightFraction = input.heightFraction;
                half inside = smoothstep(heightFraction - 0.12, heightFraction + 0.12, top);
                // Denser in the middle of a clump, and fully gone outside it (the soft
                // top edge alone would leave a thin film on the bottom layer).
                half fog = inside * (0.5 + shape) * saturate(shape * 6.0);

                // 0 at the bottom of the clump, 1 at its top.
                half withinClump = saturate(heightFraction / max(top, 0.1));

                // Each layer stands in for a slab of fog. Looking across it at a shallow
                // angle is a longer path than looking straight down, so fog in the
                // distance builds up while fog around your feet stays see-through.
                half facing = abs(dot(viewDir, normalize(input.normalWS)));
                half pathLength = _LayerThickness / max(facing, 0.1);
                half alpha = 1.0 - exp(-_Density * _Amount * fog * pathLength);

                // Fade out at the edge of the fog area and right in front of the camera.
                alpha *= input.layerAndFade.y * saturate((viewDistance - 0.2) / 1.0);

                // Fog glows when you look towards the sun, and is a little darker
                // wherever trees shade it.
                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS), input.positionWS, half4(1, 1, 1, 1));
                half sunVisibility = sun.shadowAttenuation * sun.distanceAttenuation;
                half towardSun = saturate(dot(viewDir, sun.direction));
                towardSun *= towardSun;
                towardSun *= towardSun;
                half glow = towardSun * towardSun * _SunScattering * (0.5 + 0.5 * withinClump);

                // Fake light inside the fog: the bottom of each clump sits in its own
                // cool shade and the top catches the sky, which makes the clumps read
                // as solid shapes.
                half3 clumpShade = lerp(half3(0.92, 0.96, 1.04) * (1.0 - _Shading),
                                        half3(1, 1, 1) * (1.0 + _Shading * 0.25), withinClump);

                half3 color = _FogColor.rgb * clumpShade
                            * lerp(1.0 - _ShadowStrength, 1.0, sun.shadowAttenuation)
                            + sun.color * (glow * sunVisibility);

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
