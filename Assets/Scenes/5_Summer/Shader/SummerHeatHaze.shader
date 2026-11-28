Shader "Pooyan/SummerHeatHaze"
{
    Properties
    {
        [PerRendererData] _MainTex ("Mask / Sprite Texture", 2D) = "white" {}
        _Color ("Tint / Master Opacity (Alpha)", Color) = (1, 1, 1, 1)

        [Header(Heat Shimmer)]
        _DistortionStrength ("Distortion Strength", Range(0.0, 0.03)) = 0.004
        _RiseSpeed ("Heat Rise Speed", Range(0.0, 6.0)) = 1.65
        _WaveScale ("World Wave Scale", Range(0.2, 10.0)) = 2.2
        _BottomHeatBoost ("Bottom Ground Heat Boost", Range(0.0, 2.0)) = 0.65
        _TopFadeStart ("Top Fade Start (UV Y)", Range(0.0, 1.0)) = 0.55
        _TopFadeEnd ("Top Fade End (UV Y)", Range(0.0, 1.0)) = 0.95
        _EdgeFeather ("Edge Softness", Range(0.0, 0.5)) = 0.08

        [Header(Summer Warmth Atmosphere)]
        _WarmTintColor ("Warm Atmosphere Tint", Color) = (1.0, 0.92, 0.76, 1.0)
        _WarmthStrength ("Warm Tint Strength", Range(0.0, 0.6)) = 0.22
        _HazeGlowColor ("Lower Heat Glow Color", Color) = (1.0, 0.78, 0.42, 1.0)
        _HazeGlowStrength ("Lower Heat Glow Strength", Range(0.0, 0.5)) = 0.09
        _ShimmerStrength ("Thermal Shimmer Highlight", Range(0.0, 0.25)) = 0.045
        _WarmSaturation ("Warm Saturation", Range(0.8, 1.5)) = 1.08
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
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
                float4 flowCoords : TEXCOORD1;
                float4 grabPos : TEXCOORD2;
                float2 microWaveCoord : TEXCOORD3;
            };

            sampler2D _MainTex;
            sampler2D _SummerHeatGrabTex;
            float4 _SummerHeatGrabTex_TexelSize;

            fixed4 _Color;
            float _DistortionStrength;
            float _RiseSpeed;
            float _WaveScale;
            float _BottomHeatBoost;
            float _TopFadeStart;
            float _TopFadeEnd;
            float _EdgeFeather;

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

                // คำนวณพิกัดไหลของคลื่นความร้อนไว้ใน Vertex Shader เพื่อลดภาระต่อพิกเซลใน Fragment Shader
                float2 worldPos = mul(unity_ObjectToWorld, v.vertex).xy;
                float t = _Time.y * _RiseSpeed;
                float scaledWave = _WaveScale * 6.2831853;
                o.flowCoords.xy = float2(worldPos.x * scaledWave, worldPos.y * (scaledWave * 0.72) - t * 3.1);
                o.flowCoords.zw = float2(worldPos.x * (scaledWave * 1.85) + 3.17, worldPos.y * (scaledWave * 1.25) - t * 4.5);
                o.microWaveCoord = float2(worldPos.y * (_WaveScale * 4.5) - t * 6.2, worldPos.x * (_WaveScale * 1.35));

                o.color = v.color * _Color;
                o.grabPos = ComputeGrabScreenPos(o.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.uv;
                fixed texAlpha = tex2D(_MainTex, uv).a;

                half edgeMask = 1.0h;
                if (_EdgeFeather > 0.0001)
                {
                    half invFeather = (half)(1.0 / _EdgeFeather);
                    half2 edgeMin = saturate((half2)uv * invFeather);
                    half2 edgeMax = saturate((1.0h - (half2)uv) * invFeather);
                    half2 edge = edgeMin * edgeMin * (3.0h - 2.0h * edgeMin) * edgeMax * edgeMax * (3.0h - 2.0h * edgeMax);
                    edgeMask = edge.x * edge.y;
                }

                half masterMask = (half)texAlpha * edgeMask * i.color.a;
                if (masterMask <= 0.001h)
                    return fixed4(0, 0, 0, 0);

                // มาสก์ความเข้มแนวตั้ง: ไอร้อนลอยแรงจากพื้นด้านล่างและค่อยๆ จางลงด้านบน
                float topFadeEnd = max(_TopFadeStart + 0.01, _TopFadeEnd);
                half verticalMask = (half)(1.0 - smoothstep(_TopFadeStart, topFadeEnd, uv.y));
                half groundBoost = 1.0h + (half)_BottomHeatBoost * (half)(1.0 - smoothstep(0.0, 0.38, uv.y));

                float2 grabUV = i.grabPos.xy / max(0.0001, i.grabPos.w);
                float2 minUV = _SummerHeatGrabTex_TexelSize.xy * 0.5;
                float2 maxUV = 1.0 - minUV;

                half shimmerCrest = 0.0h;
                float2 sampleUV = clamp(grabUV, minUV, maxUV);

                // ข้ามการคำนวณคลื่นรบกวนในโซนบนที่จางหายไปแล้ว เพื่อลดภาระ GPU
                if (verticalMask > 0.001h)
                {
                    half waveMask = verticalMask * groundBoost * masterMask;

                    // คลื่นความร้อน 2 ชั้นแบบ Trigonometric Interference (ลื่นกว่า ValueNoise และกิน ALU น้อยกว่ามาก)
                    half n1 = (half)(sin(i.flowCoords.x + sin(i.flowCoords.y * 0.85)) * 0.32 + cos(i.flowCoords.y + i.flowCoords.x * 0.45) * 0.18);
                    half n2 = (half)(sin(i.flowCoords.z - cos(i.flowCoords.w * 0.9)) * 0.30 + sin(i.flowCoords.w + i.flowCoords.z * 0.5) * 0.20);
                    half microWave = (half)(sin(i.microWaveCoord.x + n1 * 4.2 + sin(i.microWaveCoord.y)) * 0.28);

                    float2 offset;
                    offset.x = (float)(n1 * 0.65h + n2 * 0.35h + microWave * 0.30h) * _DistortionStrength * (float)waveMask;
                    offset.y = (float)(n2 * 0.55h - n1 * 0.25h) * (_DistortionStrength * 0.45) * (float)waveMask;
                    sampleUV = clamp(grabUV + offset, minUV, maxUV);

                    half shimmerRaw = saturate(n1 * 0.7h + n2 * 0.6h + 0.45h);
                    shimmerCrest = shimmerRaw * shimmerRaw;
                }

                fixed4 col = tex2D(_SummerHeatGrabTex, sampleUV);

                // ปรับบรรยากาศร้อนอบอ้าว (เพิ่มความอิ่มสี + โทนแดดอุ่น + ไอร้อนฟุ้งช่วงล่าง + ประกายคลื่นความร้อน)
                half luma = dot(col.rgb, fixed3(0.299, 0.587, 0.114));
                col.rgb = lerp(luma.xxx, col.rgb, lerp(1.0h, (half)_WarmSaturation, masterMask));

                half3 warmTinted = col.rgb * _WarmTintColor.rgb;
                col.rgb = lerp(col.rgb, warmTinted, (half)_WarmthStrength * masterMask);

                half groundHeatVeil = (half)(1.0 - smoothstep(0.0, 0.75, uv.y));
                col.rgb += _HazeGlowColor.rgb * (groundHeatVeil * (half)_HazeGlowStrength * masterMask);

                if (shimmerCrest > 0.001h)
                {
                    half hazeMask = saturate(verticalMask * groundBoost * 0.72h + 0.14h);
                    col.rgb += _HazeGlowColor.rgb * (shimmerCrest * hazeMask * (half)_ShimmerStrength * masterMask);
                }

                col.a = masterMask;
                return col;
            }
            ENDCG
        }
    }

    Fallback Off
}
