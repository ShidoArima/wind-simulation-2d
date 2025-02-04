Shader "Unlit/Transparent-Wind"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _WindTex ("Wind Texture", 2D) = "black" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _AnimParams ("Anim", Vector) = (1, 0, 0, 0)
    }
    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR0;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            sampler2D _WindTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            float4 _AnimParams;
            float4x4 _EffectorMatrix;

            v2f vert(appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                float3 vertex = v.vertex;
                float4 worldPos = mul(unity_ObjectToWorld, float4(vertex.xyz, 1.0));

                float2 windCoord = mul(_EffectorMatrix, worldPos);
                float2 wind = tex2Dlod(_WindTex, float4(windCoord.x, windCoord.y, 0, 0));

                const float mask = 1 - v.texcoord.y;
                vertex.x += wind.x * mask * _AnimParams.x;
                vertex.y += wind.y * mask * _AnimParams.y;
                o.vertex = UnityObjectToClipPos(vertex);

                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = _Color * v.color;

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // sample the texture
                fixed4 c = tex2D(_MainTex, i.texcoord).a * i.color;
                return c;
            }
            ENDCG
        }
    }
}