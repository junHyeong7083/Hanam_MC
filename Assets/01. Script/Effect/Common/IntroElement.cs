using System;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// IntroElement - 개별 오브젝트에 부착하여 사용하는 인트로 등장 애니메이션 컴포넌트
///
/// 【역할】 슬라이드/페이드/스케일/날라오기(FlyIn-Catmull-Rom 곡선) 등 다양한 등장 연출 제공.
///          EffectControllerBase의 IntroElement 시스템과 유사하지만, 개별 오브젝트에 독립적으로 부착 가능.
///          Catmull-Rom 스플라인으로 경유점을 지나는 곡선 경로 이동도 지원.
/// 【사용 위치】 UI 요소의 개별 등장 연출이 필요한 곳 (스텝 진입 시 순차 등장 등)
/// 【트리거】 playOnEnable=true 시 OnEnable에서 자동 재생, 또는 외부에서 Play() 호출
/// 【의존성】 DOTween(DG.Tweening), RectTransform, CanvasGroup(페이드 사용 시 자동 추가)
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

    [Header("===== 날라오기 (경유점 곡선) =====")]
    [SerializeField] private bool enableFlyIn = false;
    [Tooltip("시작 위치 오프셋 (현재 위치 기준 상대값, 픽셀)")]
    [SerializeField] private Vector2 flyStartOffset = new Vector2(-400f, -200f);
    [Tooltip("경유 꼭짓점들 (현재 위치 기준 상대값, 픽셀) — 커브가 이 점들을 통과합니다")]
    [SerializeField] private Vector2[] flyWaypoints = new Vector2[] { new Vector2(-100f, 150f) };

    /// <summary>도착 시 호출되는 이벤트 (ShakeTrigger 등에서 구독)</summary>
    public event Action OnArrived;

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
        if (enableFlyIn)
            _rectTransform.anchoredPosition = _basePosition + flyStartOffset;
        else if (slideFrom != SlideDirection.None)
            _rectTransform.anchoredPosition = _basePosition + GetSlideOffset();

        if (enableFade && _canvasGroup != null)
            _canvasGroup.alpha = 0f;

        if (enableScale)
            _rectTransform.localScale = Vector3.one * startScale;

        // 날라오기 (Catmull-Rom 곡선 — 경유점 통과)
        if (enableFlyIn)
        {
            // 경로 구축: start → waypoints → end
            Vector2 start = _basePosition + flyStartOffset;
            Vector2 end   = _basePosition;

            // Catmull-Rom 보간에 필요한 점 배열 (양 끝 가상점 포함)
            int wpCount  = flyWaypoints != null ? flyWaypoints.Length : 0;
            int pCount   = 2 + wpCount;                     // start + waypoints + end
            Vector2[] pts = new Vector2[pCount];
            pts[0] = start;
            for (int i = 0; i < wpCount; i++)
                pts[1 + i] = _basePosition + flyWaypoints[i];
            pts[pCount - 1] = end;

            _slideTween = DOTween.To(
                () => 0f,
                t => _rectTransform.anchoredPosition = EvalCatmullRom(pts, t),
                1f, duration
            ).SetEase(easeType).SetDelay(delay);
        }
        // 슬라이드
        else if (slideFrom != SlideDirection.None)
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

        // onComplete + OnArrived 이벤트 래핑
        Action completionCallback = () =>
        {
            onComplete?.Invoke();
            OnArrived?.Invoke();
        };

        // 스케일
        if (enableScale)
        {
            _scaleTween = _rectTransform
                .DOScale(1f, duration)
                .SetEase(easeType)
                .SetDelay(delay)
                .OnComplete(() => completionCallback());
        }
        else if (enableFlyIn && _slideTween != null)
        {
            _slideTween.OnComplete(() => completionCallback());
        }
        else if (slideFrom != SlideDirection.None && _slideTween != null)
        {
            _slideTween.OnComplete(() => completionCallback());
        }
        else if (enableFade && _canvasGroup != null)
        {
            _fadeTween.OnComplete(() => completionCallback());
        }
        else
        {
            completionCallback();
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
            if (enableFlyIn)
                _rectTransform.anchoredPosition = _basePosition + flyStartOffset;
            else if (slideFrom != SlideDirection.None)
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

    #region Catmull-Rom

    /// <summary>
    /// Catmull-Rom 스플라인 보간 (pts 배열의 모든 점을 통과)
    /// </summary>
    private static Vector2 EvalCatmullRom(Vector2[] pts, float t)
    {
        int segCount = pts.Length - 1;
        if (segCount <= 0) return pts[0];

        float scaled = t * segCount;
        int seg = Mathf.Min((int)scaled, segCount - 1);
        float lt = scaled - seg;

        // Catmull-Rom에는 4점 필요 (p0, p1, p2, p3)
        Vector2 p0 = pts[Mathf.Max(seg - 1, 0)];
        Vector2 p1 = pts[seg];
        Vector2 p2 = pts[Mathf.Min(seg + 1, pts.Length - 1)];
        Vector2 p3 = pts[Mathf.Min(seg + 2, pts.Length - 1)];

        // Catmull-Rom 공식
        float lt2 = lt * lt;
        float lt3 = lt2 * lt;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * lt +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * lt2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * lt3
        );
    }

    #endregion
}
