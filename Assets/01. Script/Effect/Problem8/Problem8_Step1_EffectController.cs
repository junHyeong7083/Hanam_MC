using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 8 - Step 1 스토리보드 인트로 이펙트 컨트롤러
/// - 어시스턴트 말풍선 슬라이드 업 + 페이드
/// - 스토리보드 테이블 등장
/// - 프롬프트 텍스트 펄스
/// - 스토리보드 플로팅 + NEW 뱃지
/// - 스토리보드 클릭 시 플립 애니메이션
/// </summary>
public class Problem8_Step1_EffectController : EffectControllerBase
{
    [Header("===== 어시스턴트 말풍선 =====")]
    [SerializeField] private RectTransform speechCardRect;
    [SerializeField] private CanvasGroup speechCardCanvasGroup;
    [SerializeField] private RectTransform speechBubbleRect;
    [SerializeField] private CanvasGroup speechBubbleCanvasGroup;
    [SerializeField] private float speechSlideDistance = 30f;
    [SerializeField] private float speechAppearDuration = 0.5f;

    [Header("===== 스토리보드 테이블 =====")]
    [SerializeField] private RectTransform tableCardRect;
    [SerializeField] private CanvasGroup tableCardCanvasGroup;
    [SerializeField] private float tableSlideDistance = 40f;
    [SerializeField] private float tableAppearDelay = 0.6f;

    [Header("===== 프롬프트 텍스트 =====")]
    [SerializeField] private CanvasGroup promptTextCanvasGroup;
    [SerializeField] private float promptMinAlpha = 0.6f;
    [SerializeField] private float promptMaxAlpha = 1f;
    [SerializeField] private float promptPulseDuration = 2f;

    [Header("===== 스토리보드 플로팅 =====")]
    [SerializeField] private RectTransform storyboardRect;
    [SerializeField] private float floatDistance = 10f;
    [SerializeField] private float floatDuration = 2f;

    [Header("===== NEW 뱃지 =====")]
    [SerializeField] private RectTransform newBadgeRect;
    [SerializeField] private float badgeMinScale = 1f;
    [SerializeField] private float badgeMaxScale = 1.1f;
    [SerializeField] private float badgePulseDuration = 1.5f;

    [Header("===== NEW 뱃지 글로우 =====")]
    [SerializeField] private RectTransform badgeGlowRect;
    [SerializeField] private CanvasGroup badgeGlowCanvasGroup;
    [SerializeField] private float glowMinScale = 1f;
    [SerializeField] private float glowMaxScale = 1.5f;
    [SerializeField] private float glowMinAlpha = 0.3f;
    [SerializeField] private float glowMaxAlpha = 0.6f;

    [Header("===== 스토리보드 클릭 애니메이션 =====")]
    [SerializeField] private float flipDuration = 1.5f;
    [SerializeField] private float flipScaleMax = 1.2f;

    [Header("===== 확인 메시지 =====")]
    [SerializeField] private RectTransform revealedMessageRect;
    [SerializeField] private CanvasGroup revealedMessageCanvasGroup;

    // 루프 트윈들
    private Tween _promptPulseTween;
    private Tween _floatTween;
    private Tween _badgeScaleTween;
    private Tween _glowScaleTween;
    private Tween _glowAlphaTween;
    private Vector2 _storyboardBasePos;
    private bool _initialized;

    private void Awake()
    {
        SaveInitialState();
    }

    private void SaveInitialState()
    {
        if (_initialized) return;

        if (storyboardRect != null)
            _storyboardBasePos = storyboardRect.anchoredPosition;

        _initialized = true;
    }

    #region Public API - 인트로 등장

