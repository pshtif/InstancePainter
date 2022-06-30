/*
 *	Created by:  Peter @sHTiF Stefcek
 */

Shader "Hidden/Instance Painter/InstancedIndirectUtility"
{
    Properties
    {
        _Color ("Color", Color) = (0,0,0)
        
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
                float3 _Color;
            
                StructuredBuffer<float4> _colorBuffer;
                StructuredBuffer<float4x4> _matrixBuffer;
            CBUFFER_END

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;
                
                float4x4 instanceMatrix = _matrixBuffer[instanceID];                
                half3 normalWS = normalize(mul(instanceMatrix, IN.normalOS));

                float4 position = IN.positionOS;
                
                float4 positionWS = mul(instanceMatrix, position);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                
                positionWS = mul(UNITY_MATRIX_V, positionWS);
                OUT.positionCS = mul(UNITY_MATRIX_P, positionWS);
                
                half3 lighting = mainLight.color * mainLight.distanceAttenuation;
                
                lighting *= mainLight.shadowAttenuation;
                
                OUT.color = lighting * _Color;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(IN.color, 1);
            }
            ENDHLSL
        }
    }
}