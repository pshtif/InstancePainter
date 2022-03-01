Shader "PrefabPainter/InstancedIndirectNoShadows"
{
    Properties
    {
        [HideInInspector]_BoundSize("_BoundSize", Vector) = (1,1,1)
        
         _WindIntensity ("_WindIntensity", Float) = .5
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
                float _WindIntensity;

                StructuredBuffer<float4> _colorBuffer;
                StructuredBuffer<float4x4> _matrixBuffer;
                StructuredBuffer<uint> _visibleIdBuffer;
            CBUFFER_END

            Varyings vert(Attributes IN, uint instanceID : SV_InstanceID)
            {
                Varyings OUT;

                //GetCameraPositionWS()
                
                float4x4 instanceMatrix = _matrixBuffer[instanceID];                
                half3 normalWS = normalize(mul(IN.normalOS, (float3x3)Inverse(instanceMatrix)));

                // Without billboarding
                //OUT.positionCS = TransformWorldToHClip(positionWS);

                float4 position = IN.positionOS;
                position.x += _WindIntensity * sin(_Time.y) * position.y*position.y;
                
                float4x4 v = UNITY_MATRIX_V;
                float3 right = normalize(v._m00_m01_m02);
                float3 up = normalize(v._m10_m11_m12);
                float3 forward = normalize(v._m20_m21_m22);
                //get the rotation parts of the matrix
                float4x4 rotationMatrix = float4x4(right, 0,
    	            up, 0,
    	            forward, 0,
    	            0, 0, 0, 1);
                float4x4 rotationMatrixInverse = transpose(rotationMatrix);
                
                float4 positionWS = mul(rotationMatrixInverse, position);
                positionWS = mul(instanceMatrix, positionWS);
                positionWS = mul(UNITY_MATRIX_V, positionWS);
                OUT.positionCS = mul(UNITY_MATRIX_P, positionWS);

                Light mainLight = GetMainLight(TransformWorldToShadowCoord(positionWS));
                
                half3 albedo = _colorBuffer[instanceID];
                
                half directDiffuse = dot(normalWS, mainLight.direction);
                half3 lighting = mainLight.color * (mainLight.shadowAttenuation * mainLight.distanceAttenuation);
                half3 result = albedo/2 + (albedo * directDiffuse) * lighting;
                
                OUT.color = lighting * IN.color.xyz;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                return half4(IN.color,1);
            }
            ENDHLSL
        }
    }
}