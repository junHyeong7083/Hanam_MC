using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Problem9_Step1_EffectController - 문제9 스텝1(NG 갈등 장면 해결)의 이펙트 관리자
///
/// 【역할】 인트로(NG 장면 카드+어시스턴트 말풍선 순차 등장), 대기(충돌 아이콘 흔들림+안내 텍스트 펄스),
///          드롭 효과(하이라이트/성공 시 충돌 아이콘 회전+축소), 대본 카드 플립 등장 등 복합 이펙트 관리.
/// 【사용 위치】 ProblemScene - Problem9 Step1 (NG 갈등 장면을 해결하는 스텝)
/// 【트리거】 Logic 클래스에서 PlayIntroAnimation(), PlayDropSuccessEffect(), PlayScriptCardReveal() 등 호출
/// 【의존성】 EffectControllerBase(상속), DOTween, 다수의 RectTransform/CanvasGroup 참조
///
/// 【흐름】
/// 1. PlayIntroAnimation(): NG 장면 슬라이드 업 → 어시스턴트 등장 → 대기 애니메이션 시작
/// 2. 대기: 충돌 아이콘 좌우 흔들림 + 안내 텍스트 알파 펄스
/// 3. 드래그: PlayDropTargetHighlight()/Unhighlight() - 충돌 아이콘 스케일 변화
/// 4. 드롭 성공: PlayDropSuccessEffect() - 충돌 아이콘 스케일업+회전+축소
/// 5. PlayScriptCardReveal(): 대본 카드 스케일+페이드 등장 + 내용 Y축 플립
/// </summary>
public class Problem9_Step1_EffectController : EffectControllerBase
{
    [Header("===== NG 장면 카드 =====")]
    [SerializeField] private RectTransform ngSceneCardRect;
    [SerializeField] private CanvasGroup ngSceneCardCanvasGroup;
    [SerializeField] private float introSlideDistance = 30f;
    [SerializeField] private float introAppearDuration = 0.5f;

    [Header("===== 충돌 아이콘 (💥) =====")]
    [SerializeField] private RectTransform conflictIconRect;
    [SerializeField] private float conflictWobbleAngle = 10f;
    [SerializeField] private float conflictWobbleDuration = 0.5f;

    [Header("===== 어시스턴트 말풍선 =====")]
    [SerializeField] private RectTransform assistantCardRect;
    [SerializeField] private CanvasGroup assistantCardCanvasGroup;
    [SerializeField] private RectTransform speechBubbleRect;
    [SerializeField] private CanvasGroup speechBubbleCanvasGroup;

    [Header("===== 안내 텍스트 =====")]
    [SerializeField] private CanvasGroup instructionTextCanvasGroup;
    [SerializeField] private float instructionMinAlpha = 0.6f;
    [SerializeField] private float instructionMaxAlpha = 1f;
    [SerializeField] private float instructionPulseDuration = 2f;

    [Header("===== 대본 카드 (Complete Root) =====")]
    [SerializeField] private RectTransform scriptCardRect;
    [SerializeField] private CanvasGroup scriptCardCanvasGroup;
    [SerializeField] private RectTransform scriptContentRect;
    [SerializeField] private float flipDuration = 0.6f;

    // 루프 트윈들
    private Tween _conflictWobbleTween;
    private Tween _instructionTween;
    private bool _initialized;

    #region Public API - 인트로

