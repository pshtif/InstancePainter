Shader "PrefabPainter/InstancedIndirect"
{
    Properties
    {
        [HideInInspector]_BoundSize("_BoundSize", Vector) = (1,1,1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalRenderPipeline"}

        Pass
        {
            Cull Back
            ZTest Less
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature CULLING
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                half3 normalOS      : NORMAL;
                half4 color : COLOR0;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
                half3 color : COLOR0;
            };

            CBUFFER_START(UnityPerMaterial)
                float2 _BoundSize;

                StructuredBuffer<float4> _colorBuffer;
                StructuredBuffer<float4x4> _matrixBuffer;
                StructuredBuffer<uint> _visibleIdBuffer;
            CBUFFER_END

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;
                
                float4x4 instanceMatrix = _matrixBuffer[instanceID];                
                float3 positionWS = mul(instanceMatrix, IN.positionOS);
                half3 normalWS = normalize(mul(IN.normalOS, (float3x3)Inverse(instanceMatrix)));
                
                OUT.positionCS = TransformWorldToHClip(positionWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                
                half3 albedo = _colorBuffer[instanceID];
                
                half directDiffuse = dot(normalWS, mainLight.direction);
                half3 lighting = mainLight.color * (mainLight.shadowAttenuation * mainLight.distanceAttenuation);
                half3 result = albedo/2 + (albedo * directDiffuse) * lighting;
                
                OUT.color = result * IN.color.xyz;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(IN.color,1);
            }
            ENDHLSL
        }

        Pass
        {
            Tags { "LightMode" = "ShadowCaster" }
            
            Cull Off
            Blend One Zero
            ZTest LEqual
            ZWrite On
            ColorMask 0

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature CULLING
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT

            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                half3 normalOS      : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS  : SV_POSITION;
            };

            CBUFFER_START(UnityPerMaterial)
                float2 _BoundSize;

                StructuredBuffer<float4> _colorBuffer;
                StructuredBuffer<float4x4> _matrixBuffer;
            CBUFFER_END

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;
                
                float4x4 instanceMatrix = _matrixBuffer[instanceID];
                
                float3 positionWS = mul(instanceMatrix, IN.positionOS);
                
                OUT.positionCS = TransformWorldToHClip(positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return 1;
            }
            ENDHLSL
        }
    }
}