Shader "Hidden/Pooyan/SummerHeatHaze"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _DistortionStrength ("Distortion Strength", Range(0.0, 0.02)) = 0.0032
        _RiseSpeed ("Heat Rise Speed", Range(0.0, 6.0)) = 1.65
        _WaveScaleX ("Wave Scale X", Range(2.0, 60.0)) = 18.0
        _WaveScaleY ("Wave Scale Y", Range(2.0, 60.0)) = 12.0
        _BottomHeatBoost ("Bottom Ground Heat Boost", Range(0.0, 2.0)) = 0.65
        _TopFadeStart ("Top Fade Start (UV Y)", Range(0.0, 1.0)) = 0.55
        _TopFadeEnd ("Top Fade End (UV Y)", Range(0.0, 1.0)) = 0.92
        _WarmTintColor ("Warm Atmosphere Tint", Color) = (1.0, 0.93, 0.78, 1.0)
        _WarmthStrength ("Warm Tint Strength", Range(0.0, 0.6)) = 0.22
        _HazeGlowColor ("Horizon Heat Glow Color", Color) = (1.0, 0.78, 0.42, 1.0)
        _HazeGlowStrength ("Horizon Heat Glow Strength", Range(0.0, 0.5)) = 0.085
        _ShimmerStrength ("Thermal Shimmer Highlight", Range(0.0, 0.25)) = 0.04
        _WarmSaturation ("Warm Saturation", Range(0.8, 1.5)) = 1.08
    }

    SubShader
    {
        Cull Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float _HeatTime;
            float _DistortionStrength;
            float _RiseSpeed;
            float _WaveScaleX;
            float _WaveScaleY;
            float _BottomHeatBoost;
            float _TopFadeStart;
            float _TopFadeEnd;

            fixed4 _WarmTintColor;
            float _WarmthStrength;
            fixed4 _HazeGlowColor;
            float _HazeGlowStrength;
            float _ShimmerStrength;
            float _WarmSaturation;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
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
                float2 uv = i.uv;
                float t = _HeatTime * _RiseSpeed;

                // มาสก์ความเข้มของไอความร้อน: เข้มสุดช่วงพื้นล่างถึงกลางฉาก และค่อยๆ จางลงด้านบนเพื่อให้ท้องฟ้า/HUD อ่านชัด
                float topFadeEnd = max(_TopFadeStart + 0.01, _TopFadeEnd);
                float verticalMask = 1.0 - smoothstep(_TopFadeStart, topFadeEnd, uv.y);
                float groundBoost = 1.0 + _BottomHeatBoost * (1.0 - smoothstep(0.0, 0.38, uv.y));
                float hazeMask = saturate(verticalMask * groundBoost * 0.72 + 0.14);

                // คลื่นความร้อน 2 ชั้นลอยพุ่งขึ้นด้านบน + ระลอกคลื่นถี่เล็กระดับอากาศ
                float2 flow1 = float2(uv.x * _WaveScaleX, uv.y * _WaveScaleY - t);
                float2 flow2 = float2(uv.x * (_WaveScaleX * 1.85) + 3.17, uv.y * (_WaveScaleY * 1.75) - t * 1.45);

                float n1 = ValueNoise(flow1) - 0.5;
                float n2 = ValueNoise(flow2) - 0.5;
                float microWave = sin(uv.y * 54.0 - t * 6.2 + n1 * 6.2831853) * 0.28;

                float2 offset;
                offset.x = (n1 * 0.65 + n2 * 0.35 + microWave * 0.30) * _DistortionStrength * verticalMask * groundBoost;
                offset.y = (n2 * 0.55 - n1 * 0.25) * (_DistortionStrength * 0.45) * verticalMask * groundBoost;

                // จำกัดขอบ UV ป้องกันขอบจอเพี้ยนหรือซ้ำ
                float2 minUV = _MainTex_TexelSize.xy * 0.5;
                float2 maxUV = 1.0 - minUV;
                float2 distortedUV = clamp(uv + offset, minUV, maxUV);

                fixed4 col = tex2D(_MainTex, distortedUV);

                // ปรับโทนสีให้อบอุ่นสไตล์ฤดูร้อน (แดดอุ่น + ไอร้อนฟุ้งจากด้านล่าง)
                float luma = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                col.rgb = lerp(luma.xxx, col.rgb, _WarmSaturation);

                fixed3 warmTinted = col.rgb * _WarmTintColor.rgb;
                col.rgb = lerp(col.rgb, warmTinted, _WarmthStrength);

                float groundHeatVeil = (1.0 - smoothstep(0.0, 0.75, uv.y));
                col.rgb += _HazeGlowColor.rgb * (groundHeatVeil * _HazeGlowStrength);

                // ประกายไอร้อนจางๆ ลอยขึ้นตามยอดคลื่นความร้อน
                float shimmerCrest = pow(saturate(n1 * 0.7 + n2 * 0.6 + 0.45), 2.4);
                col.rgb += _HazeGlowColor.rgb * (shimmerCrest * hazeMask * _ShimmerStrength);

                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
