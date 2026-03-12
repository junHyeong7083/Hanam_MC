using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// StepInventoryItem - StepInventory 내 개별 아이템 슬롯 컴포넌트
///
/// 【역할】 인벤토리의 각 아이템을 표현하며, 잠금/해제 비주얼 전환과
///          드래그 앤 드롭 기능을 담당한다.
///          draggable이 true일 때만 드래그가 가능하며, 드래그 중에는
///          iconImage(클릭용 이미지)가 포인터를 따라 이동하고,
///          backgroundImage(반투명 이미지)는 원래 위치에 남아 있다.
///
/// 【참조하는 곳】 StepInventory — slots 배열에서 각 슬롯의 itemComponent로 참조,
///                Director_Problem2_Step1_Logic — 드래그 이벤트(OnItemDragBegin/Dragging/DragEnd) 구독,
///                Director_Problem2_DragItem — (레거시, StepInventoryItem으로 대체됨)
/// 【참조되는 곳】 GlowEffect — 드래그 가능 시 시각적 강조 효과,
///                Canvas — 드래그 중 아이콘을 Canvas 최상위로 이동하여 다른 UI 위에 표시
///
/// 【흐름】
///   1. StepInventory.SetDraggable()로 draggable 플래그 설정
///   2. 유저가 드래그 시작 → OnBeginDrag: 아이콘을 Canvas 최상위로 이동, raycast 비활성화
///   3. 드래그 중 → OnDrag: 아이콘이 포인터 위치를 따라 이동
///   4. 드래그 종료 → OnEndDrag: OnItemDragEnd 이벤트 발행
///   5. InventoryDropTargetStepBase에서 드롭 성공/실패 판정 후 ResetIconPosition() 호출
/// </summary>
public class StepInventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    /// <summary>아이템 고유 식별자 (StepInventory.InventorySlot.itemId와 매칭)</summary>
    public string itemId;

    /// <summary>잠금 상태일 때 표시되는 루트 오브젝트</summary>
    public GameObject lockedRoot;

    /// <summary>해제 상태일 때 표시되는 루트 오브젝트</summary>
    public GameObject unlockedRoot;

    /// <summary>잠금 아이콘 이미지 (더미 리소스 대응용)</summary>
    public GameObject lockedImg;

    /// <summary>드래그 시 커서를 따라가는 아이콘 이미지 (BackgroundImageClick)</summary>
    public Image iconImage;          // BackgroundImageClcik — 드래그 시 커서 따라감

    /// <summary>드래그 중 원위치에 남아있는 반투명 배경 이미지 (BackgroundImageAlpha)</summary>
    public Image backgroundImage;    // BackgroundImageAlpha — 원위치에 남음

    /// <summary>드래그 가능 상태를 시각적으로 표시하는 글로우 효과 컴포넌트</summary>
    public GlowEffect glowEffect;

    /// <summary>아이콘의 RectTransform (위치 계산용)</summary>
    public RectTransform iconRect;

    /// <summary>드래그 시 아이콘을 최상위로 올릴 루트 Canvas 참조</summary>
    public Canvas rootCanvas;

    /// <summary>마우스 호버 시 스케일 배율 (현재 미사용, 향후 확장용)</summary>
    public float hoverScale = 1.05f;

    /// <summary>드래그 가능 여부 (StepInventory에서 제어). 인스펙터에 노출하지 않음.</summary>
    [HideInInspector] public bool draggable;

    /// <summary>itemId의 읽기 전용 프로퍼티</summary>
    public string ItemId => itemId;

    /// <summary>드래그 시작 시 발행되는 이벤트 (InventoryDropTargetStepBase에서 구독)</summary>
    public Action<StepInventoryItem> OnItemDragBegin;

    /// <summary>드래그 진행 중 매 프레임 발행되는 이벤트</summary>
    public Action<StepInventoryItem, PointerEventData> OnItemDragging;

    /// <summary>드래그 종료 시 발행되는 이벤트 (드롭 판정에 사용)</summary>
    public Action<StepInventoryItem, PointerEventData> OnItemDragEnd;

    /// <summary>드래그 시작 전 아이콘의 원래 부모 Transform (복귀용)</summary>
    private Transform _iconOriginalParent;

    /// <summary>드래그 시작 전 아이콘의 원래 anchoredPosition (복귀용)</summary>
    private Vector2 _iconOriginalPos;

    /// <summary>현재 드래그 중인지 여부</summary>
    private bool _isDragging;

    /// <summary>
    /// 활성화 시 rootCanvas와 glowEffect가 미설정이면 자동으로 찾아서 캐싱한다.
    /// rootCanvas: 드래그 시 아이콘을 최상위로 올리기 위해 필요
    /// glowEffect: 드래그 가능 상태 시각 표시용
    /// </summary>
    private void OnEnable()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (glowEffect == null && iconImage != null)
            glowEffect = iconImage.GetComponentInChildren<GlowEffect>(true);
    }

    /// <summary>
    /// 아이템의 잠금/해제 비주얼을 전환한다.
    /// locked=true: lockedImg 표시 + unlockedRoot 숨김 (아이템 미획득 상태)
    /// locked=false: lockedImg 숨김 + unlockedRoot 표시 (아이템 획득 완료 상태)
    /// 주의: lockedRoot는 항상 true로 설정됨 (더미 리소스 대응)
    /// </summary>
    /// <param name="locked">잠금 상태 여부</param>
    public void SetLocked(bool locked)
    {
        //if (lockedRoot != null) lockedRoot.SetActive(locked);
        if (lockedRoot != null) lockedRoot.SetActive(true);
        if (lockedImg != null) lockedImg.SetActive(locked); /// 더미용 리소스 대응 스크립트
        if (unlockedRoot != null) unlockedRoot.SetActive(!locked);
    }

    /// <summary>
    /// 드래그 시작 처리. draggable이 false이면 드래그를 무시한다.
    /// 아이콘을 Canvas 최상위로 이동시켜 다른 UI 요소 위에 표시되도록 한다.
    /// raycastTarget을 false로 설정하여 드롭 대상이 아이콘 아래의 오브젝트를 감지할 수 있게 한다.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!draggable)
        {
            // 드래그 불가 상태: pointerDrag를 null로 설정하여 이벤트 전파 차단
            eventData.pointerDrag = null;
            return;
        }

        _isDragging = true;

        if (iconImage != null)
        {
            // 원래 위치 저장 (드래그 종료 후 복귀용)
            _iconOriginalParent = iconImage.transform.parent;
            _iconOriginalPos = iconImage.rectTransform.anchoredPosition;

            // Canvas 최상위로 이동 → 다른 UI 위에 표시
            if (rootCanvas != null)
                iconImage.transform.SetParent(rootCanvas.transform, true);

            // raycast 비활성화: 드롭 대상이 이 아이콘이 아닌 아래의 드롭 영역을 감지하도록
            iconImage.raycastTarget = false;
        }

        OnItemDragBegin?.Invoke(this);
    }

    /// <summary>
    /// 드래그 중 매 프레임 호출. 아이콘을 마우스/터치 위치로 이동시킨다.
    /// 스크린 좌표를 Canvas 로컬 좌표로 변환하여 정확한 위치에 배치한다.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || iconImage == null) return;

        if (rootCanvas != null)
        {
            var canvasRect = rootCanvas.transform as RectTransform;
            Vector2 localPoint;
            // 스크린 좌표 → Canvas 로컬 좌표 변환
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                iconImage.rectTransform.anchoredPosition = localPoint;
            }
        }

        OnItemDragging?.Invoke(this, eventData);
    }

    /// <summary>
    /// 드래그 종료 처리. OnItemDragEnd 이벤트를 발행하여
    /// InventoryDropTargetStepBase에서 드롭 성공/실패를 판정하게 한다.
    /// 아이콘 복귀는 ResetIconPosition()에서 별도로 처리한다.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;
        _isDragging = false;

        OnItemDragEnd?.Invoke(this, eventData);
    }

    /// <summary>
    /// iconImage를 원래 위치/부모로 복귀
    /// </summary>
    public void ResetIconPosition()
    {
        if (iconImage != null && _iconOriginalParent != null)
        {
            iconImage.transform.SetParent(_iconOriginalParent, false);
            iconImage.rectTransform.anchoredPosition = _iconOriginalPos;
            iconImage.raycastTarget = true;
        }

        _isDragging = false;
    }
}
