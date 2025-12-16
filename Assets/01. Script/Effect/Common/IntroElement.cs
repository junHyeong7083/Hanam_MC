using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 인트로 요소 애니메이션
/// - EffectController 인트로 연출처럼 슬라이드 + 페이드
/// - 개별 오브젝트에 붙여서 사용
/// - 외부에서 Play() 호출하거나 playOnEnable로 자동 재생
/// </summary>
public class IntroElement : MonoBehaviour
{
    public enum SlideDirection { None, Up, Down, Left, Right }

    [Header("===== 재생 설정 =====")]
    [SerializeField] private bool playOnEnable = true;
    [SerializeField] private float delay = 0f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private Ease easeType = Ease.OutQuad;

    [Header("===== 슬라이드 =====")]
    [SerializeField] private SlideDirection slideFrom = SlideDirection.Down;
    [SerializeField] private float slideDistance = 30f;

    [Header("===== 페이드 =====")]
    [SerializeField] private bool enableFade = true;

    [Header("===== 스케일 =====")]
    [SerializeField] private bool enableScale = false;
    [SerializeField] private float startScale = 0.9f;

    // 내부
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _basePosition;
    private bool _initialized;
    private Tween _slideTween;
    private Tween _fadeTween;
    private Tween _scaleTween;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();

        if (enableFade)
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    private void OnEnable()
    {
        SaveBasePosition();

        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        KillTweens();
    }

    private void OnDestroy()
    {
        KillTweens();
    }

    private void SaveBasePosition()
    {
        if (_initialized) return;
        if (_rectTransform != null)
            _basePosition = _rectTransform.anchoredPosition;
        _initialized = true;
    }

    private void KillTweens()
    {
        _slideTween?.Kill();
        _fadeTween?.Kill();
        _scaleTween?.Kill();
        _slideTween = null;
        _fadeTween = null;
        _scaleTween = null;
    }

    private Vector2 GetSlideOffset()
    {
        switch (slideFrom)
        {
            case SlideDirection.Up: return Vector2.up * slideDistance;
            case SlideDirection.Down: return Vector2.down * slideDistance;
            case SlideDirection.Left: return Vector2.left * slideDistance;
            case SlideDirection.Right: return Vector2.right * slideDistance;
            default: return Vector2.zero;
        }
    }

    #region Public API

    /// <summary>
    /// 애니메이션 재생
    /// </summary>
    public void Play(Action onComplete = null)
    {
        KillTweens();
        SaveBasePosition();

        if (_rectTransform == null) return;

        // 시작 상태 설정
        if (slideFrom != SlideDirection.None)
            _rectTransform.anchoredPosition = _basePosition + GetSlideOffset();

        if (enableFade && _canvasGroup != null)
            _canvasGroup.alpha = 0f;

        if (enableScale)
            _rectTransform.localScale = Vector3.one * startScale;

        // 슬라이드
        if (slideFrom != SlideDirection.None)
        {
            _slideTween = _rectTransform
                .DOAnchorPos(_basePosition, duration)
                .SetEase(easeType)
                .SetDelay(delay);
        }

        // 페이드
        if (enableFade && _canvasGroup != null)
        {
            _fadeTween = _canvasGroup
                .DOFade(1f, duration)
                .SetEase(easeType)
                .SetDelay(delay);
        }

        // 스케일
        if (enableScale)
        {
            _scaleTween = _rectTransform
                .DOScale(1f, duration)
                .SetEase(easeType)
                .SetDelay(delay)
                .OnComplete(() => onComplete?.Invoke());
        }
        else if (slideFrom != SlideDirection.None)
        {
            _slideTween.OnComplete(() => onComplete?.Invoke());
        }
        else if (enableFade && _canvasGroup != null)
        {
            _fadeTween.OnComplete(() => onComplete?.Invoke());
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 즉시 최종 상태로 설정
    /// </summary>
    public void SetToEnd()
    {
        KillTweens();
        SaveBasePosition();

        if (_rectTransform != null)
        {
            _rectTransform.anchoredPosition = _basePosition;
            _rectTransform.localScale = Vector3.one;
        }

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

    /// <summary>
    /// 시작 상태로 리셋
    /// </summary>
    public void ResetToStart()
    {
        KillTweens();
        SaveBasePosition();

        if (_rectTransform != null)
        {
            if (slideFrom != SlideDirection.None)
                _rectTransform.anchoredPosition = _basePosition + GetSlideOffset();

            if (enableScale)
                _rectTransform.localScale = Vector3.one * startScale;
        }

        if (enableFade && _canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 딜레이 설정 (순차 등장용)
    /// </summary>
    public void SetDelay(float newDelay)
    {
        delay = newDelay;
    }

    #endregion
}
