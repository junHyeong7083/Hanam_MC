using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 6 - Step 2 스트레스 반응 카드 선택 이펙트 컨트롤러
/// - 카드 선택/해제 애니메이션만 담당
/// - 조명 깜빡임은 Director_Problem6_Step2_Logic에서 처리
/// </summary>
public class Problem6_Step2_EffectController : EffectControllerBase
{
    [Header("===== 카드 선택 애니메이션 =====")]
    [SerializeField] private float cardSelectDuration = 0.2f;
    [SerializeField] private float cardHoverScale = 1.05f;
    [SerializeField] private float cardTapScale = 0.95f;




    [Header("===== 카드 글로우 (선택됨) =====")]
    [SerializeField] private float glowPulseDuration = 2f;
    [SerializeField] private float glowMinScale = 1f;
    [SerializeField] private float glowMaxScale = 1.1f;
    [SerializeField] private float glowMinAlpha = 0.4f;
    [SerializeField] private float glowMaxAlpha = 0.6f;

    #region Public API - 카드

    /// <summary>
    /// 카드 호버 효과
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
    /// 카드 선택 애니메이션
    /// </summary>
    public void PlayCardSelect(RectTransform cardRect, Image glowImage = null, Action onComplete = null)
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

        seq.OnComplete(() =>
        {
            if (glowImage != null)
                StartGlowPulse(glowImage);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 카드 선택 해제 애니메이션
    /// </summary>
    public void PlayCardDeselect(RectTransform cardRect, Image glowImage = null, Action onComplete = null)
    {
        if (cardRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        if (glowImage != null)
            StopGlowPulse(glowImage);

        var seq = DOTween.Sequence();

        seq.Append(cardRect
            .DOScale(cardTapScale, cardSelectDuration * 0.3f)
            .SetEase(Ease.OutQuad));
        seq.Append(cardRect
            .DOScale(1f, cardSelectDuration * 0.7f)
            .SetEase(Ease.OutQuad));

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();
    }

    #endregion

    #region Glow Helpers

    private void StartGlowPulse(Image glowImage)
    {
        if (glowImage == null) return;

        DOTween.Kill(glowImage);
        DOTween.Kill(glowImage.transform);

        glowImage.gameObject.SetActive(true);

        Color c = glowImage.color;
        c.a = glowMinAlpha;
        glowImage.color = c;
        glowImage.transform.localScale = Vector3.one * glowMinScale;

        glowImage.transform.DOScale(glowMaxScale, glowPulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);

        glowImage.DOFade(glowMaxAlpha, glowPulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void StopGlowPulse(Image glowImage)
    {
        if (glowImage == null) return;

        DOTween.Kill(glowImage);
        DOTween.Kill(glowImage.transform);
        glowImage.gameObject.SetActive(false);
    }

    #endregion
}
