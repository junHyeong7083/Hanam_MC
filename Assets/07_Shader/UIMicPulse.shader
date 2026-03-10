Shader "UI/MicPulse"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Layer Colors  outer to inner)]
        _ColorOuter  ("Outer Layer",  Color) = (0.45, 0.82, 0.45, 0.30)
        _ColorMiddle ("Middle Layer", Color) = (0.40, 0.88, 0.40, 0.50)
        _ColorInner  ("Inner Layer",  Color) = (0.35, 0.93, 0.35, 0.75)
        _ColorCore   ("Core",         Color) = (0.30, 0.98, 0.30, 1.0)



        [Header(Shape)]
        _Roundness   ("Corner Roundness", Range(0.0, 0.5)) = 0.15
        _AspectRatio ("Aspect Ratio W/H", Range(0.1, 10.0)) = 1.0

        [Header(Layers  0 is edge  1 is center)]
        _ThreshOuter  ("Outer Start",  Range(0.0, 1.0)) = 0.0
        _ThreshMiddle ("Middle Start", Range(0.0, 1.0)) = 0.25
        _ThreshInner  ("Inner Start",  Range(0.0, 1.0)) = 0.50
        _ThreshCore   ("Core Start",   Range(0.0, 1.0)) = 0.75

        [Header(Pulse)]
        _PulseAmount ("Pulse Amount", Range(0.0, 0.3)) = 0.06
        _PulseSpeed  ("Pulse Speed",  Range(0.0, 5.0)) = 2.0

        [Header(Edge)]
        _EdgeSmooth  ("Edge Smooth",  Range(0.001, 0.1)) = 0.03

        // UI Masking
        _StencilComp ("Stencil Comp", Float) = 8
        _Stencil     ("Stencil ID",   Float) = 0
        _StencilOp   ("Stencil Op",   Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask  ("Stencil Read Mask",  Float) = 255
        _ColorMask   ("Color Mask",   Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "CanUseSpriteAtlas"="True"
            "UI"="True"
        }

        Stencil
        {
            Ref   [_Stencil]
            Comp  [_StencilComp]
            Pass  [_StencilOp]
            ReadMask  [_StencilReadMask]
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
            #pragma multi_compile_instancing
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 pos    : SV_POSITION;
                float2 uv     : TEXCOORD0;
                float4 color  : COLOR;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _ColorOuter;
            fixed4 _ColorMiddle;
            fixed4 _ColorInner;
            fixed4 _ColorCore;
            float _Roundness;
            float _AspectRatio;
            float _ThreshOuter;
            float _ThreshMiddle;
            float _ThreshInner;
            float _ThreshCore;
            float _PulseAmount;
            float _PulseSpeed;
            float _EdgeSmooth;
            float4 _ClipRect;

            // Rounded rectangle SDF (signed distance function)
            // p: point in UV space centered at origin (-0.5 ~ 0.5)
            // b: half-size of rectangle
            // r: corner radius
            float sdRoundBox(float2 p, float2 b, float r)
            {
                float2 q = abs(p) - b + r;
                return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color * _Color;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 스프라이트 텍스처 (외곽 마스킹용)
                fixed4 texColor = tex2D(_MainTex, i.uv);

                // UV를 중심 기준 좌표로 변환 (-0.5 ~ 0.5)
                float2 p = i.uv - 0.5;

                // 사각형 half-size (0.5, 0.5)
                float2 halfSize = float2(0.5, 0.5);

                // Rounded rect SDF 계산
                float r = _Roundness;
                float d = sdRoundBox(p, halfSize, r);

                // d < 0 이면 내부, d > 0 이면 외부
                // 내부 거리를 0~1로 정규화 (가장자리=0, 중심=1)
                // 최대 내부 거리 ≈ min(halfSize) - r
                float maxInner = min(halfSize.x, halfSize.y) - r * 0.3;
                float edgeDist = saturate(-d / max(maxInner, 0.001));

                // 외곽 마스크: SDF 기반 안티앨리어싱
                float edgeAA = saturate(-d / fwidth(d));

                // 펄스: 각 레이어 경계가 시간차로 움직임
                float t = _Time.y * _PulseSpeed;
                float p0 = sin(t)       * _PulseAmount;
                float p1 = sin(t - 0.5) * _PulseAmount;
                float p2 = sin(t - 1.0) * _PulseAmount;
                float p3 = sin(t - 1.5) * _PulseAmount;

                float tOuter  = _ThreshOuter  + p0;
                float tMiddle = _ThreshMiddle + p1;
                float tInner  = _ThreshInner  + p2;
                float tCore   = _ThreshCore   + p3;

                float sm = _EdgeSmooth;

                // 각 레이어 마스크
                float maskOuter  = smoothstep(tOuter  - sm, tOuter  + sm, edgeDist);
                float maskMiddle = smoothstep(tMiddle - sm, tMiddle + sm, edgeDist);
                float maskInner  = smoothstep(tInner  - sm, tInner  + sm, edgeDist);
                float maskCore   = smoothstep(tCore   - sm, tCore   + sm, edgeDist);

                // 레이어 합성: 바깥→안쪽 순서로 덮어쓰기
                fixed4 col = fixed4(0, 0, 0, 0);
                col = lerp(col, _ColorOuter,  maskOuter);
                col = lerp(col, _ColorMiddle, maskMiddle);
                col = lerp(col, _ColorInner,  maskInner);
                col = lerp(col, _ColorCore,   maskCore);

                // 외곽 클리핑 (SDF + 스프라이트 알파)
                col.a *= edgeAA * texColor.a * i.color.a;

                // UI 클리핑
                col.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);

                return col;
            }
            ENDCG
        }
    }
}
