using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 드래그 시 스케일/투명도 피드백
/// - 드래그 시작: scale 축소 + 투명도 감소
/// - 호버: scale 살짝 확대
/// - IDragHandler 인터페이스 자체 구현 또는 외부에서 호출
///
/// [사용처]
/// - Problem2 Step1: 마음 렌즈 드래그 아이템
/// - 모든 드래그 가능 아이템
/// </summary>
public class DragScaleFeedback : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("===== 드래그 설정 =====")]
    [SerializeField] private float draggingScale = 0.9f;
    [SerializeField] private float draggingAlpha = 0.5f;

    [Header("===== 호버 설정 =====")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private bool enableHover = true;

    [Header("===== 애니메이션 =====")]
    [SerializeField] private float transitionDuration = 0.15f;

    [Header("===== 타겟 (비워두면 자기 자신) =====")]
    [SerializeField] private RectTransform targetTransform;
    [SerializeField] private CanvasGroup targetCanvasGroup;

    // 상태
    private Vector3 _baseScale;
    private float _baseAlpha;
    private bool _isDragging;
    private bool _isHovering;

    // 현재 목표값
    private float _targetScale;
    private float _targetAlpha;
    private float _currentScale;
    private float _currentAlpha;

    private void Awake()
    {
        if (targetTransform == null)
            targetTransform = transform as RectTransform;

        if (targetCanvasGroup == null)
            targetCanvasGroup = GetComponent<CanvasGroup>();

        // 기본값 저장
        if (targetTransform != null)
        {
            _baseScale = targetTransform.localScale;
            _currentScale = 1f;
            _targetScale = 1f;
        }

        if (targetCanvasGroup != null)
        {
            _baseAlpha = targetCanvasGroup.alpha;
            _currentAlpha = _baseAlpha;
            _targetAlpha = _baseAlpha;
        }
    }

    private void Update()
    {
        // 부드러운 전환
        if (transitionDuration > 0f)
        {
            float speed = Time.deltaTime / transitionDuration;
            _currentScale = Mathf.MoveTowards(_currentScale, _targetScale, speed);
            _currentAlpha = Mathf.MoveTowards(_currentAlpha, _targetAlpha, speed);
        }
        else
        {
            _currentScale = _targetScale;
            _currentAlpha = _targetAlpha;
        }

        ApplyVisuals();
    }

    private void ApplyVisuals()
    {
        if (targetTransform != null)
            targetTransform.localScale = _baseScale * _currentScale;

        if (targetCanvasGroup != null)
            targetCanvasGroup.alpha = _currentAlpha;
    }

    private void UpdateTargetState()
    {
        if (_isDragging)
        {
            _targetScale = draggingScale;
            _targetAlpha = draggingAlpha;
        }
        else if (_isHovering && enableHover)
        {
            _targetScale = hoverScale;
            _targetAlpha = _baseAlpha;
        }
        else
        {
            _targetScale = 1f;
            _targetAlpha = _baseAlpha;
        }
    }

    #region Pointer Events (Hover)

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_isDragging) return;
        _isHovering = true;
        UpdateTargetState();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHovering = false;
        UpdateTargetState();
    }

    #endregion

    #region Public API (외부에서 드래그 상태 알림)

    /// <summary>
    /// 드래그 시작 시 호출 (Director_Problem2_DragItem 등에서)
    /// </summary>
    public void OnDragBegin()
    {
        _isDragging = true;
        _isHovering = false;
        UpdateTargetState();
    }

    /// <summary>
    /// 드래그 종료 시 호출
    /// </summary>
    public void OnDragEnd()
    {
        _isDragging = false;
        UpdateTargetState();
    }

    /// <summary>
    /// 즉시 원래 상태로 복원
    /// </summary>
    public void ResetImmediate()
    {
        _isDragging = false;
        _isHovering = false;
        _currentScale = 1f;
        _currentAlpha = _baseAlpha;
        _targetScale = 1f;
        _targetAlpha = _baseAlpha;
        ApplyVisuals();
    }

    #endregion
}
