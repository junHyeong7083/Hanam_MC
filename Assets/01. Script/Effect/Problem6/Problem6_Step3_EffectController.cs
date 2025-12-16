using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Part 6 - Step 3 이완 훈련 이펙트 컨트롤러
/// - 호흡 원 애니메이션: 들이쉬기 scale [1, 1.5], 내쉬기 scale [1.5, 1]
/// - 단계 전환 애니메이션
/// </summary>
public class Problem6_Step3_EffectController : EffectControllerBase
{
    [Header("===== 호흡 원 애니메이션 =====")]
    [SerializeField] private RectTransform breathingCircleRect;
    [SerializeField] private CanvasGroup breathingCircleCanvasGroup;
    [SerializeField] private float breathMinScale = 1f;
    [SerializeField] private float breathMaxScale = 1.5f;
    [SerializeField] private float breathInDuration = 4f;
    [SerializeField] private float breathOutDuration = 4f;
    [SerializeField] private float breathHoldDuration = 2f;

    [Header("===== 단계 카드 등장 애니메이션 =====")]
    [SerializeField] private RectTransform stepCardRect;
    [SerializeField] private CanvasGroup stepCardCanvasGroup;
    [SerializeField] private float cardFadeInDuration = 0.1f;
    [SerializeField] private float cardScaleUpDuration = 0.15f;
    [SerializeField] private float cardScaleDownDuration = 0.1f;
    [SerializeField] private float cardMaxScale = 1.2f;
    [SerializeField] private float cardFinalScale = 1f;

    // 호흡 루프 트윈
    private Sequence _breathingSequence;
    private Sequence _cardSequence;
    private Vector3 _breathingBaseScale;
    private Vector3 _stepCardBaseScale;
    private bool _initialized;

    private void Awake()
    {
        SaveInitialState();
    }

    public void SaveInitialState()
    {
        if (_initialized) return;

        if (breathingCircleRect != null)
            _breathingBaseScale = breathingCircleRect.localScale;

        if (stepCardRect != null)
            _stepCardBaseScale = stepCardRect.localScale;

        _initialized = true;
    }

    #region Breathing Animation

    /// <summary>
    /// 호흡 애니메이션 시작
    /// </summary>
    public void StartBreathingAnimation()
    {
        SaveInitialState();

        if (breathingCircleRect == null) return;

        KillBreathingAnimation();

        // 호흡 원 활성화
        breathingCircleRect.gameObject.SetActive(true);
        breathingCircleRect.localScale = _breathingBaseScale * breathMinScale;

        if (breathingCircleCanvasGroup != null)
            breathingCircleCanvasGroup.alpha = 1f;

        _breathingSequence = DOTween.Sequence();

        // 들이쉬기: 작 → 크
        _breathingSequence.Append(breathingCircleRect
            .DOScale(_breathingBaseScale * breathMaxScale, breathInDuration)
            .SetEase(Ease.InOutSine));

        // 참기
        if (breathHoldDuration > 0)
            _breathingSequence.AppendInterval(breathHoldDuration);

        // 내쉬기: 크 → 작
        _breathingSequence.Append(breathingCircleRect
            .DOScale(_breathingBaseScale * breathMinScale, breathOutDuration)
            .SetEase(Ease.InOutSine));

        // 참기
        if (breathHoldDuration > 0)
            _breathingSequence.AppendInterval(breathHoldDuration);

        // 무한 반복
        _breathingSequence.SetLoops(-1);
    }

    /// <summary>
    /// 호흡 애니메이션 정지
    /// </summary>
    public void StopBreathingAnimation()
    {
        KillBreathingAnimation();

        if (breathingCircleRect != null)
            breathingCircleRect.gameObject.SetActive(false);
    }

    /// <summary>
    /// 호흡 애니메이션 일시정지
    /// </summary>
    public void PauseBreathingAnimation()
    {
        _breathingSequence?.Pause();
    }

    /// <summary>
    /// 호흡 애니메이션 재개
    /// </summary>
    public void ResumeBreathingAnimation()
    {
        _breathingSequence?.Play();
    }

    #endregion

    #region Step Card Animation

    /// <summary>
    /// 단계 카드 팝인 애니메이션: 0 → 1.2 → 1 스케일 + 알파 페이드인
    /// </summary>
    public void PlayCardPopIn(Action onComplete = null)
    {
        if (stepCardRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        SaveInitialState();
        KillCardSequence();

        // 초기 상태: 스케일 0, 알파 0
        stepCardRect.localScale = Vector3.zero;
        if (stepCardCanvasGroup != null)
            stepCardCanvasGroup.alpha = 0f;

        _cardSequence = DOTween.Sequence();

        // 알파 페이드인 (0.1초)
        if (stepCardCanvasGroup != null)
            _cardSequence.Append(stepCardCanvasGroup.DOFade(1f, cardFadeInDuration));

        // 스케일 0 → 1.2 (동시에)
        _cardSequence.Join(stepCardRect
            .DOScale(_stepCardBaseScale * cardMaxScale, cardScaleUpDuration)
            .SetEase(Ease.OutBack));

        // 스케일 1.2 → 1
        _cardSequence.Append(stepCardRect
            .DOScale(_stepCardBaseScale * cardFinalScale, cardScaleDownDuration)
            .SetEase(Ease.OutQuad));

        _cardSequence.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 카드 즉시 숨김 (다음 단계 전환 전)
    /// </summary>
    public void HideCardImmediate()
    {
        KillCardSequence();

        if (stepCardRect != null)
            stepCardRect.localScale = Vector3.zero;

        if (stepCardCanvasGroup != null)
            stepCardCanvasGroup.alpha = 0f;
    }

    private void KillCardSequence()
    {
        _cardSequence?.Kill();
        _cardSequence = null;

        if (stepCardRect != null)
            DOTween.Kill(stepCardRect);
        if (stepCardCanvasGroup != null)
            DOTween.Kill(stepCardCanvasGroup);
    }

    #endregion

    #region Reset

    /// <summary>
    /// 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();
        KillBreathingAnimation();
        KillCardSequence();
        SaveInitialState();

        // 호흡 원 숨김
        if (breathingCircleRect != null)
        {
            breathingCircleRect.gameObject.SetActive(false);
            breathingCircleRect.localScale = _breathingBaseScale;
        }

        // 단계 카드 스케일/알파 복원
        if (stepCardRect != null)
            stepCardRect.localScale = _stepCardBaseScale;

        if (stepCardCanvasGroup != null)
            stepCardCanvasGroup.alpha = 1f;
    }

    #endregion

    #region Private Helpers

    private void KillBreathingAnimation()
    {
        _breathingSequence?.Kill();
        _breathingSequence = null;

        if (breathingCircleRect != null)
            DOTween.Kill(breathingCircleRect);
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        KillBreathingAnimation();
        KillCardSequence();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        KillBreathingAnimation();
        KillCardSequence();
    }
}
