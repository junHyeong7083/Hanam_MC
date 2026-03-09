Shader "UI/RadialBurst"
{
    // UI Image 하나로 방사형 광선(스타버스트) 이펙트
    // - 보상 아이템 뒤 배경 등에 사용
    // - 광선 수, 회전 속도, 색상, 페이드 조절 가능

    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Rays)]
        _RayColor ("Ray Color", Color) = (1, 0.95, 0.5, 1)
        _RayCount ("Ray Count", Range(4, 40)) = 12
        _RayWidth ("Ray Width", Range(0.01, 1)) = 0.5
        _RaySoftness ("Ray Edge Softness", Range(0.001, 0.5)) = 0.05

        [Header(Fade)]
        _InnerRadius ("Inner Radius (fade start)", Range(0, 0.5)) = 0.05
        _OuterRadius ("Outer Radius (fade end)", Range(0.1, 1.0)) = 0.7

        [Header(Center)]
        _CenterColor ("Center Color", Color) = (1, 1, 1, 1)
        _CenterGlow ("Center Glow Intensity", Range(0, 3)) = 1.0

        [Header(Animation)]
        _RotateSpeed ("Rotate Speed", Range(-3, 3)) = 0.3

        [Header(Pulse)]
        _EnablePulse ("Enable Pulse", Float) = 0
        _PulseSpeed ("Pulse Speed", Range(0, 5)) = 2
        _PulseMin ("Pulse Min Alpha", Range(0, 1)) = 0.6
        _PulseMax ("Pulse Max Alpha", Range(0, 1)) = 1.0

        // UI masking
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
            fixed4 _RayColor;
            fixed4 _CenterColor;
            float _RayCount;
            float _RayWidth;
            float _RaySoftness;
            float _InnerRadius;
            float _OuterRadius;
            float _CenterGlow;
            float _RotateSpeed;
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
                float2 d = uv - center;

                // 중심으로부터 거리
                float dist = length(d);

                // 극좌표 각도 + 회전 애니메이션
                float angle = atan2(d.y, d.x);
                angle += _Time.y * _RotateSpeed;

                // 방사형 줄무늬: sin으로 부드러운 광선 생성
                float rayPattern = sin(angle * _RayCount);

                // RayWidth로 밝은 영역 비율 조절, softness로 경계 부드럽게
                float halfWidth = _RayWidth;
                float ray = smoothstep(halfWidth - _RaySoftness, halfWidth + _RaySoftness, rayPattern);

                // 거리 기반 페이드: 안쪽 글로우 + 바깥 페이드아웃
                float radialFade = 1.0 - smoothstep(_InnerRadius, _OuterRadius, dist);

                // 중심 글로우 (밝은 원형 코어)
                float centerGlow = (1.0 - smoothstep(0.0, _InnerRadius * 3.0, dist)) * _CenterGlow;

                // 펄스 애니메이션
                float pulse = 1.0;
                if (_EnablePulse > 0.5)
                {
                    float wave = sin(_Time.y * _PulseSpeed) * 0.5 + 0.5;
                    pulse = lerp(_PulseMin, _PulseMax, wave);
                }

                // 광선 부분
                float rayAlpha = ray * radialFade * pulse;

                // 중심 원: 불투명하게 광선 위를 덮음
                float centerAlpha = saturate(centerGlow) * pulse;

                // 중심 원이 앞에 → lerp로 광선을 덮어씌움
                fixed3 rayCol = _RayColor.rgb;
                fixed3 centerCol = _CenterColor.rgb;

                fixed3 blended = lerp(rayCol, centerCol, centerAlpha);
                float totalAlpha = saturate(max(rayAlpha, centerAlpha)) * i.color.a;

                fixed4 finalColor;
                finalColor.rgb = blended * i.color.rgb;
                finalColor.a = totalAlpha;

                // UI 클리핑
                finalColor.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);

                return finalColor;
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}
