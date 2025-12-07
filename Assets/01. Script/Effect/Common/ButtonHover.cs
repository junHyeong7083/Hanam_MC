using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 버튼 호버 애니메이션
/// - 마우스/터치 호버 시 스케일 확대
/// - 선택적으로 X축 이동 가능
/// - PointerEnter/Exit 이벤트 사용
///
/// [사용처]
/// - Problem2 Step3: 관점 선택 버튼 호버
/// - Problem3 Step2: 옵션 버튼 호버 (스케일 + X 이동)
/// - 선택 가능한 버튼/카드
/// </summary>
public class ButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("===== 호버 설정 =====")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float animationSpeed = 10f;

    [Header("===== X 이동 (옵션) =====")]
    [SerializeField] private bool enableMoveX = false;
    [SerializeField] private float moveXDistance = 10f;

    // 내부
    private RectTransform _rectTransform;
    private Vector3 _originalScale;
    private Vector2 _originalPosition;
    private bool _isHovering;
    private bool _isInteractable = true;

    private void Awake()
    {
        _originalScale = transform.localScale;
        _rectTransform = GetComponent<RectTransform>();
        if (_rectTransform != null)
            _originalPosition = _rectTransform.anchoredPosition;
    }

    private void Update()
    {
        if (!_isInteractable) return;

        // 스케일 애니메이션
        float targetScale = _isHovering ? hoverScale : 1f;
        float currentScale = transform.localScale.x / _originalScale.x;
        float newScale = Mathf.Lerp(currentScale, targetScale, Time.deltaTime * animationSpeed);
        transform.localScale = _originalScale * newScale;

        // X 이동 애니메이션 (옵션)
        if (enableMoveX && _rectTransform != null)
        {
            float targetX = _isHovering ? _originalPosition.x + moveXDistance : _originalPosition.x;
            float currentX = _rectTransform.anchoredPosition.x;
            float newX = Mathf.Lerp(currentX, targetX, Time.deltaTime * animationSpeed);
            _rectTransform.anchoredPosition = new Vector2(newX, _rectTransform.anchoredPosition.y);
        }
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
            ResetPosition();
        }
    }

    /// <summary>
    /// 원래 스케일 재설정
    /// </summary>
    public void ResetScale()
    {
        transform.localScale = _originalScale;
        _isHovering = false;
        ResetPosition();
    }

    private void ResetPosition()
    {
        if (enableMoveX && _rectTransform != null && _originalPosition != Vector2.zero)
        {
            _rectTransform.anchoredPosition = new Vector2(_originalPosition.x, _rectTransform.anchoredPosition.y);
        }
    }

    private void OnDisable()
    {
        _isHovering = false;
        if (_originalScale != Vector3.zero)
            transform.localScale = _originalScale;
        ResetPosition();
    }
}
