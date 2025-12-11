using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 8 - Step 2 스토리보드 채우기 이펙트 컨트롤러
/// - 인트로 카드 등장
/// - 슬롯 채워질 때 애니메이션 (바운스, 글로우, 체크마크)
/// - 완료 시 연결선 애니메이션
/// - 최종 화면 (필름 스트립, 스파클, 버튼)
/// </summary>
public class Problem8_Step2_EffectController : EffectControllerBase
{
    [Header("===== 인트로 카드 =====")]
    [SerializeField] private RectTransform instructionCardRect;
    [SerializeField] private CanvasGroup instructionCardCanvasGroup;
    [SerializeField] private RectTransform speechBubbleRect;
    [SerializeField] private CanvasGroup speechBubbleCanvasGroup;
    [SerializeField] private float introSlideDistance = 30f;
    [SerializeField] private float introAppearDuration = 0.5f;

    [Header("===== 스토리보드 슬롯 영역 =====")]
    [SerializeField] private RectTransform storyboardCardRect;
    [SerializeField] private CanvasGroup storyboardCardCanvasGroup;

    [Header("===== 슬롯 채우기 애니메이션 =====")]
    [SerializeField] private float slotFillScaleMax = 1.05f;
    [SerializeField] private float slotFillDuration = 0.5f;

    [Header("===== 슬롯 글로우 =====")]
    [SerializeField] private float slotGlowTargetAlpha = 0.6f;
    [SerializeField] private float slotGlowDuration = 0.3f;

    [Header("===== 체크마크 =====")]
    [SerializeField] private float checkmarkScaleDuration = 0.3f;

    [Header("===== 카드 영역 =====")]
    [SerializeField] private RectTransform availableCardsRect;
    [SerializeField] private CanvasGroup availableCardsCanvasGroup;

    [Header("===== 카드 힌트 배지 =====")]
    [SerializeField] private float hintBadgeBounceDistance = 5f;
    [SerializeField] private float hintBadgeBounceDuration = 1.5f;

    [Header("===== 연결선 (완료 시) =====")]
    [SerializeField] private Image connectionLineImage;
    [SerializeField] private float lineAnimDuration = 2f;

    [Header("===== 최종 화면 =====")]
    [SerializeField] private GameObject finalPictureRoot;
    [SerializeField] private CanvasGroup finalPictureCanvasGroup;

    [Header("===== 최종 화면 - 메인 콘텐츠 =====")]
    [SerializeField] private RectTransform finalIconRect;
    [SerializeField] private RectTransform finalTitleRect;
    [SerializeField] private CanvasGroup finalTitleCanvasGroup;
    [SerializeField] private RectTransform finalSubtitleRect;
    [SerializeField] private CanvasGroup finalSubtitleCanvasGroup;
    [SerializeField] private float finalSlideDistance = 50f;
    [SerializeField] private float finalAppearDuration = 0.5f;

    [Header("===== 최종 화면 - 버튼 =====")]
    [SerializeField] private RectTransform continueButtonRect;
    [SerializeField] private CanvasGroup continueButtonCanvasGroup;

    [Header("===== 최종 화면 - 하단 텍스트 =====")]
    [SerializeField] private CanvasGroup bottomTextCanvasGroup;

    // 힌트 배지 트윈들
    private Tween[] _hintBadgeTweens;

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

