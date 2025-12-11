using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 7 - Step 2 가면/진짜 감정 선택 이펙트 컨트롤러
/// - 카드 선택 애니메이션 (호버, 탭, 체크마크)
/// - 선택된 카드 글로우 펄스
/// - Reveal 화면: 캐릭터 고개 들기 + 따뜻한 빛
/// </summary>
public class Problem7_Step2_EffectController : EffectControllerBase
{
    [Header("===== 카드 선택 애니메이션 =====")]
    [SerializeField] private float cardHoverScale = 1.05f;
    [SerializeField] private float cardTapScale = 0.95f;
    [SerializeField] private float cardSelectDuration = 0.2f;

    [Header("===== 선택된 카드 글로우 =====")]
    [SerializeField] private float glowPulseMinScale = 1f;
    [SerializeField] private float glowPulseMaxScale = 1.1f;
    [SerializeField] private float glowPulseDuration = 2f;

    [Header("===== 체크마크 =====")]
    [SerializeField] private float checkmarkScaleDuration = 0.3f;

    [Header("===== Reveal 화면 =====")]
    [SerializeField] private RectTransform characterEmojiRect;
    [SerializeField] private float characterStartRotation = 15f;
    [SerializeField] private float characterLiftDuration = 1.5f;

    [Header("===== 따뜻한 빛 (Warm Glow) =====")]
    [SerializeField] private RectTransform warmGlowRect;
    [SerializeField] private CanvasGroup warmGlowCanvasGroup;
    [SerializeField] private float warmGlowTargetScale = 1.5f;
    [SerializeField] private float warmGlowTargetAlpha = 0.3f;
    [SerializeField] private float warmGlowDuration = 1.5f;

    [Header("===== Reveal 텍스트/버튼 =====")]
    [SerializeField] private RectTransform revealTextRect;
    [SerializeField] private CanvasGroup revealTextCanvasGroup;
    [SerializeField] private RectTransform revealButtonRect;
    [SerializeField] private CanvasGroup revealButtonCanvasGroup;
    [SerializeField] private float revealSlideDistance = 20f;
    [SerializeField] private float revealFadeDuration = 0.5f;

    // 글로우 펄스 트윈 관리
    private Tween _currentGlowPulseTween;

    #region Public API - 카드 선택

