using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 10 - Step 2 장르 선택 이펙트 컨트롤러
/// - 어시스턴트 말풍선 등장
/// - 4개 장르 카드 순차 등장
/// - 선택 시 글로우 + 체크마크 애니메이션
/// - 확인 버튼 등장
/// </summary>
public class Problem10_Step2_EffectController : EffectControllerBase
{
    [Header("===== 어시스턴트 말풍선 =====")]
    [SerializeField] private RectTransform assistantCardRect;
    [SerializeField] private CanvasGroup assistantCardCanvasGroup;
    [SerializeField] private float introSlideDistance = 30f;
    [SerializeField] private float introAppearDuration = 0.5f;

    [Header("===== 장르 카드들 =====")]
    [SerializeField] private RectTransform[] genreCardRects;
    [SerializeField] private CanvasGroup[] genreCardCanvasGroups;
    [SerializeField] private float cardAppearDelay = 0.1f;

    [Header("===== 선택 글로우 =====")]
    [SerializeField] private RectTransform[] glowRects;
    [SerializeField] private float glowPulseDuration = 2f;

    [Header("===== 선택 체크마크 =====")]
    [SerializeField] private RectTransform[] checkmarkRects;

    [Header("===== 확인 버튼 =====")]
    [SerializeField] private RectTransform confirmButtonRect;
    [SerializeField] private CanvasGroup confirmButtonCanvasGroup;

    // 루프 트윈
    private Tween _selectedGlowTween;
    private int _currentGlowIndex = -1;

    #region Public API - 인트로 화면

    /// <summary>
    /// 인트로 화면 등장 애니메이션
    /// </summary>
    public void PlayIntroAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 1. 어시스턴트 말풍선 슬라이드 업 + 페이드
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

        // 2. 장르 카드들 순차 등장 (딜레이 0.2초부터)
        if (genreCardRects != null && genreCardCanvasGroups != null)
        {
            for (int i = 0; i < genreCardRects.Length && i < genreCardCanvasGroups.Length; i++)
            {
                if (genreCardRects[i] == null || genreCardCanvasGroups[i] == null) continue;

                Vector2 basePos = genreCardRects[i].anchoredPosition;
                genreCardRects[i].anchoredPosition = basePos + Vector2.down * introSlideDistance;
                genreCardCanvasGroups[i].alpha = 0f;

                float delay = 0.2f + i * cardAppearDelay;

                seq.Insert(delay, genreCardRects[i]
                    .DOAnchorPos(basePos, introAppearDuration)
                    .SetEase(Ease.OutQuad));
                seq.Insert(delay, genreCardCanvasGroups[i].DOFade(1f, introAppearDuration));
            }
        }

        // 3. 체크마크들 초기화 (숨김)
        HideAllCheckmarks();

        // 4. 글로우들 초기화 (숨김)
        HideAllGlows();

        // 5. 확인 버튼 초기화 (숨김)
        if (confirmButtonCanvasGroup != null)
            confirmButtonCanvasGroup.alpha = 0f;

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - 카드 선택

    /// <summary>
    /// 카드 선택 애니메이션
    /// </summary>
    public void PlayCardSelectedAnimation(int index)
    {
        // 이전 선택 해제
        if (_currentGlowIndex >= 0 && _currentGlowIndex != index)
        {
            HideGlow(_currentGlowIndex);
            HideCheckmark(_currentGlowIndex);
        }

        _currentGlowIndex = index;

        // 글로우 시작
        ShowGlow(index);

        // 체크마크 스프링 등장
        ShowCheckmark(index);

        // 확인 버튼 등장 (처음 선택 시)
        ShowConfirmButton();
    }

