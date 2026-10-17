// 부끄부끄 아웃라인. Inverted hull 방식 — 앞면을 컬링하고 뒷면을 노멀 방향으로 밀어내 외곽선만 남긴다.
// 확장은 클립 공간에서 수행하고 positionCS.w를 곱해 투영 나눗셈을 상쇄하므로, 카메라와의 거리에 무관하게 화면상 두께가 일정하다.
Shader "Timeless/Boo Outline"
{
    Properties
    {
        [HDR] _OutlineColor("Outline Color", Color) = (1, 1, 1, 1)
        _OutlineWidth("Outline Width (화면 높이 비율)", Range(0, 0.05)) = 0.005
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            // 몸보다 뒤에 그려야 한다. 몸이 깊이를 먼저 써야 확장된 뒷면의 내부가 가려지고 외곽선만 남는다
            "Queue" = "Transparent+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Outline"
            Tags { "LightMode" = "UniversalForward" }

            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _OutlineColor;
                float _OutlineWidth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;

                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                float4 positionCS = TransformObjectToHClip(input.positionOS.xyz);
                float3 normalVS = TransformWorldToViewDir(TransformObjectToWorldNormal(input.normalOS), true);
                float2 normalCS = mul((float3x3)UNITY_MATRIX_P, normalVS).xy;

                // 노멀이 정확히 카메라를 향하면 xy가 0이 되어 normalize가 NaN이 된다
                float2 offset = dot(normalCS, normalCS) > 1e-8 ? normalize(normalCS) : float2(0, 0);
                offset *= _OutlineWidth * positionCS.w;
                // 클립 공간 xy는 각각 화면 너비/높이를 -1~1로 덮으므로, 가로 두께를 종횡비로 보정해 굵기를 맞춘다
                offset.x *= _ScreenParams.y / _ScreenParams.x;

                positionCS.xy += offset;
                output.positionCS = positionCS;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                return half4(_OutlineColor);
            }
            ENDHLSL
        }
    }

    FallBack Off
}
