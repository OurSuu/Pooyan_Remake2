Shader "Sprites/AutumnWindGround2D"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0

        [Header(Grass Region Anchoring)]
        _GrassRootY ("Grass Root UV Y (Pinned = 0 sway)", Range(0.0, 0.95)) = 0.025
        _GrassTopY ("Grass Tip UV Y (Max sway)", Range(0.05, 1.0)) = 0.13
        _SwayCurve ("Height Sway Curve", Range(0.5, 4.0)) = 1.65

        [Header(Autumn Breeze Sway)]
        _WindDirection ("Wind Direction (-1 Left, 1 Right)", Range(-1.0, 1.0)) = -1.0
        _WindSpeed ("Wind Speed", Range(0.0, 8.0)) = 1.65
        _SwayAmplitude ("Grass Sway Amplitude (UV)", Range(0.0, 0.02)) = 0.0042
        _BendDip ("Grass Bend Dip (UV Y)", Range(0.0, 0.01)) = 0.0012
        _WindFrequency ("Blade Wave Frequency", Range(2.0, 60.0)) = 22.0
        _GustFrequency ("Rolling Gust Frequency", Range(0.5, 15.0)) = 4.2
        _FlutterStrength ("Tip Flutter Ratio", Range(0.0, 0.6)) = 0.22

        [Header(Autumn Golden Hour Mood)]
        _GustSheenColor ("Autumn Breeze Highlight", Color) = (1.0, 0.72, 0.28, 1.0)
        _GustSheenIntensity ("Breeze Highlight Intensity", Range(0.0, 0.6)) = 0.14
        _DappleColor ("Canopy Shadow Tint", Color) = (0.48, 0.30, 0.22, 1.0)
        _DappleIntensity ("Canopy Leaf Shadow Intensity", Range(0.0, 0.4)) = 0.08
        _DappleSpeed ("Canopy Shadow Drift Speed", Range(0.0, 3.0)) = 0.35
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

            float _GrassRootY;
            float _GrassTopY;
            float _SwayCurve;

            float _WindDirection;
            float _WindSpeed;
            float _SwayAmplitude;
            float _BendDip;
            float _WindFrequency;
            float _GustFrequency;
            float _FlutterStrength;

            fixed4 _GustSheenColor;
            float _GustSheenIntensity;
            fixed4 _DappleColor;
            float _DappleIntensity;
            float _DappleSpeed;

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
                float windDir = (_WindDirection >= 0.0) ? 1.0 : -1.0;

                // ล็อกโคนหญ้าด้านล่าง (_GrassRootY) ให้อยู่กับที่ แล้วค่อยๆ เพิ่มน้ำหนักการเอนจนถึงปลายยอดหญ้า (_GrassTopY)
                float topY = max(_GrassRootY + 0.005, _GrassTopY);
                float normalizedHeight = saturate((uv.y - _GrassRootY) / (topY - _GrassRootY));
                float swayWeight = pow(normalizedHeight, _SwayCurve);

                // คลื่นลมฤดูใบไม้ร่วงพัดกวาดเป็นระลอก (Rolling Gust Envelope)
                float gustPhase = uv.x * _GustFrequency - windDir * t * (_WindSpeed * 0.52) + sin(uv.x * 1.9 + t * 0.38) * 0.65;
                float gustWave = 0.5 + 0.5 * sin(gustPhase);
                float gustEnvelope = lerp(0.32, 1.0, gustWave * gustWave);

                // คลื่นพัดกอหญ้าหลัก + แรงสั่นเบาๆ ตรงปลายยอดหญ้า
                float primaryWave = sin(uv.x * _WindFrequency - windDir * t * _WindSpeed + uv.y * 24.0);
                float flutterWave = sin(uv.x * (_WindFrequency * 2.85) - windDir * t * (_WindSpeed * 2.15) + uv.y * 42.0);
                float swayAmount = (primaryWave * 0.58 + 0.42) * gustEnvelope + flutterWave * _FlutterStrength;

                // เอนไปตามทิศลมพร้อมก้มปลายยอดลงเล็กน้อยเวลาโดนลมกด
                float offsetX = windDir * swayAmount * _SwayAmplitude * swayWeight;
                float bendDip = abs(swayAmount) * _BendDip * swayWeight;

                // เลื่อน UV สวนทิศที่ต้องการให้ใบหญ้าเอน (และ clamp ป้องกันขอบภาพแตก)
                float2 sampleUV = uv + float2(-offsetX, bendDip);
                sampleUV = clamp(sampleUV, float2(0.0005, 0.0005), float2(0.9995, 0.9995));

                fixed4 c = tex2D(_MainTex, sampleUV) * IN.color;

                // 1. แสงสีทองอบอุ่นช่วงบ่ายฤดูใบไม้ร่วง วิ่งตามจังหวะลมที่พัดผ่านยอดหญ้า
                float highlightMask = smoothstep(0.38, 0.95, gustWave) * (0.35 + 0.65 * swayWeight);
                c.rgb = lerp(c.rgb, c.rgb + _GustSheenColor.rgb * 0.45, highlightMask * _GustSheenIntensity);

                // 2. เงาใบไม้ด้านบนที่ลอยพริ้วแผ่วๆ บนผืนหญ้า
                float dappleNoise = sin(uv.x * 9.5 - windDir * t * _DappleSpeed) * cos(uv.x * 5.2 + uv.y * 12.0 + t * (_DappleSpeed * 0.65));
                float shadowMask = smoothstep(0.15, 0.85, dappleNoise * 0.5 + 0.5);
                c.rgb = lerp(c.rgb, c.rgb * _DappleColor.rgb, shadowMask * _DappleIntensity);

                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
