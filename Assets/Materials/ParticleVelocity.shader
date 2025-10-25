Shader "Custom/ParticleVelocity"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (1,1,1,1)
    }
    
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        LOD 100
        
        Pass
        {
            // Disable expensive features
            Cull Back
            ZWrite On
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma target 4.5
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                half3 worldNormal : TEXCOORD0;
                half4 color : COLOR;
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _BaseColor)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                
                o.pos = UnityObjectToClipPos(v.vertex);
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                o.color = UNITY_ACCESS_INSTANCED_PROP(Props, _BaseColor);
                
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                // Very simple lighting for performance
                half3 lightDir = _WorldSpaceLightPos0.xyz;
                half ndotl = dot(i.worldNormal, lightDir) * 0.5 + 0.5; // Wrap lighting
                
                return i.color * ndotl;
            }
            ENDCG
        }
        
        // Shadow pass removed for better performance
    }
    FallBack Off
}