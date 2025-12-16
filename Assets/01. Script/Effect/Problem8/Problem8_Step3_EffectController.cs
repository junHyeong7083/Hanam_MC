using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 8 - Step 3 첫 장면 결정 이펙트 컨트롤러
/// - 인트로 카드 등장
/// - 액션 선택 애니메이션 (호버, 탭, 선택 글로우)
/// - 결과 화면 (비디오 아이콘, 메시지들)
/// </summary>
public class Problem8_Step3_EffectController : EffectControllerBase
{
    [Header("===== 인트로 카드 =====")]
    [SerializeField] private RectTransform instructionCardRect;
    [SerializeField] private CanvasGroup instructionCardCanvasGroup;
    [SerializeField] private RectTransform speechBubbleRect;
    [SerializeField] private CanvasGroup speechBubbleCanvasGroup;
    [SerializeField] private float introSlideDistance = 30f;
    [SerializeField] private float introAppearDuration = 0.5f;

    [Header("===== 액션 선택 =====")]
    [SerializeField] private float actionHoverScale = 1.02f;
    [SerializeField] private float actionHoverX = 10f;
    [SerializeField] private float actionTapScale = 0.98f;
    [SerializeField] private float actionSelectDuration = 0.2f;

    [Header("===== 선택된 액션 글로우 =====")]
    [SerializeField] private float selectedGlowMinAlpha = 0.1f;
    [SerializeField] private float selectedGlowMaxAlpha = 0.3f;
    [SerializeField] private float selectedGlowDuration = 2f;

    [Header("===== 선택된 이모지 펄스 =====")]
    [SerializeField] private float emojiPulseMinScale = 1f;
    [SerializeField] private float emojiPulseMaxScale = 1.2f;
    [SerializeField] private float emojiPulseDuration = 1.5f;

    [Header("===== 결과 화면 =====")]
    [SerializeField] private RectTransform resultCardRect;
    [SerializeField] private CanvasGroup resultCardCanvasGroup;

    [Header("===== 결과 - 메시지 =====")]
    [SerializeField] private RectTransform successMessageRect;
    [SerializeField] private CanvasGroup successMessageCanvasGroup;
    [SerializeField] private float resultSlideDistance = 20f;

    // 루프 트윈들
    private Tween _selectedGlowTween;
    private Tween _emojiPulseTween;

    #region Public API - 인트로

