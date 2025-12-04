Shader "UI/EmotionPulse"
{
     Properties
    {
        // 그냥 흰 사각형 스프라이트 써도 됨
        [PerRendererData] _MainTex ("Sprite", 2D) = "white" {}
        [PerRendererData] _Color   ("Tint Color", Color) = (1,1,1,1)

        _BaseColor   ("Base Color", Color) = (0.14, 0.51, 0.96, 1) // 파란색

        // 안쪽 원의 반지름 (0~0.5 정도)
        _InnerRadius ("Inner Radius", Range(0.0, 0.5)) = 0.25

        // 바깥 그라데이션이 끝나는 반지름 (Inner보다 항상 커야 함)
        _HaloRadius  ("Halo Radius",  Range(0.0, 2.0)) = 0.8

        // 그라데이션 최대 알파
        _HaloAlpha   ("Halo Alpha",   Range(0.0, 1.0)) = 0.35

        // 펄스 애니메이션
        _PulseAmount ("Pulse Amount", Range(0.0, 0.5)) = 0.15
        _PulseSpeed  ("Pulse Speed",  Range(0.0, 5.0)) = 1.5

        // RectTransform이 정사각형이 아니면 width/height 값 넣어주기
        _Aspect      ("Aspect (width/height)", Float) = 1.0
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                fixed4 color    : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv     : TEXCOORD0;
                fixed4 color  : COLOR;
            };

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _Color;

            fixed4 _BaseColor;
            float  _InnerRadius;
            float  _HaloRadius;
            float  _HaloAlpha;
            float  _PulseAmount;
            float  _PulseSpeed;
            float  _Aspect;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv     = TRANSFORM_TEX(v.texcoord, _MainTex);
                o.color  = v.color * _Color;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 그냥 색/알파는 UI Image 색 따라가게만 사용
                fixed4 texCol = tex2D(_MainTex, i.uv) * i.color;

                // 중심 기준 거리 (Aspect 보정해서 진짜 원 느낌)
                float2 center = float2(0.5, 0.5);
                float2 d = i.uv - center;
                d.x *= _Aspect;
                float dist = length(d); // 0 = 중심

                // 1) 안쪽 꽉 찬 원 마스크
                float innerMask = step(dist, _InnerRadius);

                // 2) 바깥 그라데이션 (InnerRadius~HaloRadius 구간만)
                float halo = 0.0;
                if (dist > _InnerRadius && dist < _HaloRadius)
                {
                    float t = (dist - _InnerRadius) / (_HaloRadius - _InnerRadius); // 0~1
                    // 0에서 1로 갈수록 서서히 줄어드는 값 (끝에서는 정확히 0)
                    halo = 1.0 - t;
                    halo = halo * halo; // 좀 더 부드럽게
                }

                // 펄스 (0.85 ~ 1.15 정도)
                float pulse = 1.0 + sin(_Time.y * _PulseSpeed) * _PulseAmount;

                // 안쪽 원은 항상 1, halo는 Pulse + Alpha 조합
                float innerAlpha = 1.0;
                float haloAlpha  = halo * _HaloAlpha * pulse;

                float finalAlpha = innerMask * innerAlpha + haloAlpha;

                // 바깥쪽은 정확히 0으로 잘라서 경계 안 보이게
                if (dist >= _HaloRadius)
                    finalAlpha = 0.0;

                // 최종 색
                fixed3 col = _BaseColor.rgb;

                // Image 자체의 알파도 곱해주기 (필요 없으면 빼도 됨)
                finalAlpha *= texCol.a;

                return fixed4(col * finalAlpha, finalAlpha);
            }
            ENDCG
        }
    }

    FallBack "UI/Default"
}