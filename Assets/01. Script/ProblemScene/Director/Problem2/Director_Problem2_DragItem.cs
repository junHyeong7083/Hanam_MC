using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Problem2 / Step1 에서 사용하는 드래그 가능한 장면 아이템.
/// - 누르고 끌면 마우스를 따라 움직임.
/// - 드래그 중에는 원래 자리에는 고스트(반투명) 이미지가 남고,
///   실제로 움직이는 건 이 RectTransform.
/// - 드래그 종료 시 Step1 컨트롤러에게 드롭 결과를 넘김.
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class Director_Problem2_DragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{

    [Header("UI References")]
    [SerializeField] private Image itemImage;     // 실제로 움직이는 이미지
    [SerializeField] private Image ghostImage;    // 원래 자리에 남는 반투명 고스트
    [SerializeField] private Canvas rootCanvas;   // 최상위 Canvas (UI 좌표 계산용)

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

        // 고스트가 지정돼 있으면 시작 시에는 숨겨두기
        if (ghostImage != null)
        {
            ghostImage.gameObject.SetActive(false);
        }
    }

    private void OnEnable()
    {
        // 첫 Enable 시점에 원래 위치 저장
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
    /// 외부에서 전체 상태 초기화 시 호출.
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

        // 고스트 스프라이트를 원본과 동일하게 맞추고
        ghostImage.sprite = itemImage != null ? itemImage.sprite : ghostImage.sprite;

        var c = ghostImage.color;
        c.a = 0.35f; // 반투명 정도
        ghostImage.color = c;

        ghostImage.gameObject.SetActive(true);
    }

    private void HideGhost()
    {
        if (ghostImage == null) return;
        ghostImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 드래그 박스 중앙으로 스냅
    /// (드롭 성공 시 Step1에서 호출)
    /// </summary>
    public void SnapToDropBoxCenter(RectTransform dropBox)
    {
        if (dropBox == null) return;

        _rect.position = dropBox.position;

        // 드롭이 완료되면 고스트는 굳이 남길 필요 없다고 보고 숨김
        HideGhost();

        // 필요하면 여기서 아이템을 다시 완전 불투명으로
        RestoreOriginalAlpha();
    }

    // =======================
    //   Drag 이벤트 구현부
    // =======================

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (_stepController != null)
            _stepController.NotifyDragBegin(this);

        // 1) 드래그 시작할 때 원래 자리에는 반투명 고스트를 켜고
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
            Debug.LogWarning("[Director_Problem2_DragItem] rootCanvas가 설정되어 있지 않음");
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
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (_stepController != null)
            _stepController.NotifyDragEnd(this, eventData);
        // 드롭 성공 여부에 따라 실제 위치/고스트 정리는
        // Step1에서 ReturnToOriginalPosition() 또는 SnapToDropBoxCenter()를 호출하면서 처리됨.
    }
}
