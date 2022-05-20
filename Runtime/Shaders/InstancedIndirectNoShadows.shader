/*
 *	Created by:  Peter @sHTiF Stefcek
 */

Shader "Instance Painter/InstancedIndirectNoShadows"
{
    Properties
    {
        _AmbientLight ("Ambient Light", Color) = (0,0,0)
        
        [HideInInspector]_BoundSize("_BoundSize", Vector) = (1,1,1)
        
        _WindIntensity ("Wind Intensity", Float) = .5
        _WindTiling ("Wind Tiling", Float) = 0
        _WindTimeScale ("Wind Time Scale", Float) = 1
        
        [Toggle(ENABLE_WIND)] _EnableWind ("Enable Wind", Float) = 0
        [Toggle(ENABLE_BILLBOARD)] _EnableBillboard ("Enable Billboard", Float) = 0
        [Toggle(ENABLE_RECEIVE_SHADOWS)] _EnableReceiveShadows ("Enable Receive Shadows", Float) = 0
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

            #pragma multi_compile _ ENABLE_WIND
            #pragma multi_compile _ ENABLE_BILLBOARD
            #pragma multi_compile _ ENABLE_RECEIVE_SHADOWS

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
                float _WindIntensity;
                float _WindTiling;
                float _WindTimeScale;
                float3 _AmbientLight;

                StructuredBuffer<float4> _colorBuffer;
                StructuredBuffer<float4x4> _matrixBuffer;
                //StructuredBuffer<uint> _visibleIdBuffer;
            CBUFFER_END

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;

                //GetCameraPositionWS()
                
                float4x4 instanceMatrix = _matrixBuffer[instanceID];                
                half3 normalWS = normalize(mul(instanceMatrix, IN.normalOS));

                // Without billboarding
                //OUT.positionCS = TransformWorldToHClip(positionWS);

                float4 position = IN.positionOS;
                #if ENABLE_WIND
                float4 positionForWind = mul(instanceMatrix, position);
                position.x += _WindIntensity * sin(_Time.y * _WindTimeScale + positionForWind.x * _WindTiling + positionForWind.z * _WindTiling) * position.y;
                #endif

                #if ENABLE_BILLBOARD
                float4x4 v = unity_WorldToCamera;
                float3 right = normalize(v._m00_m01_m02);
                float3 up = normalize(v._m10_m11_m12);
                float3 forward = normalize(v._m20_m21_m22);
                float4x4 rotationMatrix = float4x4(right, 0,
    	            up, 0,
    	            forward, 0,
    	            0, 0, 0, 1);
                float4x4 rotationMatrixInverse = transpose(rotationMatrix);
                
                position = mul(rotationMatrixInverse, position);
                #endif
                
                float4 positionWS = mul(instanceMatrix, position);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                
                positionWS = mul(UNITY_MATRIX_V, positionWS);
                OUT.positionCS = mul(UNITY_MATRIX_P, positionWS);
                
                half3 lighting = mainLight.color * mainLight.distanceAttenuation;
                
                // No direct light for billboarding would change on camera rotation
                #if !ENABLE_BILLBOARD
                half directDiffuse = dot(normalWS, mainLight.direction);
                lighting *= directDiffuse;
                #endif
                
                #if ENABLE_RECEIVE_SHADOWS
                lighting *= mainLight.shadowAttenuation;
                #endif
                
                OUT.color = (lighting + _AmbientLight) * _colorBuffer[instanceID] * IN.color.xyz;

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