using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 버튼 호버 애니메이션
/// - 마우스/터치 호버 시 스케일 확대
/// - PointerEnter/Exit 이벤트 사용
///
/// [사용처]
/// - Problem2 Step3: 관점 선택 버튼 호버
/// - 선택 가능한 버튼/카드
/// </summary>
public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("===== 호버 설정 =====")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float animationSpeed = 10f;

    // 내부
    private Vector3 _originalScale;
    private bool _isHovering;
    private bool _isInteractable = true;

    private void Awake()
    {
        _originalScale = transform.localScale;
    }

    private void Update()
    {
        if (!_isInteractable) return;

        float targetScale = _isHovering ? hoverScale : 1f;
        float currentScale = transform.localScale.x / _originalScale.x;
        float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * animationSpeed);
        transform.localScale = _originalScale * newScale;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isInteractable)
            _isHovering = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
    }

    /// <summary>
    /// 상호작용 가능 여부 설정
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
        if (!interactable)
        {
            _isHovering = false;
            transform.localScale = _originalScale;
        }
    }

    /// <summary>
    /// 원래 스케일 재설정
    /// </summary>
    public void ResetScale()
    {
        transform.localScale = _originalScale;
        _isHovering = false;
    }

    private void OnDisable()
    {
        _isHovering = false;
        if (_originalScale != Vector3.zero)
            transform.localScale = _originalScale;
    }
}
