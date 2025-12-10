using UnityEngine;
using DG.Tweening;

/// <summary>
/// 아래에서 위로 슬라이드하며 페이드인하는 애니메이션
/// - CTA 버튼, 완료 패널 등장에 사용
///
/// [사용처]
/// - Problem2 Step1: completeRoot 내부 CTA 버튼
/// - 모든 Step의 완료 버튼 등장
/// </summary>
public class SlideUpFadeIn : MonoBehaviour
{
    [Header("===== 애니메이션 설정 =====")]
    [SerializeField] private float startOffsetY = 50f;
    [SerializeField] private float duration = 0.5f;
    [SerializeField] private float delay = 0f;
    [SerializeField] private bool playOnEnable = true;

    [Header("Easing")]
    [SerializeField] private Ease easeType = Ease.OutQuad;

    // 내부 상태
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _basePosition;
    private bool _initialized;
    private Sequence _sequence;

    private void Awake()
    {
        _rectTransform = transform as RectTransform;
        if (_rectTransform == null)
            _rectTransform = GetComponent<RectTransform>();

        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        // 첫 Enable 시 기본 위치 저장
        if (!_initialized && _rectTransform != null)
        {
            _basePosition = _rectTransform.anchoredPosition;
            _initialized = true;
        }

        if (playOnEnable)
            Play();
    }

    private void OnDisable()
    {
        KillSequence();
    }

    private void OnDestroy()
    {
        KillSequence();
    }

    private void KillSequence()
    {
        _sequence?.Kill();
        _sequence = null;
    }

    #region Public API

    public void Play()
    {
        KillSequence();

        if (_rectTransform == null) return;

        // 시작 상태
        Vector2 startPos = _basePosition + new Vector2(0, -startOffsetY);
        _rectTransform.anchoredPosition = startPos;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;

        _sequence = DOTween.Sequence();

        // 딜레이
        if (delay > 0f)
            _sequence.AppendInterval(delay);

        // 슬라이드 업 + 페이드 인
        _sequence.Append(_rectTransform.DOAnchorPos(_basePosition, duration).SetEase(easeType));

        if (_canvasGroup != null)
            _sequence.Join(_canvasGroup.DOFade(1f, duration));
    }

    public void ResetToStart()
    {
        KillSequence();

        if (_rectTransform != null && _initialized)
            _rectTransform.anchoredPosition = _basePosition + new Vector2(0, -startOffsetY);

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    public void SetToEnd()
    {
        KillSequence();

        if (_rectTransform != null && _initialized)
            _rectTransform.anchoredPosition = _basePosition;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

    #endregion
}
