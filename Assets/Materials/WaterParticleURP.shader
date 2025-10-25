Shader "Custom/ContinuousWaterURP"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0,0.5,0.7,0.5)
        _Radius("Particle Radius", Float) = 0.1
    }

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" }
        LOD 200
        Blend SrcAlpha OneMinusSrcAlpha   // alpha blending
        Cull Off
        ZWrite Off

        Pass
        {
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #include <HLSLSupport.cginc>
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float3 positionOS : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 objectPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            UNITY_INSTANCING_BUFFER_START(Props)
            UNITY_INSTANCING_BUFFER_END(Props)

            float4 _BaseColor;
            float _Radius;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_OUTPUT(Varyings, OUT);

                float4 worldPos = mul(GetObjectToWorldMatrix(), float4(IN.positionOS, 1));
                OUT.worldPos = worldPos.xyz;
                OUT.objectPos = IN.positionOS;
                OUT.positionHCS = TransformWorldToHClip(worldPos.xyz);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // View direction
                float3 viewDir = normalize(_WorldSpaceCameraPos - IN.worldPos);

                // Fresnel effect
                float fresnel = pow(1 - dot(viewDir, float3(0,1,0)), 3);
                half3 color = _BaseColor.rgb + fresnel * 0.2;

                // Distance from particle center (object space)
                float dist = length(IN.objectPos);

                // Soft falloff for smooth merging
                float alpha = _BaseColor.a * exp(-dist*dist / (_Radius*_Radius));

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
