using UnityEngine;

/// <summary>
/// Problem3 Step2: Pen write animation
/// - Moves from left to right simulating writing
/// - Call Play() when rewriting starts
/// - Auto-hides after animation completes
/// </summary>
public class PenWriteAnimation : MonoBehaviour
{
    [Header("Animation Settings")]
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float startX = -100f;
    [SerializeField] private float endX = 400f;
    [SerializeField] private float yOffset = 100f;

    [Header("Rotation")]
    [SerializeField] private float startRotation = -10f;
    [SerializeField] private float endRotation = 0f;

    [Header("Fade")]
    [SerializeField] private bool enableFade = true;
    [SerializeField] private float fadeInRatio = 0.2f;   // 0~0.2: fade in
    [SerializeField] private float fadeOutStart = 0.8f;  // 0.8~1: fade out

    [Header("Auto Play")]
    [SerializeField] private bool playOnEnable = true;

    // Internal
    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private float _elapsed;
    private bool _isPlaying;
    private Vector2 _originalPosition;

    private void OnEnable()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _originalPosition = _rectTransform.anchoredPosition;

        if (playOnEnable)
            Play();
        else
            ResetState();
    }

    private void Update()
    {
        if (!_isPlaying) return;

        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / duration);

        // Position: left to right
        float x = Mathf.Lerp(startX, endX, EaseOutQuad(t));
        _rectTransform.anchoredPosition = new Vector2(x, yOffset);

        // Rotation: slight tilt to straight
        float rot = Mathf.Lerp(startRotation, endRotation, t);
        _rectTransform.localRotation = Quaternion.Euler(0, 0, rot);

        // Alpha: fade in -> hold -> fade out
        if (enableFade && _canvasGroup != null)
        {
            float alpha;
            if (t < fadeInRatio)
            {
                // Fade in
                alpha = t / fadeInRatio;
            }
            else if (t < fadeOutStart)
            {
                // Hold
                alpha = 1f;
            }
            else
            {
                // Fade out
                alpha = 1f - ((t - fadeOutStart) / (1f - fadeOutStart));
            }
            _canvasGroup.alpha = alpha;
        }

        // Complete
        if (t >= 1f)
        {
            _isPlaying = false;
            gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Start pen write animation
    /// </summary>
    public void Play()
    {
        gameObject.SetActive(true);
        _elapsed = 0f;
        _isPlaying = true;

        // Initial state
        _rectTransform.anchoredPosition = new Vector2(startX, yOffset);
        _rectTransform.localRotation = Quaternion.Euler(0, 0, startRotation);

        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Stop and hide immediately
    /// </summary>
    public void Stop()
    {
        _isPlaying = false;
        gameObject.SetActive(false);
    }

    private void ResetState()
    {
        _isPlaying = false;
        if (_canvasGroup != null)
            _canvasGroup.alpha = 0f;
    }

    private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
}
