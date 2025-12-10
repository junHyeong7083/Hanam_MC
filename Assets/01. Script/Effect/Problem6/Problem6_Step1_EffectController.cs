using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 6 - Step 1 인트로 이펙트 컨트롤러
/// React 기반 애니메이션:
/// - 드롭 인디케이터 펄스 (드래그 중 표시)
/// - 빈 공간 페이드 아웃
/// - 의자 드롭 애니메이션 (위에서 떨어짐 + 스케일 + 회전 흔들림)
/// - 스파클 방사형 퍼짐
/// </summary>
public class Problem6_Step1_EffectController : EffectControllerBase
{
    [Header("===== 드롭 인디케이터 (드래그 중 펄스) =====")]
    [SerializeField] private RectTransform dropIndicatorRect;
    [SerializeField] private CanvasGroup dropIndicatorCanvasGroup;
    [SerializeField] private float indicatorPulseDuration = 1.5f;
    [SerializeField] private float indicatorMinScale = 1f;
    [SerializeField] private float indicatorMaxScale = 1.1f;
    [SerializeField] private float indicatorMinAlpha = 0.3f;
    [SerializeField] private float indicatorMaxAlpha = 0.6f;

    [Header("===== 빈 공간 아이콘 =====")]
    [SerializeField] private GameObject emptyIconRoot;
    [SerializeField] private CanvasGroup emptyIconCanvasGroup;
    [SerializeField] private float emptyFadeOutDuration = 0.2f;

    [Header("===== 배치된 의자 =====")]
    [SerializeField] private RectTransform chairPlacedRect;
    [SerializeField] private CanvasGroup chairPlacedCanvasGroup;
    [SerializeField] private float chairDropDistance = 50f;
    [SerializeField] private float chairAppearDuration = 0.6f;
    [SerializeField] private float chairStartScale = 0.5f;
    [SerializeField] private float chairWobbleAngle = 2f;
    [SerializeField] private float chairWobbleDuration = 0.3f;

    [Header("===== 스파클 (8개 권장) =====")]
    [SerializeField] private RectTransform[] sparkleRects;
    [SerializeField] private CanvasGroup[] sparkleCanvasGroups;
    [SerializeField] private float sparkleRadius = 60f;
    [SerializeField] private float sparkleDuration = 1f;
    [SerializeField] private float sparkleDelay = 0.3f;

    // 초기값 저장
    private Vector2 _chairBasePos;
    private Vector3 _indicatorBaseScale;
    private Vector2[] _sparkleBasePositions;
    private bool _initialized;

    // 드롭 인디케이터 펄스 트윈
    private Sequence _indicatorPulseSequence;

    private void Awake()
    {
        SaveInitialState();
    }

    #region Public API

    public void SaveInitialState()
    {
        if (_initialized) return;

        if (chairPlacedRect != null)
            _chairBasePos = chairPlacedRect.anchoredPosition;

        if (dropIndicatorRect != null)
            _indicatorBaseScale = dropIndicatorRect.localScale;

        if (sparkleRects != null && sparkleRects.Length > 0)
        {
            _sparkleBasePositions = new Vector2[sparkleRects.Length];
            for (int i = 0; i < sparkleRects.Length; i++)
            {
                if (sparkleRects[i] != null)
                    _sparkleBasePositions[i] = sparkleRects[i].anchoredPosition;
            }
        }

        _initialized = true;
    }