    /// <summary>
    /// 카드 호버
    /// </summary>
    public void PlayCardHover(RectTransform cardRect)
    {
        if (cardRect == null) return;
        cardRect.DOScale(cardHoverScale, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 카드 호버 해제
    /// </summary>
    public void PlayCardUnhover(RectTransform cardRect)
    {
        if (cardRect == null) return;
        cardRect.DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 카드 선택 (탭 → 바운스 + 체크마크)
    /// </summary>
    public void PlayCardSelect(RectTransform cardRect, RectTransform checkmarkRect = null, Action onComplete = null)
    {
        if (cardRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();

        // 탭 스케일: 0.95 → 1
        seq.Append(cardRect
            .DOScale(cardTapScale, cardSelectDuration * 0.3f)
            .SetEase(Ease.OutQuad));
        seq.Append(cardRect
            .DOScale(1f, cardSelectDuration * 0.7f)
            .SetEase(Ease.OutBack));

        // 체크마크 스프링 등장
        if (checkmarkRect != null)
        {
            checkmarkRect.gameObject.SetActive(true);
            checkmarkRect.localScale = Vector3.zero;

            seq.Insert(cardSelectDuration * 0.5f, checkmarkRect
                .DOScale(1f, checkmarkScaleDuration)
                .SetEase(Ease.OutBack, 2f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 카드 선택 해제
    /// </summary>
    public void PlayCardDeselect(RectTransform cardRect, RectTransform checkmarkRect = null, Action onComplete = null)
    {
        if (cardRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();

        seq.Append(cardRect
            .DOScale(cardTapScale, cardSelectDuration * 0.3f)
            .SetEase(Ease.OutQuad));
        seq.Append(cardRect
            .DOScale(1f, cardSelectDuration * 0.7f)
            .SetEase(Ease.OutQuad));

        // 체크마크 숨김
        if (checkmarkRect != null)
        {
            seq.Insert(0f, checkmarkRect
                .DOScale(0f, checkmarkScaleDuration * 0.5f)
                .SetEase(Ease.InBack));

            seq.InsertCallback(checkmarkScaleDuration * 0.5f, () =>
            {
                checkmarkRect.gameObject.SetActive(false);
            });
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 선택된 카드 글로우 펄스 시작
    /// </summary>
    public void StartSelectedGlowPulse(RectTransform glowRect)
    {
        if (glowRect == null) return;

        StopSelectedGlowPulse();

        glowRect.gameObject.SetActive(true);
        glowRect.localScale = Vector3.one * glowPulseMinScale;

        _currentGlowPulseTween = glowRect
            .DOScale(glowPulseMaxScale, glowPulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 선택된 카드 글로우 펄스 정지
    /// </summary>
    public void StopSelectedGlowPulse()
    {
        _currentGlowPulseTween?.Kill();
        _currentGlowPulseTween = null;
    }

    #endregion

    #region Public API - Reveal 화면

    /// <summary>
    /// Reveal 애니메이션 (캐릭터 고개 들기 + 따뜻한 빛 + 텍스트)
    /// </summary>
    public void PlayRevealAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 1. 캐릭터 고개 들기 (rotate 15 → 0, y -5 → 0)
        if (characterEmojiRect != null)
        {
            characterEmojiRect.localRotation = Quaternion.Euler(0, 0, characterStartRotation);
            Vector2 basePos = characterEmojiRect.anchoredPosition;
            characterEmojiRect.anchoredPosition = basePos + Vector2.down * 5f;

            seq.Append(characterEmojiRect
                .DORotate(Vector3.zero, characterLiftDuration)
                .SetEase(Ease.OutQuad));

            seq.Join(characterEmojiRect
                .DOAnchorPos(basePos, characterLiftDuration)
                .SetEase(Ease.OutQuad));
        }

        // 2. 따뜻한 빛 확장
        if (warmGlowRect != null && warmGlowCanvasGroup != null)
        {
            warmGlowRect.gameObject.SetActive(true);
            warmGlowRect.localScale = Vector3.zero;
            warmGlowCanvasGroup.alpha = 0f;

            seq.Insert(0f, warmGlowRect
                .DOScale(warmGlowTargetScale, warmGlowDuration)
                .SetEase(Ease.OutQuad));

            seq.Insert(0f, warmGlowCanvasGroup
                .DOFade(warmGlowTargetAlpha, warmGlowDuration)
                .SetEase(Ease.OutQuad));
        }

        // 3. 텍스트 슬라이드 업 + 페이드
        if (revealTextRect != null && revealTextCanvasGroup != null)
        {
            Vector2 basePos = revealTextRect.anchoredPosition;
            revealTextRect.anchoredPosition = basePos + Vector2.down * revealSlideDistance;
            revealTextCanvasGroup.alpha = 0f;

            seq.Insert(0.5f, revealTextRect
                .DOAnchorPos(basePos, revealFadeDuration)
                .SetEase(Ease.OutQuad));

            seq.Insert(0.5f, revealTextCanvasGroup
                .DOFade(1f, revealFadeDuration));
        }

        // 4. 버튼 슬라이드 업 + 페이드
        if (revealButtonRect != null && revealButtonCanvasGroup != null)
        {
            Vector2 basePos = revealButtonRect.anchoredPosition;
            revealButtonRect.anchoredPosition = basePos + Vector2.down * revealSlideDistance;
            revealButtonCanvasGroup.alpha = 0f;

            seq.Insert(1f, revealButtonRect
                .DOAnchorPos(basePos, revealFadeDuration)
                .SetEase(Ease.OutQuad));

            seq.Insert(1f, revealButtonCanvasGroup
                .DOFade(1f, revealFadeDuration));
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

        // 캐릭터 리셋
        if (characterEmojiRect != null)
        {
            DOTween.Kill(characterEmojiRect);
            characterEmojiRect.localRotation = Quaternion.Euler(0, 0, characterStartRotation);
        }

        // 따뜻한 빛 리셋
        if (warmGlowRect != null)
        {
            DOTween.Kill(warmGlowRect);
            warmGlowRect.localScale = Vector3.zero;
            warmGlowRect.gameObject.SetActive(false);
        }

        if (warmGlowCanvasGroup != null)
        {
            DOTween.Kill(warmGlowCanvasGroup);
            warmGlowCanvasGroup.alpha = 0f;
        }

        // 텍스트/버튼 리셋
        if (revealTextCanvasGroup != null)
        {
            DOTween.Kill(revealTextRect);
            DOTween.Kill(revealTextCanvasGroup);
            revealTextCanvasGroup.alpha = 0f;
        }

        if (revealButtonCanvasGroup != null)
        {
            DOTween.Kill(revealButtonRect);
            DOTween.Kill(revealButtonCanvasGroup);
            revealButtonCanvasGroup.alpha = 0f;
        }
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        StopSelectedGlowPulse();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopSelectedGlowPulse();
    }
}
