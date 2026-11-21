Shader "Unlit/Portal" {
    Properties {
        _PanoColor ("Color", Cube) = "white" {}
        _PanoDepth ("Depth", Cube) = "black" {}
        _Scale ("Portal Scale", Float) = 1
        _Fade ("Fade", Float) = 0
    }
    SubShader {
        Pass {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            TextureCube _PanoColor;
            SamplerState sampler_PanoColor;
            TextureCube _PanoDepth;
            SamplerState sampler_PanoDepth;
            float _Scale;

            struct appdata {
                float4 vertex : POSITION;
                float3 normal :NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 clipPos : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 hash33(uint3 x) {
                const uint k = 1103515245U;
                x = (x >> 8 ^ x.yzx) * k;
                x = (x >> 8 ^ x.yzx) * k;
                x = (x >> 8 ^ x.yzx) * k;
                return x * (1.0 / float(0xffffffffU));
            }
            
            float noise(float3 p) {
                uint3 i = asuint((int3)floor(p));
                float3 f = frac(p);
	            
	            float3 u = f * f * (3.0 - 2.0 * f);

                float c0 = dot(hash33(i + uint3(0, 0, 0)), f - float3(0, 0, 0));
                float c1 = dot(hash33(i + uint3(1, 0, 0)), f - float3(1, 0, 0));
                float c2 = dot(hash33(i + uint3(0, 1, 0)), f - float3(0, 1, 0));
                float c3 = dot(hash33(i + uint3(1, 1, 0)), f - float3(1, 1, 0));
                float c4 = dot(hash33(i + uint3(0, 0, 1)), f - float3(0, 0, 1));
                float c5 = dot(hash33(i + uint3(1, 0, 1)), f - float3(1, 0, 1));
                float c6 = dot(hash33(i + uint3(0, 1, 1)), f - float3(0, 1, 1));
                float c7 = dot(hash33(i + uint3(1, 1, 1)), f - float3(1, 1, 1));

                return lerp(
                    lerp(lerp(c0, c1, u.x), lerp(c2, c3, u.x), u.y),
                    lerp(lerp(c4, c5, u.x), lerp(c6, c7, u.x), u.y),
                u.z);
            }

            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                v.vertex.xyz += v.normal * 0.017;
                v.vertex.xyz *= _Scale;
                v.vertex.xyz += v.vertex.xyz * noise(v.vertex.xyz * 4 + _Time.x * 10) * 0.05;
                o.clipPos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag() : SV_Target {
                return float4(2.00784326, 6.27450991, 6.27450991, 1);
            }
            ENDCG
        }
        Pass {
            Cull Front
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            #pragma shader_feature _MONOSCOPIC_PORTAL_ON // User setting
            #pragma shader_feature_local _MONOSCOPIC_PORTAL_OVERRIDE_ON // Override if portal doesn't support

            TextureCube _PanoColor;
            SamplerState sampler_PanoColor;
            TextureCube _PanoDepth;
            SamplerState sampler_PanoDepth;
            float _Scale;
            float _Fade;
            float4x4 _RotationMatrix;

            struct appdata {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f {
                float4 clipPos : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 viewDir : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float3 hash33(uint3 x) {
                const uint k = 1103515245U;
                x = (x >> 8 ^ x.yzx) * k;
                x = (x >> 8 ^ x.yzx) * k;
                x = (x >> 8 ^ x.yzx) * k;
                return x * (1.0 / float(0xffffffffU));
            }
            
            float noise(float3 p) {
                uint3 i = asuint((int3)floor(p));
                float3 f = frac(p);
	            
	            float3 u = f * f * (3.0 - 2.0 * f);

                float c0 = dot(hash33(i + uint3(0, 0, 0)), f - float3(0, 0, 0));
                float c1 = dot(hash33(i + uint3(1, 0, 0)), f - float3(1, 0, 0));
                float c2 = dot(hash33(i + uint3(0, 1, 0)), f - float3(0, 1, 0));
                float c3 = dot(hash33(i + uint3(1, 1, 0)), f - float3(1, 1, 0));
                float c4 = dot(hash33(i + uint3(0, 0, 1)), f - float3(0, 0, 1));
                float c5 = dot(hash33(i + uint3(1, 0, 1)), f - float3(1, 0, 1));
                float c6 = dot(hash33(i + uint3(0, 1, 1)), f - float3(0, 1, 1));
                float c7 = dot(hash33(i + uint3(1, 1, 1)), f - float3(1, 1, 1));

                return lerp(
                    lerp(lerp(c0, c1, u.x), lerp(c2, c3, u.x), u.y),
                    lerp(lerp(c4, c5, u.x), lerp(c6, c7, u.x), u.y),
                u.z);
            }

            v2f vert(appdata v) {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                v.vertex.xyz *= _Scale;
                v.vertex.xyz += v.vertex.xyz * noise(v.vertex.xyz * 4 + _Time.x * 10 + 1) * 0.03;
                
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                o.viewDir = o.worldPos - _WorldSpaceCameraPos;
                
                o.worldPos -= unity_ObjectToWorld._m03_m13_m23; // Offset to center of portal
                o.viewDir -= o.worldPos; // View direction from center of portal
                o.viewDir = mul(_RotationMatrix, o.viewDir); // Rotate view direction
                o.worldPos = mul(_RotationMatrix, o.worldPos);
                o.viewDir += o.worldPos; // Add rotated position to view direction
                o.worldPos += unity_ObjectToWorld._m03_m13_m23; // Offset back to original position
                
                o.clipPos = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target {
                float viewDist = length(_WorldSpaceCameraPos - unity_ObjectToWorld._m03_m13_m23);
                i.viewDir = normalize(i.viewDir);
                float3 pos = i.worldPos - unity_ObjectToWorld._m03_m13_m23;
                float3 objDir = normalize(pos);
                float cos = 1 - dot(i.viewDir, objDir);
                cos *= cos * 2;

                float3 dir = (i.viewDir + objDir * smoothstep(0, 1, saturate(viewDist * 0.3 - 0.2)) * 0.1 * -cos) * 0.1;
                
                #if _MONOSCOPIC_PORTAL_ON
                    fixed4 color = _PanoColor.Sample(sampler_PanoColor, objDir * 0.02 + dir);
                    color = lerp(fixed4(1,1,1,1), color, saturate(_Fade));
                    return color;

                #elif _MONOSCOPIC_PORTAL_OVERRIDE_ON
                    fixed4 color = _PanoColor.Sample(sampler_PanoColor, objDir * 0.02 + dir);
                    color = lerp(fixed4(1,1,1,1), color, saturate(_Fade));
                    return color;

                #else
                    float t = saturate(viewDist * 0.1 - 0.2);
                    uint iterations = lerp(21, 8, t);
                    pos = lerp(pos, dir, t);
                    [loop]
                    for (int j = 0; j < iterations; j++) {
                        if (_PanoDepth.SampleLevel(sampler_PanoDepth, pos, 0).r < dot(pos, pos)) break;
                        pos += dir;
                        if (_PanoDepth.SampleLevel(sampler_PanoDepth, pos, 0).r < dot(pos, pos)) break;
                        pos += dir;
                        if (_PanoDepth.SampleLevel(sampler_PanoDepth, pos, 0).r < dot(pos, pos)) break;
                        pos += dir;
                        if (_PanoDepth.SampleLevel(sampler_PanoDepth, pos, 0).r < dot(pos, pos)) break;
                        pos += dir;
                        if (_PanoDepth.SampleLevel(sampler_PanoDepth, pos, 0).r < dot(pos, pos)) break;
                        pos += dir;
                        if (_PanoDepth.SampleLevel(sampler_PanoDepth, pos, 0).r < dot(pos, pos)) break;
                        pos += dir;
                        dir *= 1.2;
                    }

                    float3 prevPos = pos - dir;
                    float d1 = sqrt(_PanoDepth.SampleLevel(sampler_PanoDepth, pos, 0).r) - length(pos);
                    float d0 = sqrt(_PanoDepth.SampleLevel(sampler_PanoDepth, prevPos, 0).r) - length(prevPos);

                    pos = lerp(prevPos, pos, max(saturate(d0 / (d0 - d1)), t));

                    fixed4 color = _PanoColor.Sample(sampler_PanoColor, pos);
                    color = lerp(fixed4(1,1,1,1), color, saturate(_Fade));
                    return color;
                #endif
            }
            ENDCG
        }
    }
}