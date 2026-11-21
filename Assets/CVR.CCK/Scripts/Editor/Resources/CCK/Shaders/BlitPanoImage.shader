Shader "Hidden/BlitPanoImage" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _FaceIndex ("Face Index", Float) = 0
    }
    SubShader {
        Tags { "RenderType" = "Opaque" }
        
        // Color 1k v2 pano (full uv)
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float _FaceIndex;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 clipPos : SV_POSITION;
                float3 uv : TEXCOORD0;
            };

            static const half3 CUBE_DIR_O[6] = { {1.0, -1.0,  1.0}, {-1.0, -1.0, -1.0}, {-1.0, 1.0,  1.0}, {-1.0, -1.0, -1.0}, {-1.0, -1.0, 1.0}, { 1.0, -1.0, -1.0} };
            static const half3 CUBE_DIR_U[6] = { {0.0,  0.0, -1.0}, { 0.0,  0.0,  1.0}, { 1.0, 0.0,  0.0}, { 1.0,  0.0,  0.0}, { 1.0,  0.0, 0.0}, {-1.0,  0.0,  0.0} };
            static const half3 CUBE_DIR_V[6] = { {0.0,  1.0,  0.0}, { 0.0,  1.0,  0.0}, { 0.0, 0.0, -1.0}, { 0.0,  0.0,  1.0}, { 0.0,  1.0, 0.0}, { 0.0,  1.0,  0.0} };

            v2f vert(appdata v) {
                v2f o;
                o.clipPos = UnityObjectToClipPos(v.vertex);
                o.uv = CUBE_DIR_O[_FaceIndex] + 2.0 * v.uv.x * CUBE_DIR_U[_FaceIndex] + 2.0 * (1.0 - v.uv.y) * CUBE_DIR_V[_FaceIndex];
                return o;
            }

            float2 ConvertUV(float3 dir) {
                const float InvPi = 0.3183098861837907;
                const float InvTwoPi = 0.15915494309189535;
                return float2(atan2(dir.z, dir.x) * InvTwoPi + 0.5, asin(dir.y) * InvPi + 0.5);
            }

            float4 frag(v2f i) : SV_Target {
                i.uv = float3(i.uv.z, i.uv.y, -i.uv.x);
                float2 panoUV = ConvertUV(normalize(i.uv));
                panoUV.x = 1 - panoUV.x;
                return _MainTex.Sample(sampler_MainTex, panoUV);
            }
            ENDCG
        }
        
        // Color 4k v1 & v2 pano (top half uv)
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            Texture2D _MainTex;
            SamplerState sampler_MainTex;
            float _FaceIndex;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 clipPos : SV_POSITION;
                float3 uv : TEXCOORD0;
            };

            static const half3 CUBE_DIR_O[6] = { {1.0, -1.0,  1.0}, {-1.0, -1.0, -1.0}, {-1.0, 1.0,  1.0}, {-1.0, -1.0, -1.0}, {-1.0, -1.0, 1.0}, { 1.0, -1.0, -1.0} };
            static const half3 CUBE_DIR_U[6] = { {0.0,  0.0, -1.0}, { 0.0,  0.0,  1.0}, { 1.0, 0.0,  0.0}, { 1.0,  0.0,  0.0}, { 1.0,  0.0, 0.0}, {-1.0,  0.0,  0.0} };
            static const half3 CUBE_DIR_V[6] = { {0.0,  1.0,  0.0}, { 0.0,  1.0,  0.0}, { 0.0, 0.0, -1.0}, { 0.0,  0.0,  1.0}, { 0.0,  1.0, 0.0}, { 0.0,  1.0,  0.0} };

            v2f vert(appdata v) {
                v2f o;
                o.clipPos = UnityObjectToClipPos(v.vertex);
                o.uv = CUBE_DIR_O[_FaceIndex] + 2.0 * v.uv.x * CUBE_DIR_U[_FaceIndex] + 2.0 * (1.0 - v.uv.y) * CUBE_DIR_V[_FaceIndex];
                return o;
            }

            float2 ConvertUV(float3 dir) {
                const float InvPi = 0.3183098861837907;
                const float InvTwoPi = 0.15915494309189535;
                return float2(atan2(dir.z, dir.x) * InvTwoPi + 0.5, asin(dir.y) * InvPi + 0.5);
            }

            float4 frag(v2f i) : SV_Target {
                i.uv = float3(i.uv.z, i.uv.y, -i.uv.x);
                float2 panoUV = ConvertUV(normalize(i.uv));
                panoUV.x = 1 - panoUV.x;
                panoUV.y = panoUV.y * 0.5 + 0.5;
                return _MainTex.Sample(sampler_MainTex, panoUV);
            }
            ENDCG
        }

        // Depth 4k v2 pano (bottom half uv)
        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            Texture2D _MainTex;
            float _FaceIndex;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f {
                float4 clipPos : SV_POSITION;
                float3 uv : TEXCOORD0;
            };

            static const half3 CUBE_DIR_O[6] = { {1.0, -1.0,  1.0}, {-1.0, -1.0, -1.0}, {-1.0, 1.0,  1.0}, {-1.0, -1.0, -1.0}, {-1.0, -1.0, 1.0}, { 1.0, -1.0, -1.0} };
            static const half3 CUBE_DIR_U[6] = { {0.0,  0.0, -1.0}, { 0.0,  0.0,  1.0}, { 1.0, 0.0,  0.0}, { 1.0,  0.0,  0.0}, { 1.0,  0.0, 0.0}, {-1.0,  0.0,  0.0} };
            static const half3 CUBE_DIR_V[6] = { {0.0,  1.0,  0.0}, { 0.0,  1.0,  0.0}, { 0.0, 0.0, -1.0}, { 0.0,  0.0,  1.0}, { 0.0,  1.0, 0.0}, { 0.0,  1.0,  0.0} };

            v2f vert(appdata v) {
                v2f o;
                o.clipPos = UnityObjectToClipPos(v.vertex);
                o.uv = CUBE_DIR_O[_FaceIndex] + 2.0 * v.uv.x * CUBE_DIR_U[_FaceIndex] + 2.0 * (1.0 - v.uv.y) * CUBE_DIR_V[_FaceIndex];
                return o;
            }

            float2 ConvertUV(float3 dir) {
                const float InvPi = 0.3183098861837907;
                const float InvTwoPi = 0.15915494309189535;
                return float2(atan2(dir.z, dir.x) * InvTwoPi + 0.5, asin(dir.y) * InvPi + 0.5);
            }

            float4 frag(v2f i) : SV_Target {
                i.uv = float3(i.uv.z, i.uv.y, -i.uv.x);
                float2 panoUV = ConvertUV(normalize(i.uv));
                panoUV.x = 1 - panoUV.x;
                panoUV.y *= 0.5;
                uint2 size;
                _MainTex.GetDimensions(size.x, size.y);
                uint3 uv = uint3(panoUV * size, 0);
                float depth = DecodeFloatRGBA(_MainTex.Load(uv)) * 1000;
                return depth * depth;
            }
            ENDCG
        }
    }
}