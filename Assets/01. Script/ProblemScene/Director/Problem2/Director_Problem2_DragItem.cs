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
    [SerializeField] private Image itemImage;     // ������ �����̴� �̹���
    [SerializeField] private Image ghostImage;    // ���� �ڸ��� ���� ������ ����Ʈ
    [SerializeField] private Canvas rootCanvas;   // �ֻ��� Canvas (UI ��ǥ ����)

    private RectTransform _rect;
    private Director_Problem2_Step1_Logic _stepController;

    private Vector2 _originalAnchoredPos;
    private bool _initializedPos = false;
    private float _originalAlpha = 1f;


    private void Awake()
    {
        _rect = GetComponent<RectTransform>();

        if (itemImage == null)
            itemImage = GetComponent<Image>();

        if (itemImage != null)
            _originalAlpha = itemImage.color.a;

        // ����Ʈ�� ������ ������ ���� �ÿ��� ���ܵα�
        if (ghostImage != null)
        {
            ghostImage.gameObject.SetActive(false);
        }
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
        RestoreOriginalAlpha();
        HideGhost();
    }

    public void ReturnToOriginalPosition()
    {
        if (!_initializedPos) return;
        _rect.anchoredPosition = _originalAnchoredPos;
    }

    public void RestoreOriginalAlpha()
    {
        if (itemImage == null) return;
        var c = itemImage.color;
        c.a = _originalAlpha;
        itemImage.color = c;
    }

    private void ShowGhost()
    {
        if (ghostImage == null) return;

        // ����Ʈ ��������Ʈ�� ������ �����ϰ� ���߰�
        ghostImage.sprite = itemImage != null ? itemImage.sprite : ghostImage.sprite;

        var c = ghostImage.color;
        c.a = 0.35f; // ������ ����
        ghostImage.color = c;

        ghostImage.gameObject.SetActive(true);
    }

    private void HideGhost()
    {
        if (ghostImage == null) return;
        ghostImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// �巡�� �ڽ� �߾����� ����
    /// (��� ���� �� Step1���� ȣ��)
    /// </summary>
    public void SnapToDropBoxCenter(RectTransform dropBox)
    {
        if (dropBox == null) return;

        _rect.position = dropBox.position;

        // ����� �Ϸ�Ǹ� ����Ʈ�� ���� ���� �ʿ� ���ٰ� ���� ����
        HideGhost();

        // �ʿ��ϸ� ���⼭ �������� �ٽ� ���� ����������
        RestoreOriginalAlpha();
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

        // 1) 드래그 시작하면 원래 자리에는 고스트 이미지를 켬
        ShowGhost();

        // 2) 드래그되는 아이템은 완전 불투명
        if (itemImage != null)
        {
            var c = itemImage.color;
            c.a = 1f;
            itemImage.color = c;
        }
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
