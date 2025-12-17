using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 9 - Step 2 대사 선택 이펙트 컨트롤러
/// - 시나리오 화면 등장 (어시스턴트 + 웹툰 장면 + 선택지)
/// - 선택지 호버/탭 효과
/// - OK컷/NG 결과 화면 애니메이션
/// - 진행도 점 업데이트
/// </summary>
public class Problem9_Step2_EffectController : EffectControllerBase
{
    [Header("===== 어시스턴트 카드 =====")]
    [SerializeField] private RectTransform assistantCardRect;
    [SerializeField] private CanvasGroup assistantCardCanvasGroup;
    [SerializeField] private float introSlideDistance = 30f;
    [SerializeField] private float introAppearDuration = 0.5f;

    [Header("===== 웹툰 장면 카드 =====")]
    [SerializeField] private RectTransform webtoonCardRect;
    [SerializeField] private CanvasGroup webtoonCardCanvasGroup;

    [Header("===== 선택지 카드 =====")]
    [SerializeField] private RectTransform choicesCardRect;
    [SerializeField] private CanvasGroup choicesCardCanvasGroup;

    [Header("===== 선택지 버튼들 =====")]
    [SerializeField] private RectTransform[] choiceButtonRects;
    [SerializeField] private CanvasGroup[] choiceButtonCanvasGroups;

    [Header("===== OK 결과 화면 =====")]
    [SerializeField] private RectTransform okResultCardRect;
    [SerializeField] private CanvasGroup okResultCardCanvasGroup;

    [Header("===== NG 결과 화면 =====")]
    [SerializeField] private RectTransform ngResultCardRect;
    [SerializeField] private CanvasGroup ngResultCardCanvasGroup;

    [Header("===== 진행도 점 =====")]
    [SerializeField] private RectTransform[] progressDotRects;

    #region Public API - 시나리오 화면

