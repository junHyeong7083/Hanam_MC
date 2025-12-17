using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 9 - Step 3 명대사 완성 이펙트 컨트롤러
/// - 녹음 연습 화면 등장
/// - 단계 전환 애니메이션
/// - 사용자 입력 표시 애니메이션
/// - 완료 화면 애니메이션 (합쳐진 대사 + OK CUT)
/// </summary>
public class Problem9_Step3_EffectController : EffectControllerBase
{
    [Header("===== 녹음 연습 화면 =====")]
    [SerializeField] private RectTransform practiceCardRect;
    [SerializeField] private CanvasGroup practiceCardCanvasGroup;
    [SerializeField] private float introSlideDistance = 30f;
    [SerializeField] private float introAppearDuration = 0.5f;

    [Header("===== 단계 표시 =====")]
    [SerializeField] private RectTransform stepIndicatorRect;
    [SerializeField] private CanvasGroup stepIndicatorCanvasGroup;

    [Header("===== 사용자 입력 표시 =====")]
    [SerializeField] private RectTransform userInputRect;
    [SerializeField] private CanvasGroup userInputCanvasGroup;

    [Header("===== 완료 화면 =====")]
    [SerializeField] private RectTransform completeCardRect;
    [SerializeField] private CanvasGroup completeCardCanvasGroup;

    [Header("===== 완료 - 체크 아이콘 =====")]
    [SerializeField] private RectTransform successCheckRect;

    [Header("===== 완료 - 합쳐진 대사 =====")]
    [SerializeField] private RectTransform dialogueBubbleRect;
    [SerializeField] private CanvasGroup dialogueBubbleCanvasGroup;

    [Header("===== 완료 - OK CUT 뱃지 =====")]
    [SerializeField] private RectTransform okCutBadgeRect;

    [Header("===== 완료 - NPC 반응 =====")]
    [SerializeField] private RectTransform npcReactionRect;
    [SerializeField] private RectTransform npcReactionBubbleRect;
    [SerializeField] private CanvasGroup npcReactionBubbleCanvasGroup;

    [Header("===== 완료 - 어시스턴트 피드백 =====")]
    [SerializeField] private RectTransform feedbackCardRect;
    [SerializeField] private CanvasGroup feedbackCardCanvasGroup;

    #region Public API - 녹음 연습 화면

