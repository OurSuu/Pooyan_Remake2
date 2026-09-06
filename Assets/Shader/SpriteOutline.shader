Shader "Sprites/Outline"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        
        _OutlineColor("Outline Color", Color) = (1,1,1,1)
        _OutlineWidth("Outline Width", Range(0, 10)) = 1
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
                float2 texcoord  : TEXCOORD0;
            };
            
            fixed4 _Color;
            float _OutlineWidth;
            fixed4 _OutlineColor;

            v2f vert(appdata_t IN)
            {
                v2f OUT;
                OUT.vertex = UnityObjectToClipPos(IN.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color;
                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap (OUT.vertex);
                #endif

                return OUT;
            }

            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            fixed4 frag(v2f IN) : SV_Target
            {
                fixed4 c = tex2D(_MainTex, IN.texcoord);
                
                // If the pixel is mostly transparent, check neighbors for outline
                if (c.a < 0.1)
                {
                    float w = _OutlineWidth * _MainTex_TexelSize.x;
                    float h = _OutlineWidth * _MainTex_TexelSize.y;

                    float a = 0;
                    a += tex2D(_MainTex, IN.texcoord + float2(w, 0)).a;
                    a += tex2D(_MainTex, IN.texcoord + float2(-w, 0)).a;
                    a += tex2D(_MainTex, IN.texcoord + float2(0, h)).a;
                    a += tex2D(_MainTex, IN.texcoord + float2(0, -h)).a;
                    a += tex2D(_MainTex, IN.texcoord + float2(w, h)).a;
                    a += tex2D(_MainTex, IN.texcoord + float2(-w, h)).a;
                    a += tex2D(_MainTex, IN.texcoord + float2(w, -h)).a;
                    a += tex2D(_MainTex, IN.texcoord + float2(-w, -h)).a;

                    if (a > 0.1)
                    {
                        c = _OutlineColor;
                    }
                }
                
                c.rgb *= c.a;
                return c * IN.color;
            }
        ENDCG
        }
    }
}
