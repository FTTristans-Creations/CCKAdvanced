Shader "Custom/PanoInsideSplit"
{
    Properties
    {
        _MainTex ("Panorama (Top = Left, Bottom = Right)", 2D) = "white" {}
        _Side ("Eye Side (0 = Left, 1 = Right)", Float) = 0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
        Cull Front
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _Side;

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 worldDir : TEXCOORD0;
            };

            v2f vert(appdata v)
            {
                v2f o;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldDir = normalize(worldPos - _WorldSpaceCameraPos);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 dir = normalize(i.worldDir);

                float2 pano_uv;
                pano_uv.x = 0.5 + atan2(dir.x, dir.z) / (2.0 * UNITY_PI);
                pano_uv.y = acos(dir.y) / UNITY_PI;
                pano_uv.y = lerp(pano_uv.y * 0.5, 0.5 + pano_uv.y * 0.5, _Side);
                pano_uv.y = 1.0 - pano_uv.y;

                float4 uv = float4(pano_uv, 0, 0); // .z = mip level = 0
                return tex2Dlod(_MainTex, uv);
            }
            ENDCG
        }
    }
}
