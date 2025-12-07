using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem3 Step2: Scale effect when option is selected
/// - Monitors Image color change to detect selection
/// - When color matches selectedColor, scales up
/// - Works alongside existing logic without modification
/// </summary>
[RequireComponent(typeof(Image))]
public class OptionSelectScale : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Color selectedColor = new Color(1f, 0.54f, 0.24f);
    [SerializeField] private float colorTolerance = 0.1f;

    [Header("Scale Animation")]
    [SerializeField] private float selectedScale = 1.05f;
    [SerializeField] private float duration = 0.2f;

    // Internal
    private Image _image;
    private RectTransform _rectTransform;
    private Color _prevColor;
    private bool _isSelected;
    private bool _isAnimating;
    private float _elapsed;
    private float _startScale;
    private float _targetScale;


    private void OnEnable()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();

        _prevColor = _image != null ? _image.color : Color.white;
        _isSelected = false;
        _isAnimating = false;

        if (_rectTransform != null)
            _rectTransform.localScale = Vector3.one;
    }

    private void Update()
    {
        if (_image == null || _rectTransform == null) return;

        // Detect color change to selected color
        Color currentColor = _image.color;

        if (!_isSelected && IsColorMatch(currentColor, selectedColor))
        {
            _isSelected = true;
            StartScaleAnimation(1f, selectedScale);
        }
        else if (_isSelected && !IsColorMatch(currentColor, selectedColor))
        {
            // Deselected - scale back to 1
            _isSelected = false;
            StartScaleAnimation(_rectTransform.localScale.x, 1f);
        }

        _prevColor = currentColor;

        // Process animation
        if (_isAnimating)
        {
            _elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_elapsed / duration);
            float eased = EaseOutBack(t);

            float scale = Mathf.Lerp(_startScale, _targetScale, eased);
            _rectTransform.localScale = Vector3.one * scale;

            if (t >= 1f)
            {
                _isAnimating = false;
                _rectTransform.localScale = Vector3.one * _targetScale;
            }
        }
    }

    private void StartScaleAnimation(float from, float to)
    {
        _startScale = from;
        _targetScale = to;
        _elapsed = 0f;
        _isAnimating = true;
    }

    private bool IsColorMatch(Color a, Color b)
    {
        return Mathf.Abs(a.r - b.r) < colorTolerance &&
               Mathf.Abs(a.g - b.g) < colorTolerance &&
               Mathf.Abs(a.b - b.b) < colorTolerance;
    }

    /// <summary>
    /// Reset to normal state
    /// </summary>
    public void Reset()
    {
        _isSelected = false;
        _isAnimating = false;
        if (_rectTransform != null)
            _rectTransform.localScale = Vector3.one;
    }

    // EaseOutBack for nice overshoot on scale up
    private float EaseOutBack(float t)
    {
        const float c1 = 1.70158f;
        const float c3 = c1 + 1f;
        return 1f + c3 * Mathf.Pow(t - 1f, 3f) + c1 * Mathf.Pow(t - 1f, 2f);
    }
}
