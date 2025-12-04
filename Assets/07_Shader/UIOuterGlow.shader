Shader "UI/OuterGlow"
{
    // UI Image 외곽에 글로우(발광) 효과
    // - 감정 조명 아이콘, 필름 카드 등에 사용
    // - box-shadow: 0 0 40px color 효과 구현

    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Outer Glow)]
        _GlowColor ("Glow Color", Color) = (0.23, 0.51, 0.96, 1) // 파란색 기본
        _CircleRadius ("Circle Radius", Range(0, 0.5)) = 0.35
        _GlowSize ("Glow Size", Range(0, 2)) = 0.5
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2
        _GlowSoftness ("Glow Softness", Range(0.01, 1)) = 0.5
        _EdgeSoftness ("Edge Softness (AA)", Range(0.001, 0.05)) = 0.01
        _Aspect ("Aspect (width/height)", Float) = 1.0

        [Header(Animation)]
        _EnablePulse ("Enable Pulse", Float) = 1
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 2
        _PulseMin ("Pulse Min", Range(0, 1)) = 0.3
        _PulseMax ("Pulse Max", Range(0, 1)) = 0.6

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
            float _CircleRadius;
            float _GlowSize;
            float _GlowIntensity;
            float _GlowSoftness;
            float _EdgeSoftness;
            float _Aspect;
            float _EnablePulse;
            float _PulseSpeed;
            float _PulseMin;
            float _PulseMax;
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
                float2 uv = i.texcoord;
                float2 center = float2(0.5, 0.5);

                // 중심으로부터 거리 계산 (Aspect 보정)
                float2 d = uv - center;
                d.x *= _Aspect;
                float dist = length(d);

                // 안티앨리어싱된 원 생성 (스프라이트 불필요)
                float circleAlpha = 1.0 - smoothstep(_CircleRadius - _EdgeSoftness, _CircleRadius + _EdgeSoftness, dist);

                // 글로우 영역 (원 바깥)
                float glowDist = dist - _CircleRadius;
                float glowFade = 1.0 - smoothstep(0, _GlowSize, glowDist);
                glowFade = pow(glowFade, _GlowSoftness);

                // 펄스 애니메이션
                float pulse = 1.0;
                if (_EnablePulse > 0.5)
                {
                    float wave = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                    pulse = lerp(_PulseMin, _PulseMax, wave);
                }

                // 글로우 알파 (원 바깥 영역에만)
                float glowAlpha = glowFade * (1.0 - circleAlpha) * _GlowIntensity * pulse * _GlowColor.a;

                // 최종 합성
                fixed4 finalColor;
                finalColor.rgb = _GlowColor.rgb;
                finalColor.a = saturate(circleAlpha + glowAlpha);

                // UI 클리핑
                finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