    /// <summary>
    /// 드롭 인디케이터 펄스 시작 (드래그 시작 시 호출)
    /// </summary>
    public void ShowDropIndicator()
    {
        SaveInitialState();

        if (dropIndicatorRect == null) return;

        // 활성화
        dropIndicatorRect.gameObject.SetActive(true);

        // 초기 상태
        dropIndicatorRect.localScale = _indicatorBaseScale * indicatorMinScale;
        if (dropIndicatorCanvasGroup != null)
            dropIndicatorCanvasGroup.alpha = indicatorMinAlpha;

        // 페이드 인
        KillIndicatorPulse();
        _indicatorPulseSequence = DOTween.Sequence();

        // 등장 애니메이션
        _indicatorPulseSequence.Append(dropIndicatorRect
            .DOScale(_indicatorBaseScale, 0.2f)
            .SetEase(Ease.OutQuad));

        if (dropIndicatorCanvasGroup != null)
            _indicatorPulseSequence.Join(dropIndicatorCanvasGroup.DOFade(indicatorMinAlpha, 0.2f));

        // 펄스 루프: scale [1, 1.1, 1], opacity [0.3, 0.6, 0.3]
        _indicatorPulseSequence.AppendCallback(() =>
        {
            // 스케일 펄스
            dropIndicatorRect
                .DOScale(_indicatorBaseScale * indicatorMaxScale, indicatorPulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);

            // 알파 펄스
            if (dropIndicatorCanvasGroup != null)
            {
                dropIndicatorCanvasGroup
                    .DOFade(indicatorMaxAlpha, indicatorPulseDuration * 0.5f)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo);
            }
        });
    }

    /// <summary>
    /// 드롭 인디케이터 숨김 (드래그 종료 시 호출)
    /// </summary>
    public void HideDropIndicator()
    {
        KillIndicatorPulse();

        if (dropIndicatorRect != null)
        {
            dropIndicatorRect.gameObject.SetActive(false);
            dropIndicatorRect.localScale = _indicatorBaseScale;
        }

        if (dropIndicatorCanvasGroup != null)
            dropIndicatorCanvasGroup.alpha = indicatorMinAlpha;
    }

    /// <summary>
    /// 활성화 이펙트 재생 (드롭 성공 시 호출)
    /// </summary>
    public void PlayActivateEffect(Action onComplete = null)
    {
        if (IsAnimating)
        {
            onComplete?.Invoke();
            return;
        }

        SaveInitialState();

        // 드롭 인디케이터 숨김
        HideDropIndicator();

        // 초기 상태 설정
        SetupInitialState();

        var seq = CreateSequence();

        // 1. 빈 공간 페이드 아웃
        if (emptyIconCanvasGroup != null)
        {
            seq.Append(emptyIconCanvasGroup.DOFade(0f, emptyFadeOutDuration));
        }

        seq.AppendCallback(() =>
        {
            if (emptyIconRoot != null)
                emptyIconRoot.SetActive(false);
        });

        // 2. 의자 드롭 애니메이션
        seq.AppendCallback(() =>
        {
            if (chairPlacedRect != null)
                chairPlacedRect.gameObject.SetActive(true);
        });

        if (chairPlacedRect != null)
        {
            // 위에서 떨어지면서 나타남
            seq.Append(chairPlacedRect
                .DOAnchorPos(_chairBasePos, chairAppearDuration)
                .SetEase(Ease.OutQuad));

            seq.Join(chairPlacedRect
                .DOScale(1f, chairAppearDuration)
                .SetEase(Ease.OutBack));
        }

        if (chairPlacedCanvasGroup != null)
        {
            seq.Join(chairPlacedCanvasGroup.DOFade(1f, chairAppearDuration));
        }

        // 3. 의자 회전 흔들림 (rotate: [0, -2, 2, 0])
        if (chairPlacedRect != null)
        {
            float wobbleDur = chairWobbleDuration / 3f;
            seq.Append(chairPlacedRect.DORotate(new Vector3(0, 0, -chairWobbleAngle), wobbleDur).SetEase(Ease.InOutSine));
            seq.Append(chairPlacedRect.DORotate(new Vector3(0, 0, chairWobbleAngle), wobbleDur).SetEase(Ease.InOutSine));
            seq.Append(chairPlacedRect.DORotate(Vector3.zero, wobbleDur).SetEase(Ease.InOutSine));
        }

        // 4. 스파클 방사형 퍼짐
        if (sparkleRects != null && sparkleRects.Length > 0)
        {
            float insertTime = emptyFadeOutDuration + sparkleDelay;

            for (int i = 0; i < sparkleRects.Length; i++)
            {
                if (sparkleRects[i] == null) continue;

                int index = i;
                float angle = (i * Mathf.PI * 2f) / sparkleRects.Length;
                Vector2 targetOffset = new Vector2(
                    Mathf.Cos(angle) * sparkleRadius,
                    Mathf.Sin(angle) * sparkleRadius
                );

                // 스파클 활성화
                seq.InsertCallback(insertTime, () =>
                {
                    sparkleRects[index].gameObject.SetActive(true);
                    sparkleRects[index].localScale = Vector3.zero;
                    sparkleRects[index].anchoredPosition = _sparkleBasePositions != null && index < _sparkleBasePositions.Length
                        ? _sparkleBasePositions[index]
                        : Vector2.zero;

                    if (sparkleCanvasGroups != null && index < sparkleCanvasGroups.Length && sparkleCanvasGroups[index] != null)
                        sparkleCanvasGroups[index].alpha = 1f;
                });

                // 스케일: 0 → 1 → 0
                seq.Insert(insertTime, sparkleRects[index]
                    .DOScale(1f, sparkleDuration * 0.5f)
                    .SetEase(Ease.OutQuad));

                seq.Insert(insertTime + sparkleDuration * 0.5f, sparkleRects[index]
                    .DOScale(0f, sparkleDuration * 0.5f)
                    .SetEase(Ease.InQuad));

                // 위치: 중심에서 바깥으로
                Vector2 startPos = _sparkleBasePositions != null && index < _sparkleBasePositions.Length
                    ? _sparkleBasePositions[index]
                    : Vector2.zero;

                seq.Insert(insertTime, sparkleRects[index]
                    .DOAnchorPos(startPos + targetOffset, sparkleDuration)
                    .SetEase(Ease.OutQuad));

                // 알파: 1 → 0
                if (sparkleCanvasGroups != null && i < sparkleCanvasGroups.Length && sparkleCanvasGroups[i] != null)
                {
                    seq.Insert(insertTime + sparkleDuration * 0.5f, sparkleCanvasGroups[i]
                        .DOFade(0f, sparkleDuration * 0.5f));
                }

                // 스파클 숨김
                seq.InsertCallback(insertTime + sparkleDuration, () =>
                {
                    if (sparkleRects[index] != null)
                        sparkleRects[index].gameObject.SetActive(false);
                });
            }
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 초기 상태로 리셋
    /// </summary>
    public void ResetToInitial()
    {
        KillCurrentSequence();
        KillIndicatorPulse();
        SaveInitialState();

        // 드롭 인디케이터 숨김
        if (dropIndicatorRect != null)
        {
            dropIndicatorRect.gameObject.SetActive(false);
            dropIndicatorRect.localScale = _indicatorBaseScale;
        }

        if (dropIndicatorCanvasGroup != null)
            dropIndicatorCanvasGroup.alpha = indicatorMinAlpha;

        // 빈 공간 보임
        if (emptyIconRoot != null)
            emptyIconRoot.SetActive(true);

        if (emptyIconCanvasGroup != null)
            emptyIconCanvasGroup.alpha = 1f;

        // 의자 숨김
        if (chairPlacedRect != null)
        {
            chairPlacedRect.gameObject.SetActive(false);
            chairPlacedRect.anchoredPosition = _chairBasePos;
            chairPlacedRect.localScale = Vector3.one;
            chairPlacedRect.localRotation = Quaternion.identity;
        }

        if (chairPlacedCanvasGroup != null)
            chairPlacedCanvasGroup.alpha = 1f;

        // 스파클 숨김
        HideAllSparkles();
    }

    #endregion

    #region Private Helpers

    private void KillIndicatorPulse()
    {
        _indicatorPulseSequence?.Kill();
        _indicatorPulseSequence = null;

        // DOTween으로 직접 건 펄스도 Kill
        if (dropIndicatorRect != null)
            DOTween.Kill(dropIndicatorRect);

        if (dropIndicatorCanvasGroup != null)
            DOTween.Kill(dropIndicatorCanvasGroup);
    }

    private void SetupInitialState()
    {
        // 의자 초기 상태 (위에서 떨어질 준비)
        if (chairPlacedRect != null)
        {
            chairPlacedRect.gameObject.SetActive(false);
            chairPlacedRect.anchoredPosition = _chairBasePos + Vector2.up * chairDropDistance;
            chairPlacedRect.localScale = Vector3.one * chairStartScale;
            chairPlacedRect.localRotation = Quaternion.identity;
        }

        if (chairPlacedCanvasGroup != null)
            chairPlacedCanvasGroup.alpha = 0f;

        // 스파클 숨김
        HideAllSparkles();
    }

    private void HideAllSparkles()
    {
        if (sparkleRects == null) return;

        for (int i = 0; i < sparkleRects.Length; i++)
        {
            if (sparkleRects[i] != null)
            {
                sparkleRects[i].gameObject.SetActive(false);
                sparkleRects[i].localScale = Vector3.zero;

                if (_sparkleBasePositions != null && i < _sparkleBasePositions.Length)
                    sparkleRects[i].anchoredPosition = _sparkleBasePositions[i];
            }

            if (sparkleCanvasGroups != null && i < sparkleCanvasGroups.Length && sparkleCanvasGroups[i] != null)
                sparkleCanvasGroups[i].alpha = 1f;
        }
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        KillIndicatorPulse();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        KillIndicatorPulse();
    }

    #endregion
}
