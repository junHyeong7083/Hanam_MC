using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 10 - Step 3 포스터 완성 이펙트 컨트롤러
/// - 포스터 프리뷰 등장
/// - 마이크 녹음 펄스
/// - 제목/다짐 텍스트 등장
/// - 완료 화면 (Star 아이콘 + 황금 글로우)
/// </summary>
public class Problem10_Step3_EffectController : EffectControllerBase
{
    [Header("===== 포스터 프리뷰 =====")]
    [SerializeField] private RectTransform posterPreviewRect;
    [SerializeField] private CanvasGroup posterPreviewCanvasGroup;
    [SerializeField] private float introAppearDuration = 0.5f;

    [Header("===== 어시스턴트 말풍선 =====")]
    [SerializeField] private RectTransform assistantCardRect;
    [SerializeField] private CanvasGroup assistantCardCanvasGroup;
    [SerializeField] private float introSlideDistance = 30f;

    [Header("===== 마이크 버튼 =====")]
    [SerializeField] private RectTransform micButtonRect;
    [SerializeField] private float micPulseScale = 1.2f;
    [SerializeField] private float micPulseDuration = 1f;

    [Header("===== 포스터 제목 텍스트 =====")]
    [SerializeField] private RectTransform posterTitleRect;
    [SerializeField] private CanvasGroup posterTitleCanvasGroup;

    [Header("===== 포스터 다짐 텍스트 =====")]
    [SerializeField] private RectTransform posterCommitmentRect;
    [SerializeField] private CanvasGroup posterCommitmentCanvasGroup;

    [Header("===== 완료 화면 =====")]
    [SerializeField] private RectTransform completeCardRect;
    [SerializeField] private CanvasGroup completeCardCanvasGroup;

    [Header("===== 완료 - Star 아이콘 =====")]
    [SerializeField] private RectTransform starIconRect;

    [Header("===== 완료 - 최종 포스터 =====")]
    [SerializeField] private RectTransform finalPosterRect;

    [Header("===== 완료 - 어시스턴트 피드백 =====")]
    [SerializeField] private RectTransform feedbackCardRect;
    [SerializeField] private CanvasGroup feedbackCardCanvasGroup;

    // 루프 트윈
    private Tween _micPulseTween;

    #region Public API - 인트로 화면