    /// <summary>
    /// 인트로 화면 등장 애니메이션
    /// </summary>
    public void PlayIntroAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 1. 인스트럭션 카드 슬라이드 업 + 페이드
        if (instructionCardRect != null && instructionCardCanvasGroup != null)
        {
            Vector2 basePos = instructionCardRect.anchoredPosition;
            instructionCardRect.anchoredPosition = basePos + Vector2.down * introSlideDistance;
            instructionCardCanvasGroup.alpha = 0f;

            seq.Append(instructionCardRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Join(instructionCardCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 2. 말풍선 슬라이드 (딜레이 0.3초)
        if (speechBubbleRect != null && speechBubbleCanvasGroup != null)
        {
            Vector2 basePos = speechBubbleRect.anchoredPosition;
            speechBubbleRect.anchoredPosition = basePos + Vector2.left * 20f;
            speechBubbleCanvasGroup.alpha = 0f;

            seq.Insert(0.3f, speechBubbleRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.3f, speechBubbleCanvasGroup.DOFade(1f, introAppearDuration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 액션 버튼들 순차 등장
    /// </summary>
    public void PlayActionButtonsAppear(RectTransform[] actionRects, CanvasGroup[] actionCanvasGroups, Action onComplete = null)
    {
        if (actionRects == null || actionRects.Length == 0)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();

        for (int i = 0; i < actionRects.Length; i++)
        {
            if (actionRects[i] == null) continue;

            Vector2 basePos = actionRects[i].anchoredPosition;
            actionRects[i].anchoredPosition = basePos + Vector2.left * 30f;

            if (actionCanvasGroups != null && i < actionCanvasGroups.Length && actionCanvasGroups[i] != null)
                actionCanvasGroups[i].alpha = 0f;

            float delay = 0.5f + i * 0.1f;

            seq.Insert(delay, actionRects[i]
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));

            if (actionCanvasGroups != null && i < actionCanvasGroups.Length && actionCanvasGroups[i] != null)
                seq.Insert(delay, actionCanvasGroups[i].DOFade(1f, introAppearDuration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - 액션 선택

    /// <summary>
    /// 액션 호버
    /// </summary>
    public void PlayActionHover(RectTransform actionRect)
    {
        if (actionRect == null) return;
        actionRect.DOScale(actionHoverScale, 0.1f).SetEase(Ease.OutQuad);
        actionRect.DOAnchorPosX(actionRect.anchoredPosition.x + actionHoverX, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 액션 호버 해제
    /// </summary>
    public void PlayActionUnhover(RectTransform actionRect, Vector2 originalPos)
    {
        if (actionRect == null) return;
        actionRect.DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
        actionRect.DOAnchorPos(originalPos, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 액션 탭
    /// </summary>
    public void PlayActionTap(RectTransform actionRect)
    {
        if (actionRect == null) return;
        actionRect.DOScale(actionTapScale, 0.05f).SetEase(Ease.OutQuad)
            .OnComplete(() => actionRect.DOScale(1f, 0.1f).SetEase(Ease.OutQuad));
    }

    /// <summary>
    /// 액션 선택 (체크마크 등장)
    /// </summary>
    public void PlayActionSelect(RectTransform checkmarkRect, Action onComplete = null)
    {
        if (checkmarkRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        checkmarkRect.gameObject.SetActive(true);
        checkmarkRect.localScale = Vector3.zero;

        checkmarkRect
            .DOScale(1f, 0.3f)
            .SetEase(Ease.OutBack, 2f)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 선택된 액션 글로우 펄스 시작
    /// </summary>
    public void StartSelectedGlowPulse(CanvasGroup glowCanvasGroup)
    {
        if (glowCanvasGroup == null) return;

        StopSelectedGlowPulse();

        glowCanvasGroup.gameObject.SetActive(true);
        glowCanvasGroup.alpha = selectedGlowMinAlpha;

        _selectedGlowTween = glowCanvasGroup
            .DOFade(selectedGlowMaxAlpha, selectedGlowDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 선택된 액션 글로우 펄스 정지
    /// </summary>
    public void StopSelectedGlowPulse()
    {
        _selectedGlowTween?.Kill();
        _selectedGlowTween = null;
    }

    /// <summary>
    /// 선택된 이모지 펄스 시작
    /// </summary>
    public void StartEmojiPulse(RectTransform emojiRect)
    {
        if (emojiRect == null) return;

        StopEmojiPulse();

        _emojiPulseTween = emojiRect
            .DOScale(emojiPulseMaxScale, emojiPulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .From(Vector3.one * emojiPulseMinScale);
    }

    /// <summary>
    /// 선택된 이모지 펄스 정지
    /// </summary>
    public void StopEmojiPulse()
    {
        _emojiPulseTween?.Kill();
        _emojiPulseTween = null;
    }

    #endregion

    #region Public API - 결과 화면

    /// <summary>
    /// 결과 화면 애니메이션
    /// </summary>
    public void PlayResultAnimation(Action onComplete = null)
    {
        StopSelectedGlowPulse();
        StopEmojiPulse();

        var seq = CreateSequence();

        // 1. 결과 카드 등장
        if (resultCardRect != null && resultCardCanvasGroup != null)
        {
            resultCardRect.localScale = Vector3.one * 0.9f;
            resultCardCanvasGroup.alpha = 0f;

            seq.Append(resultCardRect
                .DOScale(1f, 0.5f)
                .SetEase(Ease.OutQuad));
            seq.Join(resultCardCanvasGroup.DOFade(1f, 0.5f));
        }

        // 2. 성공 메시지 (딜레이 0.3초)
        if (successMessageRect != null && successMessageCanvasGroup != null)
        {
            Vector2 basePos = successMessageRect.anchoredPosition;
            successMessageRect.anchoredPosition = basePos + Vector2.down * resultSlideDistance;
            successMessageCanvasGroup.alpha = 0f;

            seq.Insert(0.3f, successMessageRect
                .DOAnchorPos(basePos, 0.5f)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.3f, successMessageCanvasGroup.DOFade(1f, 0.5f));
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
        StopSelectedGlowPulse();
        StopEmojiPulse();

        // 인트로 카드 리셋
        if (instructionCardCanvasGroup != null)
        {
            DOTween.Kill(instructionCardRect);
            DOTween.Kill(instructionCardCanvasGroup);
            instructionCardCanvasGroup.alpha = 0f;
        }

        if (speechBubbleCanvasGroup != null)
        {
            DOTween.Kill(speechBubbleRect);
            DOTween.Kill(speechBubbleCanvasGroup);
            speechBubbleCanvasGroup.alpha = 0f;
        }

        // 결과 화면 리셋
        if (resultCardCanvasGroup != null)
        {
            DOTween.Kill(resultCardRect);
            DOTween.Kill(resultCardCanvasGroup);
            resultCardCanvasGroup.alpha = 0f;
        }

        if (successMessageCanvasGroup != null)
        {
            DOTween.Kill(successMessageRect);
            DOTween.Kill(successMessageCanvasGroup);
            successMessageCanvasGroup.alpha = 0f;
        }
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        StopSelectedGlowPulse();
        StopEmojiPulse();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopSelectedGlowPulse();
        StopEmojiPulse();
    }
}
