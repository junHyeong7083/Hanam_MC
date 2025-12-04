using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 선택 시 글로우 효과
/// - 선택되면 글로우 오브젝트 활성화 + 펄스
/// - Outline 또는 별도 글로우 이미지 사용
///
/// [사용처]
/// - Problem2 Step3: 관점 카드 선택 글로우
/// - 선택 가능한 카드/버튼
/// </summary>
public class SelectionGlow : MonoBehaviour
{
    [Header("===== 글로우 설정 =====")]
    [SerializeField] private GameObject glowObject;      // 글로우 이미지 오브젝트
    [SerializeField] private Image glowImage;            // 글로우 Image (알파 펄스용)

    [Header("펄스 애니메이션")]
    [SerializeField] private bool enablePulse = true;
    [SerializeField] private float pulseDuration = 1.5f;
    [SerializeField] private float minAlpha = 0.3f;
    [SerializeField] private float maxAlpha = 0.6f;

    [Header("스케일 (선택 시)")]
    [SerializeField] private bool enableScaleOnSelect = true;
    [SerializeField] private float selectedScale = 1.05f;
    [SerializeField] private float scaleSpeed = 8f;

    // 내부
    private bool _isSelected;
    private float _time;
    private Vector3 _originalScale;
    private Color _glowBaseColor;

    private void Awake()
    {
        _originalScale = transform.localScale;

        if (glowImage != null)
            _glowBaseColor = glowImage.color;

        // 초기 상태: 글로우 숨김
        if (glowObject != null)
            glowObject.SetActive(false);
    }

    private void Update()
    {
        // 스케일 애니메이션
        if (enableScaleOnSelect)
        {
            float targetScale = _isSelected ? selectedScale : 1f;
            float currentScale = transform.localScale.x / _originalScale.x;
            float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * scaleSpeed);
            transform.localScale = _originalScale * newScale;
        }

        // 글로우 펄스
        if (_isSelected && enablePulse && glowImage != null)
        {
            _time += Time.deltaTime;
            float normalizedTime = (_time % pulseDuration) / pulseDuration;
            float wave = Mathf.Sin(normalizedTime * Mathf.PI * 2f) * 0.5f + 0.5f;
            float alpha = Mathf.Lerp(minAlpha, maxAlpha, wave);

            Color c = _glowBaseColor;
            c.a = alpha;
            glowImage.color = c;
        }
    }

    /// <summary>
    /// 선택 상태 설정
    /// </summary>
    public void SetSelected(bool selected)
    {
        _isSelected = selected;
        _time = 0f;

        if (glowObject != null)
            glowObject.SetActive(selected);
    }

    /// <summary>
    /// 선택 토글
    /// </summary>
    public void ToggleSelection()
    {
        SetSelected(!_isSelected);
    }

    public bool IsSelected => _isSelected;
}
