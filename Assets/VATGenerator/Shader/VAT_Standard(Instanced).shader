Shader "VAT/Standard(Instanced)"
{
    Properties
    {
        _Color      ("Color",        Color)      = (1, 1, 1, 1)
        _MainTex    ("Albedo (RGB)", 2D)         = "white" {}
        _Glossiness ("Smoothness",   Range(0,1)) = 0.5
        _Metallic   ("Metallic",     Range(0,1)) = 0.0

        _AnimTex           ("PosTex",     2D)     = "white" {} 
        _AnimTex_NormalTex ("Normal Tex", 2D)     = "white" {}
        _AnimTex_Scale     ("Scale",      Vector) = (1, 1, 1, 1)
        _AnimTex_Offset    ("Offset",     Vector) = (0, 0, 0, 0)
        _AnimTex_AnimEnd   ("End(t,f)",   Vector) = (0, 0, 0, 0)
        _AnimTex_Time      ("Time",       Float)  = 0
        _AnimTex_FPS       ("FPS",        Float)  = 30
        [Toggle]
        _AnimTex_Repeat    ("Repeat",     Float)  = 0
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" }
        LOD 200

        CGPROGRAM

        #pragma surface surf Standard fullforwardshadows addshadow vertex:vert
        #pragma multi_compile_instancing

        #include "VAT.cginc"

        struct appdata
        {
            float4 vertex    : POSITION;
            float4 tangent   : TANGENT;
            float3 normal    : NORMAL;
            float4 texcoord  : TEXCOORD0;
            float4 texcoord1 : TEXCOORD1;
            float4 texcoord2 : TEXCOORD2;
            float4 texcoord3 : TEXCOORD3;
            #if defined(SHADER_API_XBOX360)
            half4  texcoord4 : TEXCOORD4;
            half4  texcoord5 : TEXCOORD5;
            #endif
            fixed4 color : COLOR;
            UNITY_VERTEX_INPUT_INSTANCE_ID
            uint vid : SV_VertexID;
        };

        struct Input
        {
            float2 uv_MainTex;
        };

        sampler2D _MainTex;
        half      _Glossiness;
        half      _Metallic;
        fixed4    _Color;

        void vert(inout appdata v)
        {
            float t = UNITY_ACCESS_INSTANCED_PROP(_AnimTex_T_arr, _AnimTex_Time);
                  t = _AnimTex_Repeat ? clamp(t % _AnimTex_AnimEnd.x, 0, _AnimTex_AnimEnd.x)
                                      : clamp(t, 0, _AnimTex_AnimEnd.x);

            v.vertex.xyz = AnimTexVertexPos(v.vid, t);
            v.normal     = normalize(AnimTexNormal(v.vid, t));
        }

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;

            o.Albedo     = c.rgb;
            o.Metallic   = _Metallic;
            o.Smoothness = _Glossiness;
            o.Alpha      = c.a;
        }

        ENDCG
    }

    FallBack "Diffuse"
}