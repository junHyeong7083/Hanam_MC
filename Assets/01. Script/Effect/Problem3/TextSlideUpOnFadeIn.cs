using UnityEngine;

/// <summary>
/// Problem3 Step2: Slide up when CanvasGroup alpha increases
/// - Monitors CanvasGroup.alpha changes
/// - When alpha starts increasing from 0, triggers slide up animation
/// - Works alongside existing fade logic without modification
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class TextSlideUpOnFadeIn : MonoBehaviour
{
    [Header("Slide Settings")]
    [SerializeField] private float slideDistance = 20f;
    [SerializeField] private float slideDuration = 0.25f;

    // Internal
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Vector2 _basePosition;
    private float _prevAlpha;
    private bool _isSliding;
    private float _slideElapsed;
    private bool _initialized;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
    }

    private void OnEnable()
    {
        // Save base position on first enable
        if (!_initialized && _rectTransform != null)
        {
            _basePosition = _rectTransform.anchoredPosition;
            _initialized = true;
        }

        _prevAlpha = _canvasGroup != null ? _canvasGroup.alpha : 1f;
        _isSliding = false;
    }

    private void Update()
    {
        if (_canvasGroup == null || _rectTransform == null) return;

        float currentAlpha = _canvasGroup.alpha;

        // Detect fade in start: alpha was near 0 and now increasing
        if (!_isSliding && _prevAlpha < 0.01f && currentAlpha > _prevAlpha)
        {
            StartSlide();
        }

        _prevAlpha = currentAlpha;

        // Process slide animation
        if (_isSliding)
        {
            _slideElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_slideElapsed / slideDuration);
            float eased = EaseOutQuad(t);

            Vector2 startPos = _basePosition + Vector2.down * slideDistance;
            _rectTransform.anchoredPosition = Vector2.Lerp(startPos, _basePosition, eased);

            if (t >= 1f)
            {
                _isSliding = false;
                _rectTransform.anchoredPosition = _basePosition;
            }
        }
    }

    private void StartSlide()
    {
        _isSliding = true;
        _slideElapsed = 0f;

        // Set initial position (below base)
        _rectTransform.anchoredPosition = _basePosition + Vector2.down * slideDistance;
    }

    /// <summary>
    /// Reset for next step (allows re-triggering slide)
    /// </summary>
    public void ResetForNextStep()
    {
        _isSliding = false;
        _prevAlpha = _canvasGroup != null ? _canvasGroup.alpha : 0f;

        if (_rectTransform != null && _initialized)
        {
            _rectTransform.anchoredPosition = _basePosition;
        }
    }

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
