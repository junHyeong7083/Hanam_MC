Shader "UI/LineGlow"
{
    // UI 라인(Image)에 글로우 효과 추가
    // - UILineConnector의 lineImage에 사용
    // - 라인 주변에 블러/글로우 효과

    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Glow Settings)]
        _GlowColor ("Glow Color", Color) = (1, 0.54, 0.24, 1) // #FF8A3D
        _GlowIntensity ("Glow Intensity", Range(0, 3)) = 1.5
        _GlowSize ("Glow Size", Range(0, 0.5)) = 0.15

        [Header(Animation)]
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 2
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.3

        // UI 마스킹용
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
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

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                float4 worldPosition : TEXCOORD1;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _GlowColor;
            float _GlowIntensity;
            float _GlowSize;
            float _PulseSpeed;
            float _PulseAmount;
            float4 _ClipRect;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.worldPosition = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color = v.color * _Color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 원본 텍스처
                fixed4 texColor = tex2D(_MainTex, i.texcoord);

                // 글로우 샘플링 (주변 픽셀 블러)
                float2 offsets[8] = {
                    float2(-1, 0), float2(1, 0),
                    float2(0, -1), float2(0, 1),
                    float2(-0.7, -0.7), float2(0.7, -0.7),
                    float2(-0.7, 0.7), float2(0.7, 0.7)
                };

                float glowAlpha = 0;
                for (int j = 0; j < 8; j++)
                {
                    float2 sampleUV = i.texcoord + offsets[j] * _GlowSize;
                    glowAlpha += tex2D(_MainTex, sampleUV).a;
                }
                glowAlpha /= 8.0;

                // 펄스 애니메이션
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;
                glowAlpha *= pulse;

                // 글로우 색상
                fixed4 glow = _GlowColor * glowAlpha * _GlowIntensity;

                // 원본 + 글로우 합성
                fixed4 finalColor;
                finalColor.rgb = texColor.rgb * texColor.a * i.color.rgb + glow.rgb * (1 - texColor.a);
                finalColor.a = saturate(texColor.a + glowAlpha * _GlowColor.a);

                // 최종 색상에 버텍스 컬러 적용
                finalColor *= i.color;

                // UI 클리핑
                finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