    /// <summary>
    /// 인트로 화면 등장 애니메이션
    /// </summary>
    public void PlayIntroAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 1. 포스터 프리뷰 스케일 등장
        if (posterPreviewRect != null && posterPreviewCanvasGroup != null)
        {
            posterPreviewRect.localScale = Vector3.one * 0.9f;
            posterPreviewCanvasGroup.alpha = 0f;

            seq.Append(posterPreviewRect
                .DOScale(1f, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Join(posterPreviewCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 2. 어시스턴트 말풍선 슬라이드 업 (딜레이 0.3초)
        if (assistantCardRect != null && assistantCardCanvasGroup != null)
        {
            Vector2 basePos = assistantCardRect.anchoredPosition;
            assistantCardRect.anchoredPosition = basePos + Vector2.down * introSlideDistance;
            assistantCardCanvasGroup.alpha = 0f;

            seq.Insert(0.3f, assistantCardRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.3f, assistantCardCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 3. 포스터 제목/다짐 초기화 (숨김)
        HidePosterTitle();
        HidePosterCommitment();

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - 마이크 녹음

    /// <summary>
    /// 녹음 시작 애니메이션 (마이크 펄스)
    /// </summary>
    public void StartRecordingAnimation()
    {
        StopRecordingAnimation();

        if (micButtonRect == null) return;

        _micPulseTween = micButtonRect
            .DOScale(micPulseScale, micPulseDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .From(Vector3.one);
    }

    /// <summary>
    /// 녹음 정지 애니메이션
    /// </summary>
    public void StopRecordingAnimation()
    {
        _micPulseTween?.Kill();
        _micPulseTween = null;

        if (micButtonRect != null)
        {
            DOTween.Kill(micButtonRect);
            micButtonRect.localScale = Vector3.one;
        }
    }

    #endregion

    #region Public API - 포스터 텍스트

    /// <summary>
    /// 포스터 제목 등장 애니메이션
    /// </summary>
    public void ShowPosterTitle()
    {
        if (posterTitleRect == null || posterTitleCanvasGroup == null) return;

        posterTitleRect.gameObject.SetActive(true);
        posterTitleRect.localScale = Vector3.one * 0.8f;
        posterTitleCanvasGroup.alpha = 0f;

        posterTitleRect.DOScale(1f, 0.4f).SetEase(Ease.OutBack);
        posterTitleCanvasGroup.DOFade(1f, 0.4f);
    }

    /// <summary>
    /// 포스터 제목 숨김
    /// </summary>
    public void HidePosterTitle()
    {
        if (posterTitleRect == null || posterTitleCanvasGroup == null) return;

        DOTween.Kill(posterTitleRect);
        DOTween.Kill(posterTitleCanvasGroup);
        posterTitleCanvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 포스터 다짐 등장 애니메이션
    /// </summary>
    public void ShowPosterCommitment()
    {
        if (posterCommitmentRect == null || posterCommitmentCanvasGroup == null) return;

        posterCommitmentRect.gameObject.SetActive(true);

        Vector2 basePos = posterCommitmentRect.anchoredPosition;
        posterCommitmentRect.anchoredPosition = basePos + Vector2.down * 20f;
        posterCommitmentCanvasGroup.alpha = 0f;

        posterCommitmentRect.DOAnchorPos(basePos, 0.4f).SetEase(Ease.OutQuad);
        posterCommitmentCanvasGroup.DOFade(1f, 0.4f);
    }

    /// <summary>
    /// 포스터 다짐 숨김
    /// </summary>
    public void HidePosterCommitment()
    {
        if (posterCommitmentRect == null || posterCommitmentCanvasGroup == null) return;

        DOTween.Kill(posterCommitmentRect);
        DOTween.Kill(posterCommitmentCanvasGroup);
        posterCommitmentCanvasGroup.alpha = 0f;
    }

    #endregion

    #region Public API - 완료 화면

    /// <summary>
    /// 완료 화면 애니메이션 (포스터 완성!)
    /// </summary>
    public void PlayCompleteAnimation(Action onComplete = null)
    {
        StopRecordingAnimation();

        var seq = CreateSequence();

        // 1. 완료 카드 스케일 + 페이드
        if (completeCardRect != null && completeCardCanvasGroup != null)
        {
            completeCardRect.localScale = Vector3.one * 0.8f;
            completeCardCanvasGroup.alpha = 0f;

            seq.Append(completeCardRect
                .DOScale(1f, 0.5f)
                .SetEase(Ease.OutQuad));
            seq.Join(completeCardCanvasGroup.DOFade(1f, 0.5f));
        }

        // 2. Star 아이콘 스프링 등장 (회전 포함)
        if (starIconRect != null)
        {
            starIconRect.localScale = Vector3.zero;
            starIconRect.localRotation = Quaternion.Euler(0, 0, -180);

            seq.Insert(0.3f, starIconRect
                .DOScale(1f, 0.8f)
                .SetEase(Ease.OutBack, 2f));
            seq.Insert(0.3f, starIconRect
                .DORotate(Vector3.zero, 0.8f)
                .SetEase(Ease.OutBack));
        }

        // 3. 최종 포스터 등장 + 글로우 시작 (딜레이 0.5초)
        if (finalPosterRect != null)
        {
            finalPosterRect.localScale = Vector3.one * 0.9f;

            seq.Insert(0.5f, finalPosterRect
                .DOScale(1f, 0.5f)
                .SetEase(Ease.OutQuad));
        }

        // 4. 어시스턴트 피드백 슬라이드 업 (딜레이 0.8초)
        if (feedbackCardRect != null && feedbackCardCanvasGroup != null)
        {
            Vector2 basePos = feedbackCardRect.anchoredPosition;
            feedbackCardRect.anchoredPosition = basePos + Vector2.down * 20f;
            feedbackCardCanvasGroup.alpha = 0f;

            seq.Insert(0.8f, feedbackCardRect
                .DOAnchorPos(basePos, 0.4f)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.8f, feedbackCardCanvasGroup.DOFade(1f, 0.4f));
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
        StopRecordingAnimation();

        // 포스터 프리뷰 리셋
        if (posterPreviewRect != null)
        {
            DOTween.Kill(posterPreviewRect);
            posterPreviewRect.localScale = Vector3.one;
        }
        if (posterPreviewCanvasGroup != null)
        {
            DOTween.Kill(posterPreviewCanvasGroup);
            posterPreviewCanvasGroup.alpha = 0f;
        }

        // 어시스턴트 카드 리셋
        if (assistantCardCanvasGroup != null)
        {
            DOTween.Kill(assistantCardRect);
            DOTween.Kill(assistantCardCanvasGroup);
            assistantCardCanvasGroup.alpha = 0f;
        }

        // 마이크 버튼 리셋
        if (micButtonRect != null)
        {
            DOTween.Kill(micButtonRect);
            micButtonRect.localScale = Vector3.one;
        }

        // 포스터 텍스트 리셋
        HidePosterTitle();
        HidePosterCommitment();

        // 완료 화면 리셋
        if (completeCardCanvasGroup != null)
        {
            DOTween.Kill(completeCardRect);
            DOTween.Kill(completeCardCanvasGroup);
            completeCardCanvasGroup.alpha = 0f;
        }

        if (starIconRect != null)
        {
            DOTween.Kill(starIconRect);
            starIconRect.localScale = Vector3.zero;
            starIconRect.localRotation = Quaternion.Euler(0, 0, -180);
        }

        if (finalPosterRect != null)
        {
            DOTween.Kill(finalPosterRect);
            finalPosterRect.localScale = Vector3.one;
        }

        if (feedbackCardCanvasGroup != null)
        {
            DOTween.Kill(feedbackCardRect);
            DOTween.Kill(feedbackCardCanvasGroup);
            feedbackCardCanvasGroup.alpha = 0f;
        }
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        StopRecordingAnimation();
     
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopRecordingAnimation();
    }
}
