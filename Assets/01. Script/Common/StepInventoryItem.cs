using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// StepInventory 내 개별 아이템 슬롯.
/// - Locked/Unlocked 비주얼 전환
/// - 드래그 지원: draggable=true일 때만 드래그 가능
/// - 드래그 중 iconImage(Click)가 포인터 따라감, backgroundImage(Alpha)는 원위치에 표시
/// </summary>
public class StepInventoryItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public string itemId;
    public GameObject lockedRoot;
    public GameObject unlockedRoot;
    public GameObject lockedImg;
    public Image iconImage;          // BackgroundImageClcik — 드래그 시 커서 따라감
    public Image backgroundImage;    // BackgroundImageAlpha — 원위치에 남음
    public GlowEffect glowEffect;
    public RectTransform iconRect;
    public Canvas rootCanvas;
    public float hoverScale = 1.05f;

    [HideInInspector] public bool draggable;

    public string ItemId => itemId;

    public Action<StepInventoryItem> OnItemDragBegin;
    public Action<StepInventoryItem, PointerEventData> OnItemDragging;
    public Action<StepInventoryItem, PointerEventData> OnItemDragEnd;

    private Transform _iconOriginalParent;
    private Vector2 _iconOriginalPos;
    private bool _isDragging;

    private void OnEnable()
    {
        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        if (glowEffect == null && iconImage != null)
            glowEffect = iconImage.GetComponentInChildren<GlowEffect>(true);
    }

    public void SetLocked(bool locked)
    {
        //if (lockedRoot != null) lockedRoot.SetActive(locked);
        if (lockedRoot != null) lockedRoot.SetActive(true);
        if (lockedImg != null) lockedImg.SetActive(locked); /// 더미용 리소스 대응 스크립트
        if (unlockedRoot != null) unlockedRoot.SetActive(!locked);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!draggable)
        {
            eventData.pointerDrag = null;
            return;
        }

        _isDragging = true;

        if (iconImage != null)
        {
            _iconOriginalParent = iconImage.transform.parent;
            _iconOriginalPos = iconImage.rectTransform.anchoredPosition;

            // Canvas 최상위로 이동 → 다른 UI 위에 표시
            if (rootCanvas != null)
                iconImage.transform.SetParent(rootCanvas.transform, true);

            iconImage.raycastTarget = false;
        }

        OnItemDragBegin?.Invoke(this);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging || iconImage == null) return;

        if (rootCanvas != null)
        {
            var canvasRect = rootCanvas.transform as RectTransform;
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    canvasRect, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                iconImage.rectTransform.anchoredPosition = localPoint;
            }
        }

        OnItemDragging?.Invoke(this, eventData);
    }

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