    /// <summary>
    /// 인트로 화면 등장 애니메이션
    /// </summary>
    public void PlayIntroAnimation(Action onComplete = null)
    {
        SaveInitialState();

        var seq = CreateSequence();

        // 1. 어시스턴트 말풍선 슬라이드 업 + 페이드
        if (speechCardRect != null && speechCardCanvasGroup != null)
        {
            Vector2 basePos = speechCardRect.anchoredPosition;
            speechCardRect.anchoredPosition = basePos + Vector2.down * speechSlideDistance;
            speechCardCanvasGroup.alpha = 0f;

            seq.Append(speechCardRect
                .DOAnchorPos(basePos, speechAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Join(speechCardCanvasGroup.DOFade(1f, speechAppearDuration));
        }

        // 1-1. 말풍선 내부 텍스트 (딜레이 0.3초)
        if (speechBubbleRect != null && speechBubbleCanvasGroup != null)
        {
            Vector2 basePos = speechBubbleRect.anchoredPosition;
            speechBubbleRect.anchoredPosition = basePos + Vector2.left * 20f;
            speechBubbleCanvasGroup.alpha = 0f;

            seq.Insert(0.3f, speechBubbleRect
                .DOAnchorPos(basePos, speechAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(0.3f, speechBubbleCanvasGroup.DOFade(1f, speechAppearDuration));
        }

        // 2. 스토리보드 테이블 슬라이드 업 + 페이드 (딜레이 0.6초)
        if (tableCardRect != null && tableCardCanvasGroup != null)
        {
            Vector2 basePos = tableCardRect.anchoredPosition;
            tableCardRect.anchoredPosition = basePos + Vector2.down * tableSlideDistance;
            tableCardCanvasGroup.alpha = 0f;

            seq.Insert(tableAppearDelay, tableCardRect
                .DOAnchorPos(basePos, speechAppearDuration)
                .SetEase(Ease.OutQuad));
            seq.Insert(tableAppearDelay, tableCardCanvasGroup.DOFade(1f, speechAppearDuration));
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
    /// 대기 애니메이션 시작 (프롬프트 펄스, 스토리보드 플로팅, NEW 뱃지)
    /// </summary>
    public void StartIdleAnimations()
    {
        StopIdleAnimations();

        // 프롬프트 텍스트 펄스: opacity [0.6, 1]
        if (promptTextCanvasGroup != null)
        {
            promptTextCanvasGroup.alpha = promptMinAlpha;
            _promptPulseTween = promptTextCanvasGroup
                .DOFade(promptMaxAlpha, promptPulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        // 스토리보드 플로팅: y [0, -10, 0]
        if (storyboardRect != null)
        {
            _floatTween = storyboardRect
                .DOAnchorPosY(_storyboardBasePos.y + floatDistance, floatDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        // NEW 뱃지 스케일 펄스: [1, 1.1]
        if (newBadgeRect != null)
        {
            newBadgeRect.gameObject.SetActive(true);
            _badgeScaleTween = newBadgeRect
                .DOScale(badgeMaxScale, badgePulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(Vector3.one * badgeMinScale);
        }

        // NEW 뱃지 글로우: scale [1, 1.5], alpha [0.6, 0.3]
        if (badgeGlowRect != null)
        {
            badgeGlowRect.gameObject.SetActive(true);
            _glowScaleTween = badgeGlowRect
                .DOScale(glowMaxScale, promptPulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(Vector3.one * glowMinScale);
        }

        if (badgeGlowCanvasGroup != null)
        {
            badgeGlowCanvasGroup.alpha = glowMaxAlpha;
            _glowAlphaTween = badgeGlowCanvasGroup
                .DOFade(glowMinAlpha, promptPulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    /// <summary>
    /// 대기 애니메이션 정지
    /// </summary>
    public void StopIdleAnimations()
    {
        _promptPulseTween?.Kill();
        _floatTween?.Kill();
        _badgeScaleTween?.Kill();
        _glowScaleTween?.Kill();
        _glowAlphaTween?.Kill();

        _promptPulseTween = null;
        _floatTween = null;
        _badgeScaleTween = null;
        _glowScaleTween = null;
        _glowAlphaTween = null;
    }

    #endregion

    #region Public API - 스토리보드 클릭

    /// <summary>
    /// 스토리보드 호버
    /// </summary>
    public void PlayStoryboardHover()
    {
        if (storyboardRect == null) return;
        storyboardRect.DOScale(1.02f, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 스토리보드 호버 해제
    /// </summary>
    public void PlayStoryboardUnhover()
    {
        if (storyboardRect == null) return;
        storyboardRect.DOScale(1f, 0.1f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// 스토리보드 클릭 (플립 애니메이션)
    /// </summary>
    public void PlayStoryboardFlip(Action onComplete = null)
    {
        if (storyboardRect == null)
        {
            onComplete?.Invoke();
            return;
        }

        // 대기 애니메이션 정지
        StopIdleAnimations();

        // NEW 뱃지 숨김
        if (newBadgeRect != null)
            newBadgeRect.gameObject.SetActive(false);
        if (badgeGlowRect != null)
            badgeGlowRect.gameObject.SetActive(false);

        var seq = DOTween.Sequence();

        // 스케일 펀치: 1 → 1.2 → 1
        seq.Append(storyboardRect
            .DOScale(flipScaleMax, flipDuration * 0.3f)
            .SetEase(Ease.OutQuad));
        seq.Append(storyboardRect
            .DOScale(1f, flipDuration * 0.7f)
            .SetEase(Ease.OutQuad));

        // Y축 회전 (3D 플립 효과 - UI에서는 X 스케일로 시뮬레이션)
        seq.Insert(0f, storyboardRect
            .DORotate(new Vector3(0, 360, 0), flipDuration, RotateMode.FastBeyond360)
            .SetEase(Ease.OutQuad));

        seq.OnComplete(() =>
        {
            ShowRevealedMessage();
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// 확인 메시지 표시
    /// </summary>
    private void ShowRevealedMessage()
    {
        if (revealedMessageRect == null || revealedMessageCanvasGroup == null) return;

        revealedMessageRect.gameObject.SetActive(true);
        revealedMessageRect.localScale = Vector3.one * 0.8f;
        revealedMessageCanvasGroup.alpha = 0f;

        var seq = DOTween.Sequence();
        seq.Append(revealedMessageRect
            .DOScale(1f, 0.3f)
            .SetEase(Ease.OutBack));
        seq.Join(revealedMessageCanvasGroup.DOFade(1f, 0.3f));
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
        SaveInitialState();

        // 말풍선 리셋
        if (speechCardCanvasGroup != null)
        {
            DOTween.Kill(speechCardRect);
            DOTween.Kill(speechCardCanvasGroup);
            speechCardCanvasGroup.alpha = 0f;
        }

        if (speechBubbleCanvasGroup != null)
        {
            DOTween.Kill(speechBubbleRect);
            DOTween.Kill(speechBubbleCanvasGroup);
            speechBubbleCanvasGroup.alpha = 0f;
        }

        // 테이블 리셋
        if (tableCardCanvasGroup != null)
        {
            DOTween.Kill(tableCardRect);
            DOTween.Kill(tableCardCanvasGroup);
            tableCardCanvasGroup.alpha = 0f;
        }

        // 프롬프트 리셋
        if (promptTextCanvasGroup != null)
        {
            DOTween.Kill(promptTextCanvasGroup);
            promptTextCanvasGroup.alpha = promptMinAlpha;
        }

        // 스토리보드 리셋
        if (storyboardRect != null)
        {
            DOTween.Kill(storyboardRect);
            storyboardRect.anchoredPosition = _storyboardBasePos;
            storyboardRect.localScale = Vector3.one;
            storyboardRect.localRotation = Quaternion.identity;
        }

        // NEW 뱃지 리셋
        if (newBadgeRect != null)
        {
            DOTween.Kill(newBadgeRect);
            newBadgeRect.localScale = Vector3.one;
            newBadgeRect.gameObject.SetActive(true);
        }

        if (badgeGlowRect != null)
        {
            DOTween.Kill(badgeGlowRect);
            badgeGlowRect.gameObject.SetActive(true);
        }

        if (badgeGlowCanvasGroup != null)
        {
            DOTween.Kill(badgeGlowCanvasGroup);
            badgeGlowCanvasGroup.alpha = glowMaxAlpha;
        }

        // 확인 메시지 리셋
        if (revealedMessageRect != null)
        {
            DOTween.Kill(revealedMessageRect);
            DOTween.Kill(revealedMessageCanvasGroup);
            revealedMessageRect.gameObject.SetActive(false);
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
