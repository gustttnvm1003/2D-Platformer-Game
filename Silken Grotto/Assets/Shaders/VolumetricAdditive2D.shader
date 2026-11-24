Shader "SilkenGrotto/VolumetricAdditive2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _PulseSpeed ("Pulse Speed", Float) = 0.75
        _PulseAmp ("Pulse Amplitude", Range(0, 0.5)) = 0.18
        _DriftSpeed ("Subtle Shimmer Drift", Float) = 0.08
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
        Fog { Mode Off }
        Blend SrcAlpha One

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float _PulseSpeed;
            float _PulseAmp;
            float _DriftSpeed;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                fixed4 texCol = tex2D(_MainTex, uv);
                float wave = sin(_Time.y * _PulseSpeed + uv.x * 6.283185 + uv.y * 1.4) * 0.5 + 0.5;
                float shimmer = 1.0 - _PulseAmp + (_PulseAmp * 2.0) * wave;
                fixed4 outCol = texCol * IN.color;
                outCol.a *= shimmer;
                return outCol;
            }
            ENDCG
        }
    }
}
