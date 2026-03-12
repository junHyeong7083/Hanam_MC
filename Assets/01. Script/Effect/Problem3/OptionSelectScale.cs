using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// OptionSelectScale - Image 색상 변화를 감지하여 선택 시 스케일 확대 효과를 적용하는 컴포넌트
///
/// 【역할】 매 프레임 Image.color를 모니터링하여 selectedColor와 일치하면 스케일 확대(EaseOutBack),
///          색상이 변경되면 스케일 1.0으로 복귀. 기존 로직 수정 없이 선택 피드백 추가 가능.
/// 【사용 위치】 Problem3 Step2 (선택지 옵션에 부착하여 선택 시 확대 효과)
/// 【트리거】 Update에서 Image.color 변화 자동 감지 (외부 호출 불필요)
/// 【의존성】 Image(RequireComponent - 색상 감지 대상), RectTransform(스케일)
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
