Shader "UI/SweepHighlight"
{
    // UI Sprite/Image 에 적용하는 "대각선 반짝임(sweep)" 효과
    //
    // 사용법:
    //   1) 이 쉐이더로 Material 생성 (Assets > Create > Material)
    //   2) UI Image 의 Material 슬롯에 할당
    //   3) SweepHighlightTrigger 스크립트를 같은 GameObject 에 붙임
    //   4) 보상 지급 시 SweepHighlightTrigger.PlaySweep() 호출
    //
    // 특징:
    //   - 평소에는 완전히 원본 스프라이트 그대로 보임 (_SweepT = 0)
    //   - 스윕 밴드가 아이콘 알파 영역 밖으로 절대 나오지 않음
    //   - 공유 머티리얼 오염 없음 (SweepHighlightTrigger 가 인스턴스화)

    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Sweep Effect)]
        [Space(4)]
        _SweepT         ("Sweep T  (0=대기  1=완료)", Range(0,1)) = 0
        _SweepWidth     ("Sweep Width",    Range(0.01, 0.5)) = 0.18
        _SweepSharpness ("Sharpness  (0=넓고부드럽게  1=날카롭게)", Range(0, 1)) = 0.75
        _SweepIntensity ("Intensity", Range(0, 5))           = 2.2
        _SweepAngle     ("Angle (deg, 45=대각)", Range(0, 180)) = 45
        _SweepBulge     ("Center Bulge (중심 굵기)", Range(0, 1)) = 0.6
        _SweepColor     ("Sweep Color", Color)               = (1, 1, 1, 1)

        // ── UI 마스킹 (Mask 컴포넌트와 연동) ─────────────────────────
        _StencilComp    ("Stencil Comparison", Float) = 8
        _Stencil        ("Stencil ID",         Float) = 0
        _StencilOp      ("Stencil Operation",  Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask      ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "Queue"            = "Transparent"
            "IgnoreProjector"  = "True"
            "RenderType"       = "Transparent"
            "PreviewType"      = "Plane"
            "CanUseSpriteAtlas"= "True"
        }

        Stencil
        {
            Ref         [_Stencil]
            Comp        [_StencilComp]
            Pass        [_StencilOp]
            ReadMask    [_StencilReadMask]
            WriteMask   [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "SweepHighlight"

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex       : SV_POSITION;
                fixed4 color        : COLOR;
                float2 texcoord     : TEXCOORD0;
                float4 worldPosition: TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ── 프로퍼티 ───────────────────────────────────────────────
            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            float     _SweepT;
            float     _SweepWidth;
            float     _SweepSharpness;
            float     _SweepIntensity;
            float     _SweepAngle;
            float     _SweepBulge;
            fixed4    _SweepColor;

            float4    _ClipRect;

            // ── 버텍스 ────────────────────────────────────────────────
            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.worldPosition = v.vertex;
                o.vertex        = UnityObjectToClipPos(v.vertex);
                o.texcoord      = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color         = v.color * _Color;
                return o;
            }

            // ── 프래그먼트 ────────────────────────────────────────────
            fixed4 frag(v2f i) : SV_Target
            {
                float2 uv = i.texcoord;

                // ── 원본 스프라이트 ───────────────────────────────────
                fixed4 texColor = tex2D(_MainTex, uv);
                fixed4 color    = texColor * i.color;
                float  maskAlpha = texColor.a;

                // ── 스윕 밴드 계산 ────────────────────────────────────
                float rad = radians(_SweepAngle);
                float2 dir = normalize(float2(cos(rad), sin(rad)));

                float diag    = dot(uv - 0.5, dir) + 0.5;
                float halfExt = 0.5 * (abs(cos(rad)) + abs(sin(rad)));

                float diagStart = 0.5 - halfExt - _SweepWidth;
                float diagEnd   = 0.5 + halfExt + _SweepWidth;
                float sweepPos  = lerp(diagStart, diagEnd, _SweepT);
                float dist      = abs(diag - sweepPos);

                // ── 수직축 기반 폭 변조 (중심=굵게, 양 끝=가늘게) ─────────
                // 스윕 방향의 수직(perpendicular) 벡터로 투영
                float2 perpDir  = float2(-sin(rad), cos(rad));
                float  perpProj = dot(uv - 0.5, perpDir); // 중심=0, 양 끝=±0.5
                // 포물선 형태: 중심에서 bulge 최대, 끝으로 갈수록 감소
                float bulge     = 1.0 - perpProj * perpProj * 4.0 * _SweepBulge;
                bulge           = max(0.15, bulge); // 끝부분 최소 두께 유지
                float effWidth  = _SweepWidth * bulge;

                // ── Layer 1: Wide Gaussian glow (매우 완만한 발광, 경계 없음) ──
                // 계수가 낮을수록 더 넓고 서서히 사라짐
                // 0.7: normDist=1에서 50%, normDist=2에서 6% → 경계 체감 불가
                float normDist = dist / max(0.001, effWidth);
                float glow = exp(-normDist * normDist * 0.7);

                // ── Layer 2: Gaussian core (중심 빛 선, 타이트한 피크) ──────
                float coreW    = effWidth * (1.0 - _SweepSharpness * 0.88);
                float normCore = dist / max(0.001, coreW);
                float core     = exp(-normCore * normCore * 3.5);

                // ── 합산 & 적용 ────────────────────────────────────────
                // glow: 밝기 올림 (multiplicative) + SweepColor 가산으로 색조 입힘
                color.rgb *= 1.0 + glow * _SweepIntensity * 0.4 * maskAlpha;
                color.rgb += _SweepColor.rgb * glow * _SweepIntensity * 0.35 * maskAlpha;
                // core: 중심선에 SweepColor 강하게 가산
                color.rgb += _SweepColor.rgb * core * _SweepIntensity * maskAlpha;

                // ── UI 클립 (Rect Mask 2D 등) ─────────────────────────
                #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif

                // ── 알파클립 (Mask 컴포넌트) ──────────────────────────
                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif

                return color;
            }
            ENDCG

        }
    }

    FallBack "UI/Default"
}