    /// <summary>
    /// 녹음 연습 화면 등장 애니메이션
    /// </summary>
    public void PlayPracticeEnterAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 1. 연습 카드 슬라이드 업 + 페이드
        if (practiceCardRect != null && practiceCardCanvasGroup != null)
        {
            Vector2 basePos = practiceCardRect.anchoredPosition;
            practiceCardRect.anchoredPosition = basePos + Vector2.down * introSlideDistance;
            practiceCardCanvasGroup.alpha = 0f;

            seq.Append(practiceCardRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Join(practiceCardCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 2. 단계 표시 등장
        if (stepIndicatorRect != null && stepIndicatorCanvasGroup != null)
        {
            stepIndicatorRect.localScale = Vector3.zero;
            stepIndicatorCanvasGroup.alpha = 0f;

            seq.Insert(0.3f, stepIndicatorRect
                .DOScale(1f, 0.3f)
                .SetEase(Ease.OutBack));
            seq.Insert(0.3f, stepIndicatorCanvasGroup.DOFade(1f, 0.3f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 단계 전환 애니메이션
    /// </summary>
    public void PlayPhaseTransition(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 단계 표시 펀치
        if (stepIndicatorRect != null)
        {
            seq.Append(stepIndicatorRect
                .DOScale(1.2f, 0.15f)
                .SetEase(Ease.OutQuad));
            seq.Append(stepIndicatorRect
                .DOScale(1f, 0.15f)
                .SetEase(Ease.OutQuad));
        }

        // 사용자 입력 숨김
        if (userInputCanvasGroup != null)
        {
            seq.Join(userInputCanvasGroup.DOFade(0f, 0.2f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - 사용자 입력 표시

    /// <summary>
    /// 녹음 완료 애니메이션 (사용자 입력 표시)
    /// </summary>
    public void PlayRecordingCompleteAnimation(Action onComplete = null)
    {
        if (userInputRect == null || userInputCanvasGroup == null)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();

        Vector2 basePos = userInputRect.anchoredPosition;
        userInputRect.anchoredPosition = basePos + Vector2.down * 20f;
        userInputCanvasGroup.alpha = 0f;

        seq.Append(userInputRect
            .DOAnchorPos(basePos, 0.3f)
            .SetEase(Ease.OutQuad));
        seq.Join(userInputCanvasGroup.DOFade(1f, 0.3f));

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - 완료 화면

    /// <summary>
    /// 완료 화면 애니메이션
    /// </summary>
    public void PlayCompleteAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 1. 완료 카드 스케일 + 페이드
        if (completeCardRect != null && completeCardCanvasGroup != null)
        {
            completeCardRect.localScale = Vector3.one * 0.9f;
            completeCardCanvasGroup.alpha = 0f;

            seq.Append(completeCardRect
                .DOScale(1f, 0.4f)
                .SetEase(Ease.OutQuad));
            seq.Join(completeCardCanvasGroup.DOFade(1f, 0.4f));
        }

        // 2. 체크 아이콘 스프링 등장
        if (successCheckRect != null)
        {
            successCheckRect.localScale = Vector3.zero;
            seq.Insert(0.2f, successCheckRect
                .DOScale(1f, 0.5f)
                .SetEase(Ease.OutBack, 2f));
        }

        // 3. 대사 버블 슬라이드 업 (딜레이 0.4초)
        if (dialogueBubbleRect != null && dialogueBubbleCanvasGroup != null)
        {
            Vector2 basePos = dialogueBubbleRect.anchoredPosition;
            dialogueBubbleRect.anchoredPosition = basePos + Vector2.down * 20f;
            dialogueBubbleCanvasGroup.alpha = 0f;

            seq.Insert(0.4f, dialogueBubbleRect
                .DOAnchorPos(basePos, 0.4f)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.4f, dialogueBubbleCanvasGroup.DOFade(1f, 0.4f));
        }

        // 4. OK CUT 뱃지 스프링 등장 (딜레이 0.6초)
        if (okCutBadgeRect != null)
        {
            okCutBadgeRect.localScale = Vector3.zero;
            seq.Insert(0.6f, okCutBadgeRect
                .DOScale(1f, 0.4f)
                .SetEase(Ease.OutBack, 1.5f));
        }

        // 5. NPC 반응 슬라이드 인 (딜레이 0.8초)
        if (npcReactionRect != null)
        {
            Vector2 basePos = npcReactionRect.anchoredPosition;
            npcReactionRect.anchoredPosition = basePos + Vector2.left * 30f;

            seq.Insert(0.8f, npcReactionRect
                .DOAnchorPos(basePos, 0.4f)
                .SetEase(Ease.OutQuad));

            // NPC 흔들림
            seq.Insert(1.0f, npcReactionRect
                .DORotate(new Vector3(0, 0, -5), 0.2f)
                .SetLoops(4, LoopType.Yoyo)
                .SetEase(Ease.InOutSine));
        }

        // 5-1. NPC 반응 버블 페이드 인
        if (npcReactionBubbleRect != null && npcReactionBubbleCanvasGroup != null)
        {
            npcReactionBubbleCanvasGroup.alpha = 0f;
            seq.Insert(1.0f, npcReactionBubbleCanvasGroup.DOFade(1f, 0.3f));
        }

        // 6. 어시스턴트 피드백 슬라이드 업 (딜레이 1.2초)
        if (feedbackCardRect != null && feedbackCardCanvasGroup != null)
        {
            Vector2 basePos = feedbackCardRect.anchoredPosition;
            feedbackCardRect.anchoredPosition = basePos + Vector2.down * 20f;
            feedbackCardCanvasGroup.alpha = 0f;

            seq.Insert(1.2f, feedbackCardRect
                .DOAnchorPos(basePos, 0.4f)
                .SetEase(Ease.OutQuad));
            seq.Insert(1.2f, feedbackCardCanvasGroup.DOFade(1f, 0.4f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Reset

    /// <summary>
    /// 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();

        // 연습 카드 리셋
        if (practiceCardCanvasGroup != null)
        {
            DOTween.Kill(practiceCardRect);
            DOTween.Kill(practiceCardCanvasGroup);
            practiceCardCanvasGroup.alpha = 0f;
        }

        // 단계 표시 리셋
        if (stepIndicatorRect != null)
        {
            DOTween.Kill(stepIndicatorRect);
            stepIndicatorRect.localScale = Vector3.one;
        }

        // 사용자 입력 리셋
        if (userInputCanvasGroup != null)
        {
            DOTween.Kill(userInputRect);
            DOTween.Kill(userInputCanvasGroup);
            userInputCanvasGroup.alpha = 0f;
        }

        // 완료 화면 리셋
        if (completeCardCanvasGroup != null)
        {
            DOTween.Kill(completeCardRect);
            DOTween.Kill(completeCardCanvasGroup);
            completeCardCanvasGroup.alpha = 0f;
        }

        if (successCheckRect != null)
        {
            DOTween.Kill(successCheckRect);
            successCheckRect.localScale = Vector3.zero;
        }

        if (dialogueBubbleCanvasGroup != null)
        {
            DOTween.Kill(dialogueBubbleRect);
            DOTween.Kill(dialogueBubbleCanvasGroup);
            dialogueBubbleCanvasGroup.alpha = 0f;
        }

        if (okCutBadgeRect != null)
        {
            DOTween.Kill(okCutBadgeRect);
            okCutBadgeRect.localScale = Vector3.zero;
        }

        if (npcReactionRect != null)
        {
            DOTween.Kill(npcReactionRect);
            npcReactionRect.localRotation = Quaternion.identity;
        }

        if (npcReactionBubbleCanvasGroup != null)
        {
            DOTween.Kill(npcReactionBubbleCanvasGroup);
            npcReactionBubbleCanvasGroup.alpha = 0f;
        }

        if (feedbackCardCanvasGroup != null)
        {
            DOTween.Kill(feedbackCardRect);
            DOTween.Kill(feedbackCardCanvasGroup);
            feedbackCardCanvasGroup.alpha = 0f;
        }
    }

    #endregion

}
