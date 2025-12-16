using UnityEngine;
using DG.Tweening;

/// <summary>
/// Part 6 - Step 1 이펙트 컨트롤러
/// - 인트로 연출 (EffectControllerBase에서 상속)
/// - 드롭 인디케이터 펄스 (드래그 중 표시)
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

    // 초기값 저장
    private Vector3 _indicatorBaseScale;
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

        if (dropIndicatorRect != null)
            _indicatorBaseScale = dropIndicatorRect.localScale;

        // 인트로 요소 위치 저장 (베이스 클래스)
        SaveIntroBasePositions();

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

        // 인트로 요소들 원래 위치로 (베이스 클래스)
        ResetIntroElements();
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
