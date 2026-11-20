Shader "Sprites/WinterSnowGround2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0

        [Header(Snow Surface Region)]
        _SnowMinY ("Snow Crust Bottom UV Y", Range(0.0, 1.0)) = 0.036
        _SnowMaxY ("Snow Ridge Top UV Y", Range(0.02, 1.0)) = 0.088
        _SnowSoftness ("Frost Edge Softness", Range(0.005, 0.25)) = 0.018
        _AspectRatio ("Texture Aspect (Width / Height)", Float) = 1.7778

        [Header(Fresh Snow Frost and Accumulation)]
        _FrostColor ("Fresh Snow Frost Color", Color) = (0.90, 0.96, 1.0, 1.0)
        _FrostIntensity ("Fresh Frost Tint Intensity", Range(0.0, 1.0)) = 0.34
        _SnowGlowColor ("Snow Impact & Flake Color", Color) = (0.94, 0.98, 1.0, 1.0)

        [Header(Falling Snow Hitting Ground)]
        _LandingIntensity ("Snow Landing Puff Intensity", Range(0.0, 1.5)) = 0.65
        _LandingDensity ("Snow Landing Cell Density", Range(8.0, 80.0)) = 38.0
        _LandingSpeed ("Snowfall Impact Speed", Range(0.2, 4.0)) = 1.15
        _AirFlakeIntensity ("Airborne Snow Above Ridge", Range(0.0, 1.0)) = 0.72

        [Header(Wind Drifted Snow Powder)]
        _DriftColor ("Wind Drift Mist Color", Color) = (0.85, 0.94, 1.0, 1.0)
        _DriftIntensity ("Wind Drift Powder Intensity", Range(0.0, 0.8)) = 0.22
        _DriftSpeed ("Wind Drift Speed", Range(-5.0, 5.0)) = 1.45

        [Header(Moonlit Ice Crystal Sparkles)]
        _SparkleIntensity ("Ice Sparkle Intensity", Range(0.0, 1.5)) = 0.52
        _SparkleDensity ("Ice Sparkle Density", Range(10.0, 90.0)) = 46.0
        _SparkleSpeed ("Ice Sparkle Speed", Range(0.5, 6.0)) = 2.1
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

            float _SnowMinY;
            float _SnowMaxY;
            float _SnowSoftness;
            float _AspectRatio;

            fixed4 _FrostColor;
            float _FrostIntensity;
            fixed4 _SnowGlowColor;

            float _LandingIntensity;
            float _LandingDensity;
            float _LandingSpeed;
            float _AirFlakeIntensity;

            fixed4 _DriftColor;
            float _DriftIntensity;
            float _DriftSpeed;

            float _SparkleIntensity;
            float _SparkleDensity;
            float _SparkleSpeed;

            float2 Hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // เกล็ดหิมะร่วงลงมาแตะพื้นแล้วแตกฟุ้งจางหายไปบนผิวหิมะ
            void ComputeSnowLanding(float2 uv, float time, out float fallingFlake, out float landingPuff)
            {
                float2 aspectUV = float2(uv.x * _AspectRatio, uv.y);
                float2 scaledUV = aspectUV * _LandingDensity;
                float2 cell = floor(scaledUV);
                float2 local = frac(scaledUV) - 0.5;

                fallingFlake = 0.0;
                landingPuff = 0.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 rnd = Hash22(cell + neighbor);

                        // แบ่งจังหวะ 0..0.62 = เกล็ดหิมะปลิวลงมา, 0.58..1.0 = กระทบพื้นฟุ้งกระจายแล้วละลาย
                        float phase = frac(time * _LandingSpeed * (0.75 + rnd.x * 0.55) + rnd.y * 6.2831);
                        float fallProgress = saturate(phase / 0.62);
                        float impactProgress = saturate((phase - 0.56) / 0.44);

                        float driftX = sin(time * 2.2 + rnd.x * 12.0) * 0.14 * (1.0 - fallProgress);
                        float2 targetPos = neighbor + (rnd - 0.5) * 0.55;
                        float2 flakePos = targetPos + float2(driftX, (1.0 - fallProgress) * 0.85);

                        float distFlake = length(flakePos - local);
                        float flakeAlpha = (1.0 - smoothstep(0.05, 0.14, distFlake)) * (1.0 - smoothstep(0.55, 0.68, phase));
                        fallingFlake += flakeAlpha;

                        if (phase > 0.54)
                        {
                            float distImpact = length(targetPos - local);
                            float puffRadius = lerp(0.04, 0.34, impactProgress);
                            float puffRing = smoothstep(0.14, 0.0, abs(distImpact - puffRadius * 0.7));
                            float puffCore = (1.0 - smoothstep(0.0, puffRadius, distImpact)) * 0.75;
                            float puffFade = (1.0 - impactProgress) * (1.0 - impactProgress);
                            landingPuff += (puffRing * 0.6 + puffCore) * puffFade;
                        }
                    }
                }

                fallingFlake = saturate(fallingFlake);
                landingPuff = saturate(landingPuff);
            }

            // ประกายเกล็ดน้ำแข็งวิบวับบนพื้นหิมะใต้แสงจันทร์
            float ComputeIceSparkles(float2 uv, float time)
            {
                float2 aspectUV = float2(uv.x * _AspectRatio, uv.y);
                float2 scaledUV = aspectUV * _SparkleDensity;
                float2 cell = floor(scaledUV);
                float2 local = frac(scaledUV) - 0.5;

                float2 rnd = Hash22(cell);
                if (rnd.x < 0.58) return 0.0;

                float2 center = (rnd - 0.5) * 0.55;
                float dist = length(local - center);
                float twinkle = pow(saturate(sin(time * _SparkleSpeed + rnd.y * 25.0) * 0.5 + 0.5), 4.0);
                float glint = smoothstep(0.16, 0.0, dist);
                return glint * twinkle;
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
                float t = _Time.y;

                fixed4 c = tex2D(_MainTex, uv) * IN.color;

                // Mask โซนชั้นหิมะด้านบนของพื้น (_SnowMinY .. _SnowMaxY)
                float crustMask = smoothstep(_SnowMinY - _SnowSoftness, _SnowMinY + _SnowSoftness, uv.y);
                float ridgeMask = 1.0 - smoothstep(_SnowMaxY - _SnowSoftness, _SnowMaxY + _SnowSoftness, uv.y);
                float snowSurfaceMask = saturate(crustMask * ridgeMask);

                // 1. ย้อมผิวหิมะด้านบนให้ขาวเย็นฉ่ำขึ้นเล็กน้อยเหมือนหิมะใหม่เพิ่งตกทับ
                c.rgb = lerp(c.rgb, _FrostColor.rgb, snowSurfaceMask * _FrostIntensity);

                // 2. ไอหิมะถูกลมพัดฟุ้งเลียดผิวพื้นหิมะ
                float driftWave1 = sin(uv.x * 36.0 - t * _DriftSpeed + uv.y * 85.0);
                float driftWave2 = cos(uv.x * 19.0 - t * (_DriftSpeed * 1.35) - uv.y * 50.0);
                float driftPowder = smoothstep(0.25, 0.95, 0.5 + 0.5 * (driftWave1 * 0.6 + driftWave2 * 0.4));
                c.rgb = lerp(c.rgb, _DriftColor.rgb, driftPowder * _DriftIntensity * snowSurfaceMask * c.a);

                // 3. เกล็ดหิมะร่วงลงแตะพื้น & แตกฟุ้งจางหายบนผิวหิมะ
                float fallingFlake;
                float landingPuff;
                ComputeSnowLanding(uv, t, fallingFlake, landingPuff);

                float surfaceEffects = saturate(landingPuff * _LandingIntensity + fallingFlake * 0.45) * snowSurfaceMask;
                c.rgb = lerp(c.rgb, _SnowGlowColor.rgb, surfaceEffects);

                // 4. ประกายเกล็ดน้ำแข็งวิบวับบนพื้นหิมะ
                float sparkles = ComputeIceSparkles(uv, t) * snowSurfaceMask;
                c.rgb += _SnowGlowColor.rgb * (sparkles * _SparkleIntensity * c.a);

                // 5. เกล็ดหิมะที่กำลังปลิวเหนือสันหิมะเล็กน้อยก่อนตกถึงพื้น (ในแถบโปร่งใสเหนือ _SnowMinY .. _SnowMaxY)
                float airBand = smoothstep(_SnowMinY, _SnowMaxY, uv.y) * (1.0 - smoothstep(_SnowMaxY, _SnowMaxY + _SnowSoftness * 1.5, uv.y));
                float airFlakeAlpha = saturate(fallingFlake * _AirFlakeIntensity * airBand * (1.0 - c.a));

                fixed3 premulRGB = c.rgb * c.a + _SnowGlowColor.rgb * airFlakeAlpha;
                float outAlpha = saturate(c.a + airFlakeAlpha);

                return fixed4(premulRGB, outAlpha);
            }
            ENDCG
        }
    }
}
