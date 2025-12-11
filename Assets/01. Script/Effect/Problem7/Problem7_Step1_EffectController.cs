using System;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Part 7 - Step 1 인트로 이펙트 컨트롤러
/// - NG 뱃지 흔들림
/// - 캐릭터 위아래 움직임 + 이모지 회전
/// - 경고 아이콘 펄스
/// - 화살표 바운스 (인벤토리 안내)
/// - 메가폰 활성화 (카메라 쉐이크 + 플래시)
/// </summary>
public class Problem7_Step1_EffectController : EffectControllerBase
{
    [Header("===== NG 뱃지 =====")]
    [SerializeField] private RectTransform ngBadgeRect;
    [SerializeField] private float ngWobbleAngle = 5f;
    [SerializeField] private float ngWobbleDuration = 2f;

    [Header("===== 캐릭터 =====")]
    [SerializeField] private RectTransform characterRect;
    [SerializeField] private float characterBobDistance = 5f;
    [SerializeField] private float characterBobDuration = 3f;

    [Header("===== 슬픈 이모지 =====")]
    [SerializeField] private RectTransform sadEmojiRect;
    [SerializeField] private float emojiRotateMin = 15f;
    [SerializeField] private float emojiRotateMax = 20f;
    [SerializeField] private float emojiRotateDuration = 2f;

    [Header("===== 경고 아이콘 =====")]
    [SerializeField] private RectTransform alertIconRect;
    [SerializeField] private CanvasGroup alertIconCanvasGroup;
    [SerializeField] private float alertMinScale = 1f;
    [SerializeField] private float alertMaxScale = 1.2f;
    [SerializeField] private float alertMinAlpha = 0.5f;
    [SerializeField] private float alertMaxAlpha = 1f;
    [SerializeField] private float alertPulseDuration = 1.5f;

    [Header("===== 캡션 텍스트 =====")]
    [SerializeField] private CanvasGroup captionCanvasGroup;
    [SerializeField] private float captionMinAlpha = 0.5f;
    [SerializeField] private float captionMaxAlpha = 0.8f;
    [SerializeField] private float captionPulseDuration = 2f;

    [Header("===== 화살표 (인벤토리 안내) =====")]
    [SerializeField] private RectTransform arrowRect;
    [SerializeField] private float arrowBounceDistance = 10f;
    [SerializeField] private float arrowBounceDuration = 1.5f;

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

    // 루프 트윈들
    private Tween _ngWobbleTween;
    private Tween _characterBobTween;
    private Tween _emojiRotateTween;
    private Tween _alertScaleTween;
    private Tween _alertAlphaTween;
    private Tween _captionTween;
    private Tween _arrowTween;

    #region Public API

    /// <summary>
    /// NG 모니터 애니메이션 시작
    /// </summary>
    public void StartNGMonitorAnimations()
    {
        StopNGMonitorAnimations();

        // NG 뱃지 흔들림: rotate [-5, 5]
        if (ngBadgeRect != null)
        {
            _ngWobbleTween = ngBadgeRect
                .DORotate(new Vector3(0, 0, ngWobbleAngle), ngWobbleDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(new Vector3(0, 0, -ngWobbleAngle));
        }

        // 캐릭터 위아래 움직임
        if (characterRect != null)
        {
            _characterBobTween = characterRect
                .DOAnchorPosY(characterRect.anchoredPosition.y - characterBobDistance, characterBobDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        // 슬픈 이모지 회전
        if (sadEmojiRect != null)
        {
            _emojiRotateTween = sadEmojiRect
                .DORotate(new Vector3(0, 0, emojiRotateMax), emojiRotateDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(new Vector3(0, 0, emojiRotateMin));
        }

        // 경고 아이콘 펄스
        if (alertIconRect != null)
        {
            _alertScaleTween = alertIconRect
                .DOScale(alertMaxScale, alertPulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .From(Vector3.one * alertMinScale);
        }

        if (alertIconCanvasGroup != null)
        {
            alertIconCanvasGroup.alpha = alertMinAlpha;
            _alertAlphaTween = alertIconCanvasGroup
                .DOFade(alertMaxAlpha, alertPulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }

        // 캡션 텍스트 펄스
        if (captionCanvasGroup != null)
        {
            captionCanvasGroup.alpha = captionMinAlpha;
            _captionTween = captionCanvasGroup
                .DOFade(captionMaxAlpha, captionPulseDuration * 0.5f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    /// <summary>
    /// NG 모니터 애니메이션 정지
    /// </summary>
    public void StopNGMonitorAnimations()
    {
        _ngWobbleTween?.Kill();
        _characterBobTween?.Kill();
        _emojiRotateTween?.Kill();
        _alertScaleTween?.Kill();
        _alertAlphaTween?.Kill();
        _captionTween?.Kill();

        _ngWobbleTween = null;
        _characterBobTween = null;
        _emojiRotateTween = null;
        _alertScaleTween = null;
        _alertAlphaTween = null;
        _captionTween = null;
    }

    /// <summary>
    /// 화살표 바운스 시작
    /// </summary>
    public void StartArrowBounce()
    {
        StopArrowBounce();

        if (arrowRect == null) return;

        arrowRect.gameObject.SetActive(true);
        _arrowTween = arrowRect
            .DOAnchorPosY(arrowRect.anchoredPosition.y + arrowBounceDistance, arrowBounceDuration * 0.5f)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo);
    }

    /// <summary>
    /// 화살표 바운스 정지
    /// </summary>
    public void StopArrowBounce()
    {
        _arrowTween?.Kill();
        _arrowTween = null;

        if (arrowRect != null)
            arrowRect.gameObject.SetActive(false);
    }

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
        StopNGMonitorAnimations();
        StopArrowBounce();

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

    protected override void OnDisable()
    {
        base.OnDisable();
        StopNGMonitorAnimations();
        StopArrowBounce();
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        StopNGMonitorAnimations();
        StopArrowBounce();
    }
}
