Shader "Hidden/RenderDepth" {
    Properties {}
    SubShader {
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata {
                float4 vertex : POSITION;
            };

            struct v2f {
                float4 clipPos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            v2f vert (appdata v) {
                v2f o;
                o.clipPos = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }
            
            float4 frag (v2f i) : SV_Target {
                float dist = length(i.worldPos - _WorldSpaceCameraPos);
                return EncodeFloatRGBA(dist / _ProjectionParams.z);
            }
            ENDCG
        }
    }
}