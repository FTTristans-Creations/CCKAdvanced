Shader "Hidden/RenderPanoDepth" {
    Properties {}
    SubShader {
        Tags { "RenderType" = "Opaque" "Queue"="Overlay+1000" }
        ZWrite Off
        ZTest Always
        Cull Off
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _CameraDepthTexture;

            struct appdata {
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 clipPos : SV_POSITION;
                float2 uv : TEXCOORD1;
            };

            v2f vert(appdata v) {
                v2f o;
                o.clipPos = float4(v.uv * float2(2, -2) + float2(-1, 1), 1, 1);
                o.uv = v.uv;
                return o;
            }

            float4x4 CreateClipToViewMatrix() {
                float4x4 flipZ = float4x4(1, 0, 0, 0,
                                          0, 1, 0, 0,
                                          0, 0, -1, 1,
                                          0, 0, 0, 1);
                float4x4 scaleZ = float4x4(1, 0, 0, 0,
                                           0, 1, 0, 0,
                                           0, 0, 2, -1,
                                           0, 0, 0, 1);
                float4x4 invP = unity_CameraInvProjection;
                float4x4 flipY = float4x4(1, 0, 0, 0,
                                          0, _ProjectionParams.x, 0, 0,
                                          0, 0, 1, 0,
                                          0, 0, 0, 1);

                float4x4 result = mul(scaleZ, flipZ);
                result = mul(invP, result);
                result = mul(flipY, result);
                result._24 *= _ProjectionParams.x;
                result._42 *= -1;
                return result;
            }
            
            float4 frag(v2f i) : SV_Target {
                float depth = LinearEyeDepth(tex2D(_CameraDepthTexture, i.uv).r);

                if (depth > _ProjectionParams.z) return 1;
                
                float4 viewPos = mul(CreateClipToViewMatrix(), float4(i.uv * 2.0 - 1.0, depth, 1.0));
                viewPos.xyz *= viewPos.w;
                
                return EncodeFloatRGBA(length(viewPos.xyz) / _ProjectionParams.z / 100);
            }
            ENDCG
        }
    }
}