    /// <summary>
    /// 시나리오 화면 등장 애니메이션
    /// </summary>
    public void PlayScenarioEnterAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 1. 어시스턴트 카드 슬라이드 업 + 페이드
        if (assistantCardRect != null && assistantCardCanvasGroup != null)
        {
            Vector2 basePos = assistantCardRect.anchoredPosition;
            assistantCardRect.anchoredPosition = basePos + Vector2.down * introSlideDistance;
            assistantCardCanvasGroup.alpha = 0f;

            seq.Append(assistantCardRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Join(assistantCardCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 2. 웹툰 장면 카드 스케일 + 페이드 (딜레이 0.3초)
        if (webtoonCardRect != null && webtoonCardCanvasGroup != null)
        {
            webtoonCardRect.localScale = Vector3.one * 0.95f;
            webtoonCardCanvasGroup.alpha = 0f;

            seq.Insert(0.3f, webtoonCardRect
                .DOScale(1f, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.3f, webtoonCardCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 3. 선택지 카드 슬라이드 업 + 페이드 (딜레이 0.5초)
        if (choicesCardRect != null && choicesCardCanvasGroup != null)
        {
            Vector2 basePos = choicesCardRect.anchoredPosition;
            choicesCardRect.anchoredPosition = basePos + Vector2.down * introSlideDistance;
            choicesCardCanvasGroup.alpha = 0f;

            seq.Insert(0.5f, choicesCardRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.5f, choicesCardCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 4. 선택지 버튼들 순차 등장 (딜레이 0.6초+)
        if (choiceButtonRects != null && choiceButtonCanvasGroups != null)
        {
            for (int i = 0; i < choiceButtonRects.Length && i < choiceButtonCanvasGroups.Length; i++)
            {
                if (choiceButtonRects[i] == null || choiceButtonCanvasGroups[i] == null) continue;

                Vector2 basePos = choiceButtonRects[i].anchoredPosition;
                choiceButtonRects[i].anchoredPosition = basePos + Vector2.left * 20f;
                choiceButtonCanvasGroups[i].alpha = 0f;

                float delay = 0.6f + i * 0.1f;

                seq.Insert(delay, choiceButtonRects[i]
                    .DOAnchorPos(basePos, 0.3f)
                    .SetEase(Ease.OutQuad));
                seq.Insert(delay, choiceButtonCanvasGroups[i].DOFade(1f, 0.3f));
            }
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    /// <summary>
    /// 시나리오 전환 애니메이션 (다음 시나리오로)
    /// </summary>
    public void PlayScenarioTransition(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 페이드 아웃
        if (webtoonCardCanvasGroup != null)
            seq.Append(webtoonCardCanvasGroup.DOFade(0f, 0.2f));
        if (choicesCardCanvasGroup != null)
            seq.Join(choicesCardCanvasGroup.DOFade(0f, 0.2f));

        // 페이드 인
        seq.AppendCallback(() =>
        {
            if (webtoonCardCanvasGroup != null)
            {
                webtoonCardRect.localScale = Vector3.one * 0.95f;
                webtoonCardCanvasGroup.alpha = 0f;
            }
        });

        if (webtoonCardRect != null && webtoonCardCanvasGroup != null)
        {
            seq.Append(webtoonCardRect.DOScale(1f, 0.3f).SetEase(Ease.OutQuad));
            seq.Join(webtoonCardCanvasGroup.DOFade(1f, 0.3f));
        }

        if (choicesCardCanvasGroup != null)
            seq.Join(choicesCardCanvasGroup.DOFade(1f, 0.3f));

        // 선택지 버튼들 순차 등장
        if (choiceButtonRects != null && choiceButtonCanvasGroups != null)
        {
            for (int i = 0; i < choiceButtonRects.Length && i < choiceButtonCanvasGroups.Length; i++)
            {
                if (choiceButtonRects[i] == null || choiceButtonCanvasGroups[i] == null) continue;

                Vector2 basePos = choiceButtonRects[i].anchoredPosition;
                choiceButtonRects[i].anchoredPosition = basePos + Vector2.left * 20f;
                choiceButtonCanvasGroups[i].alpha = 0f;

                float delay = 0.3f + i * 0.1f;

                seq.Insert(delay, choiceButtonRects[i]
                    .DOAnchorPos(basePos, 0.3f)
                    .SetEase(Ease.OutQuad));
                seq.Insert(delay, choiceButtonCanvasGroups[i].DOFade(1f, 0.3f));
            }
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - OK 결과 화면

    /// <summary>
    /// OK컷 결과 화면 애니메이션 (카드 스케일 + 페이드)
    /// </summary>
    public void PlayOkResultAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 카드 스케일 + 페이드
        if (okResultCardRect != null && okResultCardCanvasGroup != null)
        {
            okResultCardRect.localScale = Vector3.one * 0.9f;
            okResultCardCanvasGroup.alpha = 0f;

            seq.Append(okResultCardRect
                .DOScale(1f, 0.4f)
                .SetEase(Ease.OutQuad));
            seq.Join(okResultCardCanvasGroup.DOFade(1f, 0.4f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - NG 결과 화면

    /// <summary>
    /// NG 결과 화면 애니메이션 (카드 스케일 + 페이드)
    /// </summary>
    public void PlayNgResultAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 카드 스케일 + 페이드
        if (ngResultCardRect != null && ngResultCardCanvasGroup != null)
        {
            ngResultCardRect.localScale = Vector3.one * 0.9f;
            ngResultCardCanvasGroup.alpha = 0f;

            seq.Append(ngResultCardRect
                .DOScale(1f, 0.4f)
                .SetEase(Ease.OutQuad));
            seq.Join(ngResultCardCanvasGroup.DOFade(1f, 0.4f));
        }

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - 진행도 점

    /// <summary>
    /// 진행도 점 완료 애니메이션
    /// </summary>
    public void PlayProgressDotComplete(int index)
    {
        if (progressDotRects == null || index >= progressDotRects.Length) return;
        if (progressDotRects[index] == null) return;

        progressDotRects[index]
            .DOScale(1.3f, 0.2f)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => progressDotRects[index]
                .DOScale(1f, 0.1f)
                .SetEase(Ease.InQuad));
    }

    #endregion

    #region Reset

    /// <summary>
    /// 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();

        // 어시스턴트 카드 리셋
        if (assistantCardCanvasGroup != null)
        {
            DOTween.Kill(assistantCardRect);
            DOTween.Kill(assistantCardCanvasGroup);
            assistantCardCanvasGroup.alpha = 0f;
        }

        // 웹툰 카드 리셋
        if (webtoonCardCanvasGroup != null)
        {
            DOTween.Kill(webtoonCardRect);
            DOTween.Kill(webtoonCardCanvasGroup);
            webtoonCardCanvasGroup.alpha = 0f;
        }

        // 선택지 카드 리셋
        if (choicesCardCanvasGroup != null)
        {
            DOTween.Kill(choicesCardRect);
            DOTween.Kill(choicesCardCanvasGroup);
            choicesCardCanvasGroup.alpha = 0f;
        }

        // 선택지 버튼들 리셋
        if (choiceButtonRects != null)
        {
            foreach (var rect in choiceButtonRects)
            {
                if (rect != null)
                {
                    DOTween.Kill(rect);
                    rect.localScale = Vector3.one;
                }
            }
        }

        if (choiceButtonCanvasGroups != null)
        {
            foreach (var cg in choiceButtonCanvasGroups)
            {
                if (cg != null)
                {
                    DOTween.Kill(cg);
                    cg.alpha = 0f;
                }
            }
        }

        // OK 결과 리셋 (카드만)
        if (okResultCardCanvasGroup != null)
        {
            DOTween.Kill(okResultCardRect);
            DOTween.Kill(okResultCardCanvasGroup);
            okResultCardCanvasGroup.alpha = 0f;
        }

        // NG 결과 리셋 (카드만)
        if (ngResultCardCanvasGroup != null)
        {
            DOTween.Kill(ngResultCardRect);
            DOTween.Kill(ngResultCardCanvasGroup);
            ngResultCardCanvasGroup.alpha = 0f;
        }
    }

    #endregion
}
