using UnityEngine;

/// <summary>
/// CompletionSparkle - 텍스트 재작성 완료 시 스프링 바운스로 등장하는 스파클 효과 컴포넌트
///
/// 【역할】 스케일 0에서 시작하여 스프링 오버슈트(지정 비율까지 초과 후 1.0으로 안착)하며 등장.
///          watchCanvasGroup의 알파 변화를 감시하여 페이드인 완료 시 자동 트리거 가능.
///          autoHide=true이면 일정 시간 후 자동 숨김.
/// 【사용 위치】 Problem3 Step2 (텍스트 재작성 완료 후 스파클 표시)
/// 【트리거】 외부에서 Show() 호출, 또는 watchCanvasGroup 알파 감시로 자동 트리거
/// 【의존성】 RectTransform(스케일), watchCanvasGroup(자동 트리거용, 선택)
/// </summary>
public class CompletionSparkle : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.4f;
    [SerializeField] private float delay = 0.3f;
    [SerializeField] private float overshoot = 1.2f;  // Spring overshoot

    [Header("Auto Hide")]
    [SerializeField] private bool autoHide = false;
    [SerializeField] private float hideDelay = 2f;

    [Header("Auto Trigger (monitors CanvasGroup)")]
    [SerializeField] private CanvasGroup watchCanvasGroup;
    [SerializeField] private bool triggerOnFadeInComplete = true;

    // Internal
    private RectTransform _rectTransform;
    private float _elapsed;
    private bool _isAnimating;
    private bool _delaying;
    private float _delayElapsed;
    private float _prevWatchAlpha;
    private bool _hasTriggered;

    private void OnEnable()
    {
        _rectTransform = GetComponent<RectTransform>();
        // Start hidden
        if (_rectTransform != null)
            _rectTransform.localScale = Vector3.zero;

        _isAnimating = false;
        _delaying = false;
        _hasTriggered = false;
        _prevWatchAlpha = watchCanvasGroup != null ? watchCanvasGroup.alpha : 0f;
    }

    private void Update()
    {
        // Auto trigger: watch CanvasGroup fade in complete
        if (triggerOnFadeInComplete && watchCanvasGroup != null && !_hasTriggered)
        {
            float currentAlpha = watchCanvasGroup.alpha;
            // Detect fade in complete: alpha went from < 1 to >= 0.99
            if (_prevWatchAlpha < 0.99f && currentAlpha >= 0.99f)
            {
                _hasTriggered = true;
                Show();
            }
            _prevWatchAlpha = currentAlpha;
        }

        // Delay phase
        if (_delaying)
        {
            _delayElapsed += Time.deltaTime;
            if (_delayElapsed >= delay)
            {
                _delaying = false;
                _isAnimating = true;
                _elapsed = 0f;
            }
            return;
        }

        // Animation phase
        if (!_isAnimating) return;
        if (_rectTransform == null) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / duration);

        // Spring-like easing with overshoot
        float scale = SpringEase(t, overshoot);
        _rectTransform.localScale = Vector3.one * scale;

        if (t >= 1f)
        {
            _isAnimating = false;
            _rectTransform.localScale = Vector3.one;

            if (autoHide)
            {
                Invoke(nameof(Hide), hideDelay);
            }
        }
    }

    /// <summary>
    /// Show sparkle with animation
    /// </summary>
    public void Show()
    {
        gameObject.SetActive(true);

        if (_rectTransform != null)
            _rectTransform.localScale = Vector3.zero;

        if (delay > 0f)
        {
            _delaying = true;
            _delayElapsed = 0f;
        }
        else
        {
            _isAnimating = true;
            _elapsed = 0f;
        }
    }

    /// <summary>
    /// Hide sparkle immediately
    /// </summary>
    public void Hide()
    {
        CancelInvoke(nameof(Hide));

        if (_rectTransform != null)
            _rectTransform.localScale = Vector3.zero;

        _isAnimating = false;
        _delaying = false;
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Reset for next step (allows re-triggering)
    /// </summary>
    public void ResetTrigger()
    {
        _hasTriggered = false;
        _prevWatchAlpha = watchCanvasGroup != null ? watchCanvasGroup.alpha : 0f;
        Hide();
    }

    /// <summary>
    /// Spring easing with overshoot
    /// </summary>
    private float SpringEase(float t, float overshoot)
    {
        if (t < 0.6f)
        {
            // Rise to overshoot
            float x = t / 0.6f;
            return EaseOutQuad(x) * overshoot;
        }
        else
        {
            // Settle to 1
            float x = (t - 0.6f) / 0.4f;
            return Mathf.Lerp(overshoot, 1f, EaseOutQuad(x));
        }
    }

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