    /// <summary>
    /// 인트로 화면 등장 애니메이션
    /// </summary>
    public void PlayIntroAnimation(Action onComplete = null)
    {
        var seq = CreateSequence();

        // 1. NG 장면 카드 슬라이드 업 + 페이드
        if (ngSceneCardRect != null && ngSceneCardCanvasGroup != null)
        {
            Vector2 basePos = ngSceneCardRect.anchoredPosition;
            ngSceneCardRect.anchoredPosition = basePos + Vector2.down * introSlideDistance;
            ngSceneCardCanvasGroup.alpha = 0f;

            seq.Append(ngSceneCardRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Join(ngSceneCardCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 2. 어시스턴트 카드 슬라이드 업 + 페이드 (딜레이 0.3초)
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

        // 2-1. 말풍선 내부 (딜레이 0.5초)
        if (speechBubbleRect != null && speechBubbleCanvasGroup != null)
        {
            Vector2 basePos = speechBubbleRect.anchoredPosition;
            speechBubbleRect.anchoredPosition = basePos + Vector2.left * 20f;
            speechBubbleCanvasGroup.alpha = 0f;

            seq.Insert(0.5f, speechBubbleRect
                .DOAnchorPos(basePos, introAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.5f, speechBubbleCanvasGroup.DOFade(1f, introAppearDuration));
        }

        // 3. 애니메이션 완료 후 루프 애니메이션 시작
        seq.OnComplete(() =>
        {
            StartIdleAnimations();
            onComplete?.Invoke();
        });
    }

    #endregion

    #region Public API - 대기 애니메이션

    /// <summary>
    /// 대기 애니메이션 시작 (충돌 아이콘 흔들림, 안내 텍스트 펄스)
    /// </summary>
    public void StartIdleAnimations()
    {
        StopIdleAnimations();

        // 충돌 아이콘 흔들림
        if (conflictIconRect != null)
        {
            _conflictWobbleTween = conflictIconRect
                .DORotate(new Vector3(0, 0, conflictWobbleAngle), conflictWobbleDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(new Vector3(0, 0, -conflictWobbleAngle));
        }

        // 안내 텍스트 펄스
        if (instructionTextCanvasGroup != null)
        {
            instructionTextCanvasGroup.alpha = instructionMinAlpha;
            _instructionTween = instructionTextCanvasGroup
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
        _conflictWobbleTween?.Kill();
        _instructionTween?.Kill();

        _conflictWobbleTween = null;
        _instructionTween = null;
    }

    #endregion

    #region Public API - 드롭 효과

    /// <summary>
    /// 드롭 시작 시 (드래그 중 타겟 근처)
    /// </summary>
    public void PlayDropTargetHighlight()
    {
        if (conflictIconRect == null) return;

        // 흔들림 정지하고 스케일 업
        _conflictWobbleTween?.Kill();
        conflictIconRect.localRotation = Quaternion.identity;
        conflictIconRect.DOScale(1.2f, 0.2f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 드롭 취소 시 (타겟에서 벗어남)
    /// </summary>
    public void PlayDropTargetUnhighlight()
    {
        if (conflictIconRect == null) return;

        conflictIconRect.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);

        // 다시 흔들림 시작
        _conflictWobbleTween = conflictIconRect
            .DORotate(new Vector3(0, 0, conflictWobbleAngle), conflictWobbleDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .From(new Vector3(0, 0, -conflictWobbleAngle));
    }

    /// <summary>
    /// 드롭 성공 시 충돌 아이콘 효과
    /// </summary>
    public void PlayDropSuccessEffect(Action onComplete = null)
    {
        StopIdleAnimations();

        if (conflictIconRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = DOTween.Sequence();

        // 충돌 아이콘 스케일 펀치 + 회전
        seq.Append(conflictIconRect
            .DOScale(1.3f, 0.2f)
            .SetEase(Ease.OutQuad));
        seq.Join(conflictIconRect
            .DORotate(new Vector3(0, 0, 360), 0.5f, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad));
        seq.Append(conflictIconRect
            .DOScale(0f, 0.3f)
            .SetEase(Ease.InBack));

        seq.OnComplete(() => onComplete?.Invoke());
    }

    #endregion

    #region Public API - 대본 카드 등장

    /// <summary>
    /// 대본 카드 플립 등장 애니메이션
    /// </summary>
    public void PlayScriptCardReveal(Action onComplete = null)
    {
        if (scriptCardRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        var seq = CreateSequence();

        // 1. 카드 전체 스케일 + 페이드
        scriptCardRect.localScale = Vector3.one * 0.8f;
        if (scriptCardCanvasGroup != null)
            scriptCardCanvasGroup.alpha = 0f;

        seq.Append(scriptCardRect
            .DOScale(1f, 0.5f)
            .SetEase(Ease.OutQuad));
        if (scriptCardCanvasGroup != null)
            seq.Join(scriptCardCanvasGroup.DOFade(1f, 0.5f));

        // 2. 대본 내용 플립 (rotateY 90 → 0)
        if (scriptContentRect != null)
        {
            scriptContentRect.localRotation = Quaternion.Euler(0, 90, 0);
            seq.Insert(0.2f, scriptContentRect
                .DORotate(Vector3.zero, flipDuration)
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
        StopIdleAnimations();

        // NG 장면 카드 리셋
        if (ngSceneCardCanvasGroup != null)
        {
            DOTween.Kill(ngSceneCardRect);
            DOTween.Kill(ngSceneCardCanvasGroup);
            ngSceneCardCanvasGroup.alpha = 0f;
        }

        // 충돌 아이콘 리셋
        if (conflictIconRect != null)
        {
            DOTween.Kill(conflictIconRect);
            conflictIconRect.localScale = Vector3.one;
            conflictIconRect.localRotation = Quaternion.identity;
        }

        // 어시스턴트 카드 리셋
        if (assistantCardCanvasGroup != null)
        {
            DOTween.Kill(assistantCardRect);
            DOTween.Kill(assistantCardCanvasGroup);
            assistantCardCanvasGroup.alpha = 0f;
        }

        if (speechBubbleCanvasGroup != null)
        {
            DOTween.Kill(speechBubbleRect);
            DOTween.Kill(speechBubbleCanvasGroup);
            speechBubbleCanvasGroup.alpha = 0f;
        }

        // 안내 텍스트 리셋
        if (instructionTextCanvasGroup != null)
        {
            DOTween.Kill(instructionTextCanvasGroup);
            instructionTextCanvasGroup.alpha = instructionMinAlpha;
        }

        // 대본 카드 리셋
        if (scriptCardCanvasGroup != null)
        {
            DOTween.Kill(scriptCardRect);
            DOTween.Kill(scriptCardCanvasGroup);
            scriptCardCanvasGroup.alpha = 0f;
        }

        if (scriptContentRect != null)
        {
            DOTween.Kill(scriptContentRect);
            scriptContentRect.localRotation = Quaternion.Euler(0, 90, 0);
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
