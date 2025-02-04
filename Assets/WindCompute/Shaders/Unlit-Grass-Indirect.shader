Shader "Unlit/Grass-Indirect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _Offset ("Offset", Vector) = (1,1,1,1)
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

            #include "UnityCG.cginc"
            #define UNITY_INDIRECT_DRAW_ARGS IndirectDrawIndexedArgs
            #include "UnityIndirect.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR0;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _Offset;

            StructuredBuffer<float4x4> _GrassMatrixBuffer;
            StructuredBuffer<float> _WindBuffer;

            v2f vert(appdata v, uint svInstanceID : SV_InstanceID)
            {
                v2f o;

                InitIndirectDrawArgs(0);
                uint instanceID = GetIndirectInstanceID(svInstanceID);

                float wind = _WindBuffer[instanceID] * UNITY_PI;
                const float4x4 windMatrix = float4x4(
                    cos(wind), -sin(wind), 0, 0,
                    sin(wind), cos(wind), 0, 0,
                    0, 0, 1, 0,
                    0, 0, 0, 1
                );

                const float4 vertex = lerp(v.vertex, mul(windMatrix, v.vertex), v.texcoord.y);
                const float4 wpos = mul(_GrassMatrixBuffer[instanceID], vertex);

                o.vertex = mul(UNITY_MATRIX_VP, wpos);
                o.texcoord = v.texcoord;
                o.color = _Color * v.color;

                //Debug
                //float w = _WindBuffer[instanceID];
                //o.color = fixed4(saturate(w), saturate(-w), 0, o.color.a);

                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 coord = float2(i.texcoord.x, i.texcoord.y);
                fixed4 c = tex2D(_MainTex, coord) * i.color;
                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}