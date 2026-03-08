using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Problem2 / Step1 에서 사용하던 드래그 아이템 (Legacy).
/// 현재는 StepInventory 기반으로 대체됨.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class Director_Problem2_DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("UI References")]
    [SerializeField] private Image itemImage;
    [SerializeField] private Canvas rootCanvas;

    private RectTransform _rect;

    private Vector2 _originalAnchoredPos;
    private bool _initializedPos = false;

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

    public void ResetToOriginalState()
    {
        ReturnToOriginalPosition();
    }

    public void ReturnToOriginalPosition()
    {
        if (!_initializedPos) return;
        _rect.anchoredPosition = _originalAnchoredPos;
    }

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