        // 3. 스토리보드 카드 등장 (딜레이 0.5초)
        if (storyboardCardRect != null && storyboardCardCanvasGroup != null)
        {
            Vector2 basePos = storyboardCardRect.anchoredPosition;
            storyboardCardRect.anchoredPosition = basePos + Vector2.down * introSlideDistance;
            storyboardCardCanvasGroup.alpha = 0f;

            seq.Insert(0.5f, storyboardCardRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.5f, storyboardCardCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 4. 카드 영역 등장 (딜레이 0.7초)
        if (availableCardsRect != null && availableCardsCanvasGroup != null)
        {
            Vector2 basePos = availableCardsRect.anchoredPosition;
            availableCardsRect.anchoredPosition = basePos + Vector2.down * introSlideDistance;
            availableCardsCanvasGroup.alpha = 0f;

            seq.Insert(0.7f, availableCardsRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.7f, availableCardsCanvasGroup.DOFade(1f, introAppearDuration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - 슬롯 채우기

    /// <summary>
    /// 슬롯 채워질 때 애니메이션
    /// </summary>
    public void PlaySlotFillAnimation(RectTransform slotRect, RectTransform glowRect = null,
        RectTransform contentRect = null, RectTransform checkmarkRect = null, Action onComplete = null)
    {
        if (slotRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();

        // 1. 슬롯 스케일 바운스: 1 → 1.05 → 1
        seq.Append(slotRect
            .DOScale(slotFillScaleMax, slotFillDuration * 0.5f)
            .SetEase(Ease.OutQuad));
        seq.Append(slotRect
            .DOScale(1f, slotFillDuration * 0.5f)
            .SetEase(Ease.OutQuad));

        // 2. 글로우 등장
        if (glowRect != null)
        {
            glowRect.gameObject.SetActive(true);
            glowRect.localScale = Vector3.one * 0.8f;

            var glowCG = glowRect.GetComponent<CanvasGroup>();
            if (glowCG != null)
            {
                glowCG.alpha = 0f;
                seq.Insert(0f, glowCG.DOFade(slotGlowTargetAlpha, slotGlowDuration));
            }

            seq.Insert(0f, glowRect
                .DOScale(1f, slotGlowDuration)
                .SetEase(Ease.OutQuad));
        }

        // 3. 콘텐츠 회전 등장: rotate -180 → 0, scale 0 → 1
        if (contentRect != null)
        {
            contentRect.gameObject.SetActive(true);
            contentRect.localScale = Vector3.zero;
            contentRect.localRotation = Quaternion.Euler(0, 0, -180);

            seq.Insert(0f, contentRect
                .DOScale(1f, slotFillDuration)
                .SetEase(Ease.OutBack));
            seq.Insert(0f, contentRect
                .DORotate(Vector3.zero, slotFillDuration)
                .SetEase(Ease.OutBack));
        }

        // 4. 체크마크 스프링 등장
        if (checkmarkRect != null)
        {
            checkmarkRect.gameObject.SetActive(true);
            checkmarkRect.localScale = Vector3.zero;

            seq.Insert(slotFillDuration * 0.5f, checkmarkRect
                .DOScale(1f, checkmarkScaleDuration)
                .SetEase(Ease.OutBack, 2f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 카드 호버
    /// </summary>
    public void PlayCardHover(RectTransform cardRect)
    {
        if (cardRect == null) return;
        cardRect.DOScale(1.05f, 0.1f).SetEase(Ease.OutQuad);
        cardRect.DORotate(new Vector3(0, 0, 2), 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 카드 호버 해제
    /// </summary>
    public void PlayCardUnhover(RectTransform cardRect)
    {
        if (cardRect == null) return;
        cardRect.DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
        cardRect.DORotate(Vector3.zero, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 카드 탭
    /// </summary>
    public void PlayCardTap(RectTransform cardRect)
    {
        if (cardRect == null) return;
        cardRect.DOScale(0.95f, 0.05f).SetEase(Ease.OutQuad)
            .OnComplete(() => cardRect.DOScale(1f, 0.1f).SetEase(Ease.OutQuad));
    }

    /// <summary>
    /// 힌트 배지 바운스 시작
    /// </summary>
    public void StartHintBadgeBounce(RectTransform[] badgeRects)
    {
        StopHintBadgeBounce();

        if (badgeRects == null || badgeRects.Length == 0) return;

        _hintBadgeTweens = new Tween[badgeRects.Length];

        for (int i = 0; i < badgeRects.Length; i++)
        {
            if (badgeRects[i] == null) continue;

            _hintBadgeTweens[i] = badgeRects[i]
                .DOAnchorPosY(badgeRects[i].anchoredPosition.y + hintBadgeBounceDistance, hintBadgeBounceDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    /// <summary>
    /// 힌트 배지 바운스 정지
    /// </summary>
    public void StopHintBadgeBounce()
    {
        if (_hintBadgeTweens != null)
        {
            for (int i = 0; i < _hintBadgeTweens.Length; i++)
            {
                _hintBadgeTweens[i]?.Kill();
                _hintBadgeTweens[i] = null;
            }
        }
    }

    #endregion

    #region Public API - 완료

    /// <summary>
    /// 연결선 애니메이션 (모든 슬롯 채워졌을 때)
    /// </summary>
    public void PlayConnectionLineAnimation(Action onComplete = null)
    {
        if (connectionLineImage == null)
        {
            onComplete?.Invoke();
            return;
        }

        connectionLineImage.gameObject.SetActive(true);
        connectionLineImage.fillAmount = 0f;

        connectionLineImage
            .DOFillAmount(1f, lineAnimDuration)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 최종 화면 애니메이션
    /// </summary>
    public void PlayFinalPictureAnimation(Action onComplete = null)
    {
        if (finalPictureRoot == null)
        {
            onComplete?.Invoke();
            return;
        }

        finalPictureRoot.SetActive(true);

        var seq = CreateSequence();

        // 1. 전체 페이드 인 + 스케일
        if (finalPictureCanvasGroup != null)
        {
            finalPictureCanvasGroup.alpha = 0f;
            var finalRect = finalPictureRoot.GetComponent<RectTransform>();
            if (finalRect != null)
            {
                finalRect.localScale = Vector3.one * 0.95f;
                seq.Append(finalRect.DOScale(1f, finalAppearDuration).SetEase(Ease.OutQuad));
            }
            seq.Join(finalPictureCanvasGroup.DOFade(1f, finalAppearDuration));
        }

        // 2. 아이콘 등장 (딜레이 0.3초)
        if (finalIconRect != null)
        {
            finalIconRect.localScale = Vector3.zero;
            seq.Insert(0.3f, finalIconRect
                .DOScale(1f, finalAppearDuration)
                .SetEase(Ease.OutBack, 2f));
        }

        // 3. 타이틀 슬라이드 업 + 페이드
        if (finalTitleRect != null && finalTitleCanvasGroup != null)
        {
            Vector2 basePos = finalTitleRect.anchoredPosition;
            finalTitleRect.anchoredPosition = basePos + Vector2.down * finalSlideDistance;
            finalTitleCanvasGroup.alpha = 0f;

            seq.Insert(0.3f, finalTitleRect
                .DOAnchorPos(basePos, finalAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.3f, finalTitleCanvasGroup.DOFade(1f, finalAppearDuration));
        }

        // 4. 서브타이틀
        if (finalSubtitleRect != null && finalSubtitleCanvasGroup != null)
        {
            finalSubtitleCanvasGroup.alpha = 0f;
            seq.Insert(0.5f, finalSubtitleCanvasGroup.DOFade(1f, finalAppearDuration));
        }

        // 5. 버튼 스프링 등장 (딜레이 0.6초)
        if (continueButtonRect != null)
        {
            continueButtonRect.localScale = Vector3.zero;

            if (continueButtonCanvasGroup != null)
                continueButtonCanvasGroup.alpha = 0f;

            seq.Insert(0.6f, continueButtonRect
                .DOScale(1f, finalAppearDuration)
                .SetEase(Ease.OutBack, 2f));

            if (continueButtonCanvasGroup != null)
                seq.Insert(0.6f, continueButtonCanvasGroup.DOFade(1f, finalAppearDuration));
        }

        // 6. 하단 텍스트 (딜레이 1초)
        if (bottomTextCanvasGroup != null)
        {
            bottomTextCanvasGroup.alpha = 0f;
            seq.Insert(1f, bottomTextCanvasGroup.DOFade(1f, finalAppearDuration));
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
        StopHintBadgeBounce();

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

        // 스토리보드 카드 리셋
        if (storyboardCardCanvasGroup != null)
        {
            DOTween.Kill(storyboardCardRect);
            DOTween.Kill(storyboardCardCanvasGroup);
            storyboardCardCanvasGroup.alpha = 0f;
        }

        // 카드 영역 리셋
        if (availableCardsCanvasGroup != null)
        {
            DOTween.Kill(availableCardsRect);
            DOTween.Kill(availableCardsCanvasGroup);
            availableCardsCanvasGroup.alpha = 0f;
        }

        // 연결선 리셋
        if (connectionLineImage != null)
        {
            DOTween.Kill(connectionLineImage);
            connectionLineImage.fillAmount = 0f;
            connectionLineImage.gameObject.SetActive(false);
        }

        // 최종 화면 리셋
        if (finalPictureRoot != null)
            finalPictureRoot.SetActive(false);

        if (finalPictureCanvasGroup != null)
        {
            DOTween.Kill(finalPictureCanvasGroup);
            finalPictureCanvasGroup.alpha = 0f;
        }

        if (finalIconRect != null)
        {
            DOTween.Kill(finalIconRect);
            finalIconRect.localScale = Vector3.zero;
        }

        if (finalTitleCanvasGroup != null)
        {
            DOTween.Kill(finalTitleRect);
            DOTween.Kill(finalTitleCanvasGroup);
            finalTitleCanvasGroup.alpha = 0f;
        }

        if (continueButtonRect != null)
        {
            DOTween.Kill(continueButtonRect);
            continueButtonRect.localScale = Vector3.zero;
        }

        if (bottomTextCanvasGroup != null)
        {
            DOTween.Kill(bottomTextCanvasGroup);
            bottomTextCanvasGroup.alpha = 0f;
        }
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        StopHintBadgeBounce();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopHintBadgeBounce();
    }
}
