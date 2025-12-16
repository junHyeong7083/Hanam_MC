using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 7 - Step 1 이펙트 컨트롤러
/// - 메가폰 활성화 화면 애니메이션
/// </summary>
public class Problem7_Step1_EffectController : EffectControllerBase
{
    [Header("===== 활성화 화면 (CompleteRoot) =====")]
    [SerializeField] private GameObject activatedRoot;
    [SerializeField] private RectTransform megaphoneIconRect;
    [SerializeField] private RectTransform titleRect;
    [SerializeField] private CanvasGroup titleCanvasGroup;
    [SerializeField] private RectTransform descriptionRect;
    [SerializeField] private CanvasGroup descriptionCanvasGroup;
    [SerializeField] private RectTransform buttonRect;
    [SerializeField] private CanvasGroup buttonCanvasGroup;
    [SerializeField] private float activateSlideDistance = 20f;
    [SerializeField] private float activateDuration = 0.5f;

    #region Public API

    /// <summary>
    /// 활성화 화면 등장 애니메이션
    /// </summary>
    public void PlayActivatedScreenAnimation(Action onComplete = null)
    {
        // 활성화 루트 켜기
        if (activatedRoot != null)
            activatedRoot.SetActive(true);

        var seq = CreateSequence();

        // 1. 메가폰 아이콘 스프링 등장 (scale 0 → 1)
        if (megaphoneIconRect != null)
        {
            megaphoneIconRect.localScale = Vector3.zero;
            seq.Append(megaphoneIconRect
                .DOScale(1f, activateDuration)
                .SetEase(Ease.OutBack, 2f));
        }

        // 2. 타이틀 슬라이드 업 + 페이드 인
        if (titleRect != null && titleCanvasGroup != null)
        {
            Vector2 basePos = titleRect.anchoredPosition;
            titleRect.anchoredPosition = basePos + Vector2.down * activateSlideDistance;
            titleCanvasGroup.alpha = 0f;

            seq.Insert(0.3f, titleRect
                .DOAnchorPos(basePos, activateDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.3f, titleCanvasGroup
                .DOFade(1f, activateDuration));
        }

        // 3. 설명 텍스트 슬라이드 업 + 페이드 인
        if (descriptionRect != null && descriptionCanvasGroup != null)
        {
            Vector2 basePos = descriptionRect.anchoredPosition;
            descriptionRect.anchoredPosition = basePos + Vector2.down * activateSlideDistance;
            descriptionCanvasGroup.alpha = 0f;

            seq.Insert(0.5f, descriptionRect
                .DOAnchorPos(basePos, activateDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.5f, descriptionCanvasGroup
                .DOFade(1f, activateDuration));
        }

        // 4. 버튼 슬라이드 업 + 페이드 인
        if (buttonRect != null && buttonCanvasGroup != null)
        {
            Vector2 basePos = buttonRect.anchoredPosition;
            buttonRect.anchoredPosition = basePos + Vector2.down * activateSlideDistance;
            buttonCanvasGroup.alpha = 0f;

            seq.Insert(0.7f, buttonRect
                .DOAnchorPos(basePos, activateDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.7f, buttonCanvasGroup
                .DOFade(1f, activateDuration));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();

        // 활성화 화면 리셋
        if (activatedRoot != null)
            activatedRoot.SetActive(false);

        if (megaphoneIconRect != null)
        {
            DOTween.Kill(megaphoneIconRect);
            megaphoneIconRect.localScale = Vector3.zero;
        }

        if (titleCanvasGroup != null)
        {
            DOTween.Kill(titleRect);
            DOTween.Kill(titleCanvasGroup);
            titleCanvasGroup.alpha = 0f;
        }

        if (descriptionCanvasGroup != null)
        {
            DOTween.Kill(descriptionRect);
            DOTween.Kill(descriptionCanvasGroup);
            descriptionCanvasGroup.alpha = 0f;
        }

        if (buttonCanvasGroup != null)
        {
            DOTween.Kill(buttonRect);
            DOTween.Kill(buttonCanvasGroup);
            buttonCanvasGroup.alpha = 0f;
        }
    }

    #endregion
}
