using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Director_Problem2_DragItem - 문제2 스텝1용 드래그 아이템 (Legacy/미사용).
///
/// 【역할】 과거에 Problem2 Step1에서 드래그앤드롭 아이템으로 사용되었으나,
///         현재는 StepInventory + StepInventoryItem 시스템으로 완전히 대체되었다.
///         코드는 남아있지만 실제로 사용되지 않는 레거시 컴포넌트이다.
///         IBeginDragHandler, IDragHandler, IEndDragHandler 인터페이스를 구현하지만
///         모든 핸들러가 no-op(빈 메서드)이다.
/// 【패턴】 독립 컴포넌트 (Binder/Logic 패턴 미적용)
/// 【문제/스텝】 Director 테마 / 문제2 / 스텝1 (레거시)
/// 【부모 클래스】 MonoBehaviour + IDrag 인터페이스
/// 【참조하는 곳】 없음 (레거시, 현재 미사용)
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class Director_Problem2_DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemImage;         // 아이템 이미지
    [SerializeField] private Canvas rootCanvas;       // 루트 캔버스 참조

    private RectTransform _rect;                      // 자신의 RectTransform 캐시

    private Vector2 _originalAnchoredPos;             // 초기 앵커 위치 (복원용)
    private bool _initializedPos = false;             // 초기 위치 저장 완료 여부

    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (itemImage == null)
            itemImage = GetComponent<Image>();
    }

    private void OnEnable()
    {
        if (!_initializedPos)
        {
            _originalAnchoredPos = _rect.anchoredPosition;
            _initializedPos = true;
        }
    }

    private void OnDisable()
    {
        ResetToOriginalState();
    }

    /// <summary>원래 상태로 초기화 (위치 복원).</summary>
    public void ResetToOriginalState()
    {
        ReturnToOriginalPosition();
    }

    /// <summary>저장된 초기 위치로 복원한다.</summary>
    public void ReturnToOriginalPosition()
    {
        if (!_initializedPos) return;
        _rect.anchoredPosition = _originalAnchoredPos;
    }

    /// <summary>드롭 박스 중앙으로 위치를 스냅한다.</summary>
    public void SnapToDropBoxCenter(RectTransform dropBox)
    {
        if (dropBox == null) return;
        _rect.position = dropBox.position;
    }

    // =======================
    //   Drag 이벤트 (no-op)
    // =======================

    public void OnBeginDrag(PointerEventData eventData) { }
    public void OnDrag(PointerEventData eventData) { }
    public void OnEndDrag(PointerEventData eventData) { }
}
