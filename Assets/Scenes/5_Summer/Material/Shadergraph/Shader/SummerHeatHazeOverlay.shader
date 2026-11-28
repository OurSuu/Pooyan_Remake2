Shader "Pooyan/SummerHeatHazeOverlay"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1, 0.90, 0.68, 1)
        _DistortionStrength ("Distortion Strength", Range(0.0, 0.03)) = 0.0045
        _RiseSpeed ("Heat Rise Speed", Range(0.0, 6.0)) = 1.8
        _WaveScaleX ("Wave Scale X", Range(2.0, 50.0)) = 14.0
        _WaveScaleY ("Wave Scale Y", Range(2.0, 50.0)) = 10.0
        _EdgeFeather ("Edge Softness", Range(0.01, 0.5)) = 0.22
        _WarmOverlayStrength ("Warm Air Tint Strength", Range(0.0, 0.5)) = 0.14
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent+20"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        GrabPass { "_SummerHeatGrabTex" }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                fixed4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 grabPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            sampler2D _SummerHeatGrabTex;
            float4 _SummerHeatGrabTex_TexelSize;

            fixed4 _Color;
            float _DistortionStrength;
            float _RiseSpeed;
            float _WaveScaleX;
            float _WaveScaleY;
            float _EdgeFeather;
            float _WarmOverlayStrength;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f);

                float a = Hash21(i);
                float b = Hash21(i + float2(1.0, 0.0));
                float c = Hash21(i + float2(0.0, 1.0));
                float d = Hash21(i + float2(1.0, 1.0));

                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed texAlpha = tex2D(_MainTex, i.uv).a;

                // ขอบนุ่มรอบแผ่น เพื่อไม่ให้เห็นรอยต่อสี่เหลี่ยมเวลานำไปวางเฉพาะจุดในฉาก
                float edgeX = smoothstep(0.0, _EdgeFeather, i.uv.x) * smoothstep(0.0, _EdgeFeather, 1.0 - i.uv.x);
                float edgeY = smoothstep(0.0, _EdgeFeather, i.uv.y) * smoothstep(0.0, _EdgeFeather, 1.0 - i.uv.y);
                float mask = texAlpha * edgeX * edgeY * i.color.a;

                float t = _Time.y * _RiseSpeed;
                float n1 = ValueNoise(float2(i.uv.x * _WaveScaleX, i.uv.y * _WaveScaleY - t)) - 0.5;
                float n2 = ValueNoise(float2(i.uv.x * (_WaveScaleX * 1.8) + 2.3, i.uv.y * (_WaveScaleY * 1.6) - t * 1.4)) - 0.5;

                float2 offset;
                offset.x = (n1 * 0.65 + n2 * 0.35) * _DistortionStrength * mask;
                offset.y = (n2 * 0.45 - n1 * 0.25) * _DistortionStrength * mask;

                float2 grabUV = i.grabPos.xy / max(0.0001, i.grabPos.w);
                float2 minUV = _SummerHeatGrabTex_TexelSize.xy * 0.5;
                float2 maxUV = 1.0 - minUV;
                float2 sampleUV = clamp(grabUV + offset, minUV, maxUV);

                fixed4 bg = tex2D(_SummerHeatGrabTex, sampleUV);
                bg.rgb = lerp(bg.rgb, bg.rgb * i.color.rgb + i.color.rgb * 0.15, _WarmOverlayStrength * mask);
                bg.a = mask;

                return bg;
            }
            ENDCG
        }
    }
}
