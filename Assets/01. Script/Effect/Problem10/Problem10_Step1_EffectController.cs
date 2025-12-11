using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 10 - Step 1 인트로 이펙트 컨트롤러
/// - 빈 포스터 프레임 등장
/// - 포스터 주변 반짝임 효과
/// - 안내 텍스트 펄스
/// - 완료 화면 (Award 아이콘 스프링 등장)
/// </summary>
public class Problem10_Step1_EffectController : EffectControllerBase
{
    [Header("===== 빈 포스터 프레임 =====")]
    [SerializeField] private float introSlideDistance = 30f;
    [SerializeField] private float introAppearDuration = 0.5f;
    [SerializeField] private RectTransform posterFrameRect;
    [SerializeField] private CanvasGroup posterFrameCanvasGroup;

    [Header("===== 포스터 주변 반짝임 =====")]
    [SerializeField] private RectTransform[] sparkleRects;
    [SerializeField] private float sparkleCycleDuration = 2f;

    [Header("===== 어시스턴트 말풍선 =====")]
    [SerializeField] private RectTransform assistantCardRect;
    [SerializeField] private CanvasGroup assistantCardCanvasGroup;

    [Header("===== 안내 텍스트 =====")]
    [SerializeField] private CanvasGroup instructionTextCanvasGroup;
    [SerializeField] private float instructionMinAlpha = 0.6f;
    [SerializeField] private float instructionMaxAlpha = 1f;
    [SerializeField] private float instructionPulseDuration = 2f;

    [Header("===== 완료 화면 =====")]
    [SerializeField] private RectTransform completeCardRect;
    [SerializeField] private CanvasGroup completeCardCanvasGroup;

    [Header("===== 완료 - Award 아이콘 =====")]
    [SerializeField] private RectTransform awardIconRect;

    [Header("===== 완료 - 어시스턴트 응답 =====")]
    [SerializeField] private RectTransform completeAssistantRect;
    [SerializeField] private CanvasGroup completeAssistantCanvasGroup;

    // 루프 트윈들
    private Tween _instructionPulseTween;
    private Tween[] _sparkleTweens;

    #region Public API - 인트로 화면

