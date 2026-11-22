Shader "Pooyan/SpriteFresnelGlow"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [HDR] _FresnelColor ("Fresnel Rim Color", Color) = (1.0, 0.92, 0.42, 1.0)
        _FresnelIntensity ("Fresnel Intensity", Range(0, 6)) = 2.0
        _FresnelPower ("Fresnel Power", Range(0.5, 8)) = 2.0
        _RimWidthPx ("Box Rim Width (Pixels)", Range(0.5, 12)) = 2.6
        _UseRadialFresnel ("Use Radial Fresnel (0=Box, 1=Circle)", Float) = 0
        _OuterGlowMode ("Outer Halo Mode (0=SpriteRim, 1=SoftHalo)", Float) = 0
        _HaloInnerExtent ("Halo Inner Extent (XY)", Vector) = (0.76, 0.20, 0, 0)
        _HaloAlpha ("Halo Opacity", Range(0, 1)) = 0.58
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
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
            #pragma target 3.0
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
            };

            sampler2D _MainTex;
            fixed4 _Color;
            float4 _FresnelColor;
            float _FresnelIntensity;
            float _FresnelPower;
            float _RimWidthPx;
            float _UseRadialFresnel;
            float _OuterGlowMode;
            float4 _HaloInnerExtent;
            float _HaloAlpha;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                if (_OuterGlowMode >= 0.5)
                {
                    float2 p = (IN.texcoord - 0.5) * 2.0;
                    float2 inner = clamp(_HaloInnerExtent.xy, float2(0.05, 0.05), float2(0.95, 0.95));
                    float2 q = max(abs(p) - inner, 0.0) / max(1.0 - inner, float2(1e-4, 1e-4));
                    float radial = saturate(length(q));
                    float halo = pow(1.0 - radial, _FresnelPower);
                    float haloAlpha = saturate(halo * _HaloAlpha) * IN.color.a;
                    float3 haloRGB = _FresnelColor.rgb * _FresnelIntensity;
                    return float4(haloRGB * haloAlpha, haloAlpha);
                }

                fixed4 tex = tex2D(_MainTex, IN.texcoord);
                float baseAlpha = tex.a * IN.color.a;
                float3 baseRGB = tex.rgb * IN.color.rgb;

                float fresnel = 0.0;
                if (_UseRadialFresnel >= 0.5)
                {
                    float radialDist = saturate(length(IN.texcoord - 0.5) * 2.0);
                    fresnel = pow(radialDist, _FresnelPower);
                }
                else
                {
                    float2 edgeUV = min(IN.texcoord, 1.0 - IN.texcoord);
                    float2 uvDeriv = max(fwidth(IN.texcoord), float2(1e-5, 1e-5));
                    float2 edgePx = edgeUV / uvDeriv;
                    float minEdgePx = min(edgePx.x, edgePx.y);
                    float rimNorm = saturate(1.0 - (minEdgePx / max(_RimWidthPx, 0.01)));
                    fresnel = pow(rimNorm, _FresnelPower);
                }

                float3 rimRGB = _FresnelColor.rgb * _FresnelIntensity;
                float3 finalRGB = lerp(baseRGB, rimRGB, fresnel);
                return float4(finalRGB * baseAlpha, baseAlpha);
            }
            ENDCG
        }
    }
}
