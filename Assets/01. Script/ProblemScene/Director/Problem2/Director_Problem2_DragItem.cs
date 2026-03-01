using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Problem2 / Step1 ���� ����ϴ� �巡�� ������ ��� ������.
/// - ������ ���� ���콺�� ���� ������.
/// - �巡�� �߿��� ���� �ڸ����� ����Ʈ(������) �̹����� ����,
///   ������ �����̴� �� �� RectTransform.
/// - �巡�� ���� �� Step1 ��Ʈ�ѷ����� ��� ����� �ѱ�.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class Director_Problem2_DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    [Header("UI References")]
    [SerializeField] private Image itemImage;
    [SerializeField] private Canvas rootCanvas;

    private RectTransform _rect;
    private Director_Problem2_Step1_Logic _stepController;

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
        // ù Enable ������ ���� ��ġ ����
        if (!_initializedPos)
        {
            _originalAnchoredPos = _rect.anchoredPosition;
            _initializedPos = true;
        }
    }

    private void OnDisable()
    {
        // 씬/스텝 전환 시 드래그 상태 정리 (잔상 방지)
        ResetToOriginalState();
    }

    public void SetStepController(Director_Problem2_Step1_Logic controller)
    {
        _stepController = controller;
    }

    /// <summary>
    /// �ܺο��� ��ü ���� �ʱ�ȭ �� ȣ��.
    /// </summary>
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
    //   Drag �̺�Ʈ ������
    // =======================

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log($"[DragItem] OnBeginDrag - _stepController={(_stepController != null ? "OK" : "NULL")}");

        if (_stepController != null)
            _stepController.NotifyDragBegin(this);
        else
            Debug.LogWarning("[DragItem] _stepController가 null! SetStepController가 호출되지 않았습니다.");

    }

    public void OnDrag(PointerEventData eventData)
    {
        if (rootCanvas == null)
        {
            Debug.LogWarning("[DragItem] rootCanvas가 설정되지 않음! 인스펙터에서 rootCanvas를 할당하세요.");
            return;
        }

        RectTransform canvasRect = rootCanvas.transform as RectTransform;
        if (canvasRect == null) return;

        Vector2 localPoint;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint))
        {
            _rect.anchoredPosition = localPoint;
        }

        if (_stepController != null)
            _stepController.NotifyDragging(this, eventData);
        else
            Debug.LogWarning("[DragItem] OnDrag: _stepController가 null!");
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log($"[DragItem] OnEndDrag - _stepController={(_stepController != null ? "OK" : "NULL")}");

        if (_stepController != null)
            _stepController.NotifyDragEnd(this, eventData);
        else
            Debug.LogWarning("[DragItem] OnEndDrag: _stepController가 null! 드롭이 처리되지 않습니다.");
    }
}