    /// <summary>
    /// 인트로 화면 등장 애니메이션
    /// </summary>
    public void PlayIntroAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 1. 빈 포스터 프레임 스케일 등장
        if (posterFrameRect != null && posterFrameCanvasGroup != null)
        {
            posterFrameRect.localScale = Vector3.one * 0.9f;
            posterFrameCanvasGroup.alpha = 0f;

            seq.Append(posterFrameRect
                .DOScale(1f, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Join(posterFrameCanvasGroup.DOFade(1f, introAppearDuration));
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

        // 3. 완료 후 루프 애니메이션 시작
        seq.OnComplete(() =>
        {
            StartIdleAnimations();
            onComplete?.Invoke();
        });
    }

    #endregion

    #region Public API - 대기 애니메이션

    /// <summary>
    /// 대기 애니메이션 시작 (반짝임 + 안내 텍스트 펄스)
    /// </summary>
    public void StartIdleAnimations()
    {
        StopIdleAnimations();

        // 포스터 주변 반짝임
        StartSparkleAnimations();

        // 안내 텍스트 펄스
        if (instructionTextCanvasGroup != null)
        {
            instructionTextCanvasGroup.alpha = instructionMinAlpha;
            _instructionPulseTween = instructionTextCanvasGroup
                .DOFade(instructionMaxAlpha, instructionPulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    /// <summary>
    /// 대기 애니메이션 정지
    /// </summary>
    public void StopIdleAnimations()
    {
        StopSparkleAnimations();

        _instructionPulseTween?.Kill();
        _instructionPulseTween = null;
    }

    private void StartSparkleAnimations()
    {
        if (sparkleRects == null || sparkleRects.Length == 0) return;

        _sparkleTweens = new Tween[sparkleRects.Length];

        for (int i = 0; i < sparkleRects.Length; i++)
        {
            if (sparkleRects[i] == null) continue;

            float delay = i * (sparkleCycleDuration / sparkleRects.Length);
            int index = i;

            sparkleRects[i].localScale = Vector3.zero;
            sparkleRects[i].localRotation = Quaternion.identity;

            _sparkleTweens[i] = DOTween.Sequence()
                .AppendInterval(delay)
                .Append(sparkleRects[index]
                    .DOScale(1.5f, sparkleCycleDuration * 0.5f)
                    .SetEase(Ease.OutQuad))
                .Join(sparkleRects[index]
                    .DORotate(new Vector3(0, 0, 180), sparkleCycleDuration * 0.5f, RotateMode.FastBeyond360))
                .Append(sparkleRects[index]
                    .DOScale(0f, sparkleCycleDuration * 0.5f)
                    .SetEase(Ease.InQuad))
                .Join(sparkleRects[index]
                    .DORotate(new Vector3(0, 0, 360), sparkleCycleDuration * 0.5f, RotateMode.FastBeyond360))
                .SetLoops(-1);
        }
    }

    private void StopSparkleAnimations()
    {
        if (_sparkleTweens == null) return;

        foreach (var tween in _sparkleTweens)
        {
            tween?.Kill();
        }
        _sparkleTweens = null;

        // 반짝임 리셋
        if (sparkleRects != null)
        {
            foreach (var rect in sparkleRects)
            {
                if (rect != null)
                {
                    DOTween.Kill(rect);
                    rect.localScale = Vector3.zero;
                    rect.localRotation = Quaternion.identity;
                }
            }
        }
    }

    #endregion

    #region Public API - 드롭 효과

    /// <summary>
    /// 드롭 타겟 하이라이트
    /// </summary>
    public void PlayDropTargetHighlight()
    {
        if (posterFrameRect == null) return;

        posterFrameRect.DOScale(1.1f, 0.2f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 드롭 타겟 언하이라이트
    /// </summary>
    public void PlayDropTargetUnhighlight()
    {
        if (posterFrameRect == null) return;

        posterFrameRect.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 드롭 성공 효과
    /// </summary>
    public void PlayDropSuccessEffect(Action onComplete = null)
    {
        StopIdleAnimations();

        if (posterFrameRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();

        // 포스터 펀치 스케일
        seq.Append(posterFrameRect
            .DOScale(1.2f, 0.2f)
            .SetEase(Ease.OutQuad));
        seq.Append(posterFrameRect
            .DOScale(1f, 0.15f)
            .SetEase(Ease.InQuad));

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - 완료 화면

    /// <summary>
    /// 완료 화면 애니메이션 (포스터 발견!)
    /// </summary>
    public void PlayCompleteAnimation(Action onComplete = null)
    {
        StopIdleAnimations();

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

        // 2. Award 아이콘 스프링 등장 (회전 포함)
        if (awardIconRect != null)
        {
            awardIconRect.localScale = Vector3.zero;
            awardIconRect.localRotation = Quaternion.Euler(0, 0, -180);

            seq.Insert(0.3f, awardIconRect
                .DOScale(1f, 0.8f)
                .SetEase(Ease.OutBack, 2f));
            seq.Insert(0.3f, awardIconRect
                .DORotate(Vector3.zero, 0.8f)
                .SetEase(Ease.OutBack));
        }

        // 3. 어시스턴트 응답 슬라이드 업 (딜레이 0.5초)
        if (completeAssistantRect != null && completeAssistantCanvasGroup != null)
        {
            Vector2 basePos = completeAssistantRect.anchoredPosition;
            completeAssistantRect.anchoredPosition = basePos + Vector2.down * 20f;
            completeAssistantCanvasGroup.alpha = 0f;

            seq.Insert(0.5f, completeAssistantRect
                .DOAnchorPos(basePos, 0.4f)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.5f, completeAssistantCanvasGroup.DOFade(1f, 0.4f));
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
        StopIdleAnimations();

        // 포스터 프레임 리셋
        if (posterFrameRect != null)
        {
            DOTween.Kill(posterFrameRect);
            posterFrameRect.localScale = Vector3.one;
        }
        if (posterFrameCanvasGroup != null)
        {
            DOTween.Kill(posterFrameCanvasGroup);
            posterFrameCanvasGroup.alpha = 0f;
        }

        // 어시스턴트 카드 리셋
        if (assistantCardCanvasGroup != null)
        {
            DOTween.Kill(assistantCardRect);
            DOTween.Kill(assistantCardCanvasGroup);
            assistantCardCanvasGroup.alpha = 0f;
        }

        // 안내 텍스트 리셋
        if (instructionTextCanvasGroup != null)
        {
            DOTween.Kill(instructionTextCanvasGroup);
            instructionTextCanvasGroup.alpha = instructionMinAlpha;
        }

        // 완료 화면 리셋
        if (completeCardCanvasGroup != null)
        {
            DOTween.Kill(completeCardRect);
            DOTween.Kill(completeCardCanvasGroup);
            completeCardCanvasGroup.alpha = 0f;
        }

        if (awardIconRect != null)
        {
            DOTween.Kill(awardIconRect);
            awardIconRect.localScale = Vector3.zero;
            awardIconRect.localRotation = Quaternion.Euler(0, 0, -180);
        }

        if (completeAssistantCanvasGroup != null)
        {
            DOTween.Kill(completeAssistantRect);
            DOTween.Kill(completeAssistantCanvasGroup);
            completeAssistantCanvasGroup.alpha = 0f;
        }
    }

    #endregion

    protected override void OnDisable()
    {
        base.OnDisable();
        StopIdleAnimations();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopIdleAnimations();
    }
}