    private void ShowGlow(int index)
    {
        if (glowRects == null || index >= glowRects.Length || glowRects[index] == null) return;

        _selectedGlowTween?.Kill();

        glowRects[index].gameObject.SetActive(true);
        glowRects[index].localScale = Vector3.one;

        _selectedGlowTween = glowRects[index]
            .DOScale(1.1f, glowPulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    private void HideGlow(int index)
    {
        if (glowRects == null || index >= glowRects.Length || glowRects[index] == null) return;

        if (_currentGlowIndex == index)
        {
            _selectedGlowTween?.Kill();
            _selectedGlowTween = null;
        }

        DOTween.Kill(glowRects[index]);
        glowRects[index].localScale = Vector3.one;
        glowRects[index].gameObject.SetActive(false);
    }

    private void HideAllGlows()
    {
        _selectedGlowTween?.Kill();
        _selectedGlowTween = null;

        if (glowRects == null) return;

        foreach (var glow in glowRects)
        {
            if (glow != null)
            {
                DOTween.Kill(glow);
                glow.localScale = Vector3.one;
                glow.gameObject.SetActive(false);
            }
        }
    }

    private void ShowCheckmark(int index)
    {
        if (checkmarkRects == null || index >= checkmarkRects.Length || checkmarkRects[index] == null) return;

        checkmarkRects[index].gameObject.SetActive(true);
        checkmarkRects[index].localScale = Vector3.zero;

        checkmarkRects[index]
            .DOScale(1f, 0.4f)
            .SetEase(Ease.OutBack, 2f);
    }

    private void HideCheckmark(int index)
    {
        if (checkmarkRects == null || index >= checkmarkRects.Length || checkmarkRects[index] == null) return;

        DOTween.Kill(checkmarkRects[index]);
        checkmarkRects[index].localScale = Vector3.zero;
        checkmarkRects[index].gameObject.SetActive(false);
    }

    private void HideAllCheckmarks()
    {
        if (checkmarkRects == null) return;

        foreach (var check in checkmarkRects)
        {
            if (check != null)
            {
                DOTween.Kill(check);
                check.localScale = Vector3.zero;
                check.gameObject.SetActive(false);
            }
        }
    }

    private void ShowConfirmButton()
    {
        if (confirmButtonRect == null || confirmButtonCanvasGroup == null) return;

        // 이미 보이면 스킵
        if (confirmButtonCanvasGroup.alpha > 0.9f) return;

        Vector2 basePos = confirmButtonRect.anchoredPosition;
        confirmButtonRect.anchoredPosition = basePos + Vector2.down * 20f;
        confirmButtonCanvasGroup.alpha = 0f;

        confirmButtonRect
            .DOAnchorPos(basePos, 0.3f)
            .SetEase(Ease.OutQuad);
        confirmButtonCanvasGroup.DOFade(1f, 0.3f);
    }

    #endregion

    #region Reset

    /// <summary>
    /// 리셋
    /// </summary>
    public void ResetAll()
    {
        KillCurrentSequence();
        _selectedGlowTween?.Kill();
        _selectedGlowTween = null;
        _currentGlowIndex = -1;

        // 어시스턴트 카드 리셋
        if (assistantCardCanvasGroup != null)
        {
            DOTween.Kill(assistantCardRect);
            DOTween.Kill(assistantCardCanvasGroup);
            assistantCardCanvasGroup.alpha = 0f;
        }

        // 장르 카드들 리셋
        if (genreCardRects != null && genreCardCanvasGroups != null)
        {
            for (int i = 0; i < genreCardRects.Length && i < genreCardCanvasGroups.Length; i++)
            {
                if (genreCardRects[i] != null)
                    DOTween.Kill(genreCardRects[i]);
                if (genreCardCanvasGroups[i] != null)
                {
                    DOTween.Kill(genreCardCanvasGroups[i]);
                    genreCardCanvasGroups[i].alpha = 0f;
                }
            }
        }

        // 글로우 리셋
        HideAllGlows();

        // 체크마크 리셋
        HideAllCheckmarks();

        // 확인 버튼 리셋
        if (confirmButtonCanvasGroup != null)
        {
            DOTween.Kill(confirmButtonRect);
            DOTween.Kill(confirmButtonCanvasGroup);
            confirmButtonCanvasGroup.alpha = 0f;
        }
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        _selectedGlowTween?.Kill();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        _selectedGlowTween?.Kill();
    }
}
