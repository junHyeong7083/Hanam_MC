using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 10 - Step 3 포스터 완성 이펙트 컨트롤러
/// - 완료 화면 (완료 카드 + 최종 포스터)
/// - 인트로 연출은 IntroElements에서 처리
/// - 마이크 연출은 MicRecordingIndicator 프리팹에서 처리
/// </summary>
public class Problem10_Step3_EffectController : EffectControllerBase
{
    [Header("===== 완료 화면 =====")]
    [SerializeField] private RectTransform completeCardRect;
    [SerializeField] private CanvasGroup completeCardCanvasGroup;

    [Header("===== 완료 - 최종 포스터 =====")]
    [SerializeField] private RectTransform finalPosterRect;

    #region Public API - 완료 화면

    /// <summary>
    /// 완료 화면 애니메이션 (포스터 완성!)
    /// </summary>
    public void PlayCompleteAnimation(Action onComplete = null)
    {
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

        // 2. 최종 포스터 등장 (딜레이 0.3초)
        if (finalPosterRect != null)
        {
            finalPosterRect.localScale = Vector3.one * 0.9f;

            seq.Insert(0.3f, finalPosterRect
                .DOScale(1f, 0.5f)
                .SetEase(Ease.OutQuad));
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

        // 완료 화면 리셋
        if (completeCardCanvasGroup != null)
        {
            DOTween.Kill(completeCardRect);
            DOTween.Kill(completeCardCanvasGroup);
            completeCardCanvasGroup.alpha = 0f;
        }

        if (finalPosterRect != null)
        {
            DOTween.Kill(finalPosterRect);
            finalPosterRect.localScale = Vector3.one;
        }
    }

    #endregion
}
