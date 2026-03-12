using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Problem6_Step3_EffectController - 문제6 스텝3(이완 훈련)의 이펙트 관리자
///
/// 【역할】 이완 훈련 단계 카드의 팝인 애니메이션(스케일 0→1.2→1 + 알파 페이드인) 관리.
///          별도 _cardSequence로 메인 시퀀스와 독립 동작.
/// 【사용 위치】 ProblemScene - Problem6 Step3 (이완 훈련 스텝)
/// 【트리거】 Logic 클래스에서 PlayCardPopIn() 호출
/// 【의존성】 EffectControllerBase(상속), DOTween, stepCardRect/stepCardCanvasGroup
/// </summary>
public class Problem6_Step3_EffectController : EffectControllerBase
{
    [Header("===== 단계 카드 등장 애니메이션 =====")]
    [SerializeField] private RectTransform stepCardRect;
    [SerializeField] private CanvasGroup stepCardCanvasGroup;
    [SerializeField] private float cardFadeInDuration = 0.1f;
    [SerializeField] private float cardScaleUpDuration = 0.15f;
    [SerializeField] private float cardScaleDownDuration = 0.1f;
    [SerializeField] private float cardMaxScale = 1.2f;
    [SerializeField] private float cardFinalScale = 1f;

    private Sequence _cardSequence;
    private Vector3 _stepCardBaseScale;
    private bool _initialized;

    private void Awake()
    {
        SaveInitialState();
    }

    public void SaveInitialState()
    {
        if (_initialized) return;

        if (stepCardRect != null)
            _stepCardBaseScale = stepCardRect.localScale;

        _initialized = true;
    }

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

        stepCardRect.localScale = Vector3.zero;
        if (stepCardCanvasGroup != null)
            stepCardCanvasGroup.alpha = 0f;

        _cardSequence = DOTween.Sequence();

        if (stepCardCanvasGroup != null)
            _cardSequence.Append(stepCardCanvasGroup.DOFade(1f, cardFadeInDuration));

        _cardSequence.Join(stepCardRect
            .DOScale(_stepCardBaseScale * cardMaxScale, cardScaleUpDuration)
            .SetEase(Ease.OutBack));

        _cardSequence.Append(stepCardRect
            .DOScale(_stepCardBaseScale * cardFinalScale, cardScaleDownDuration)
            .SetEase(Ease.OutQuad));

        _cardSequence.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 카드 즉시 숨김
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

    public void ResetAll()
    {
        KillCurrentSequence();
        KillCardSequence();
        SaveInitialState();

        if (stepCardRect != null)
            stepCardRect.localScale = _stepCardBaseScale;

        if (stepCardCanvasGroup != null)
            stepCardCanvasGroup.alpha = 1f;
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        KillCardSequence();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        KillCardSequence();
    }
}
