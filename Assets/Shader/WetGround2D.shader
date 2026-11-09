Shader "Sprites/WetGround2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0

        [Header(Wet Ground Region)]
        _WetHeightMax ("Wet Ground Top UV (0-1)", Range(0.0, 1.0)) = 0.22
        _WetFadeSoftness ("Wet Edge Softness", Range(0.01, 0.35)) = 0.08
        _WetDarken ("Wet Soil Darkening", Range(0.0, 0.5)) = 0.14

        [Header(Water Sheen and Reflection)]
        _SheenColor ("Wet Sheen Color", Color) = (0.78, 0.90, 1.0, 1.0)
        _SheenIntensity ("Wet Sheen Intensity", Range(0.0, 1.0)) = 0.24
        _WaveDistortion ("Water Wave Distortion", Range(0.0, 0.02)) = 0.003
        _WaveSpeed ("Water Wave Speed", Range(0.0, 10.0)) = 2.2

        [Header(Raindrop Puddle Ripples)]
        _RippleIntensity ("Rain Ripple Intensity", Range(0.0, 1.0)) = 0.32
        _RippleDensity ("Rain Ripple Density", Range(4.0, 40.0)) = 18.0
        _RippleSpeed ("Rain Ripple Speed", Range(0.5, 6.0)) = 2.4
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
            #pragma multi_compile _ PIXELSNAP_ON
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
                float3 worldPos : TEXCOORD1;
            };

            sampler2D _MainTex;
            fixed4 _Color;

            float _WetHeightMax;
            float _WetFadeSoftness;
            float _WetDarken;

            fixed4 _SheenColor;
            float _SheenIntensity;
            float _WaveDistortion;
            float _WaveSpeed;

            float _RippleIntensity;
            float _RippleDensity;
            float _RippleSpeed;

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // คำนวณวงคลื่นน้ำกระเพื่อม (Raindrop Ripples) บนพื้นเปียก
            float ComputeRainRipples(float2 uv, float time)
            {
                // บีบแกน Y ให้วงคลื่นดูแบนตามมุมมอง Perspective ของพื้นดิน 2D
                float2 scaledUV = float2(uv.x * _RippleDensity, uv.y * _RippleDensity * 3.2);
                float2 cell = floor(scaledUV);
                float2 local = frac(scaledUV) - 0.5;

                float rippleSum = 0.0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 rnd = Hash22(cell + neighbor);
                        float t = frac(time * _RippleSpeed + rnd.x);
                        float2 diff = neighbor + (rnd - 0.5) * 0.65 - local;
                        float dist = length(diff);

                        float radius = t * 0.65;
                        float ring = smoothstep(0.08, 0.0, abs(dist - radius));
                        float fade = (1.0 - t) * (1.0 - t);
                        rippleSum += ring * fade;
                    }
                }
                return saturate(rippleSum);
            }

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.worldPos = mul(unity_ObjectToWorld, IN.vertex).xyz;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // Mask เฉพาะโซนพื้นด้านล่างของภาพ (หรือทั้งภาพหากตั้ง _WetHeightMax = 1)
                float wetMask = 1.0 - smoothstep(_WetHeightMax - _WetFadeSoftness, _WetHeightMax + _WetFadeSoftness, uv.y);

                // บิดคลื่นน้ำเบาๆ เฉพาะบริเวณพื้นเปียก
                float wave = sin(uv.y * 85.0 + _Time.y * _WaveSpeed) * cos(uv.x * 45.0 - _Time.y * (_WaveSpeed * 0.7));
                float2 distortedUV = uv + float2(wave * _WaveDistortion * wetMask, 0.0);

                fixed4 c = tex2D(_MainTex, distortedUV) * IN.color;

                // 1. ทำให้ดินที่เปียกดูเข้มและอิ่มสีขึ้นเล็กน้อยเหมือนดินชุ่มน้ำ
                c.rgb *= lerp(1.0, 1.0 - _WetDarken, wetMask);

                // 2. เพิ่มความเงาสะท้อนน้ำ (Wet Specular Sheen) เป็นริ้วคลื่นจางๆ บนพื้น
                float sheenBand = 0.5 + 0.5 * sin(uv.x * 28.0 + uv.y * 60.0 + _Time.y * (_WaveSpeed * 0.8));
                sheenBand = smoothstep(0.35, 0.95, sheenBand);
                c.rgb = lerp(c.rgb, _SheenColor.rgb, sheenBand * _SheenIntensity * wetMask);

                // 3. เพิ่มวงคลื่นเม็ดฝนตกกระทบพื้นเปียก (Raindrop Puddle Ripples)
                float ripples = ComputeRainRipples(uv, _Time.y);
                c.rgb += _SheenColor.rgb * (ripples * _RippleIntensity * wetMask);

                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
