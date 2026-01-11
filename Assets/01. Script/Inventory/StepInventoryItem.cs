using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Step 내부 인벤토리의 각 아이템 슬롯에서
/// - Hover 시 살짝 확대
/// - 필요 시 wiggle(크기 진동) 연출
/// - 드래그 시 반투명 고스트 + 아이콘 드래그
/// 를 담당하는 컴포넌트.
/// </summary>
public class StepInventoryItem : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("아이템 ID (선택 사항)")]
    [Tooltip("DB InventoryItem.ItemId 와 맞춰두면 Step 쪽에서 식별하기 편함")]
    public string itemId;

    [Header("락/언락 루트")]
    [SerializeField] private GameObject lockedRoot;
    [SerializeField] private GameObject unlockedRoot;

    [Header("UI 참조")]
    [Tooltip("드래그되는 실제 아이콘 이미지")]
    [SerializeField] private Image iconImage;

    [Tooltip("슬롯에 남겨둘 반투명 배경(고스트) 이미지")]
    [SerializeField] private Image backgroundImage;

    [Tooltip("이 아이콘이 속한 RectTransform. 보통 iconImage.rectTransform 와 동일")]
    [SerializeField] private RectTransform iconRect;

    [Tooltip("UI Canvas (ScreenSpace Overlay 기준)")]
    [SerializeField] private Canvas rootCanvas;

    [Header("Hover 설정")]
    [SerializeField] private float hoverScale = 1.05f;
    [SerializeField] private float hoverSpeed = 10f;

    [Header("Wiggle 설정 (드래그 가능한 아이템 강조용)")]
    [SerializeField] private bool wiggleEnabled = false;
    [SerializeField] private float wiggleAmplitude = 0.05f;
    [SerializeField] private float wiggleSpeed = 4f;

    MonoBehaviour dragHandlerTarget; // IStepInventoryDragHandler 구현체 (비워놔도 됨)

    private IStepInventoryDragHandler _dragHandler;

    // 내부 상태
    private bool _canDrag = false;
    private bool _isHover;
    private bool _isDragging;

    private Vector3 _originalScale;
    private Vector2 _originalAnchoredPos;
    private Transform _originalParent;
    private Canvas _cachedCanvas;

    private bool _initialized;

    /// <summary>
    /// 이 스텝에서 실제로 드래그 가능한 아이템인지 외부에서 조회할 때 사용
    /// (Step 쪽에서 penItemId 비교 대신 이거만 보면 됨)
    /// </summary>
    public bool IsDraggableThisStep => _canDrag;

    // ==== 초기화 & 활성화 ====

    private void OnEnable()
    {
        // 한 번만 하는 초기화
        if (!_initialized)
        {
            InitOnce();
            _initialized = true;
        }

        _originalScale = new Vector3(1,1,1);
        // 활성화될 때마다 리셋할 것들
        if (iconRect != null)
        {
            _originalAnchoredPos = iconRect.anchoredPosition;
        }

        _isHover = false;
        _isDragging = false;

        StopAllCoroutines();

        if (wiggleEnabled)
        {
            StartCoroutine(WiggleRoutine());
        }
        else if (iconRect != null)
        {
            iconRect.localScale = _originalScale;
        }
    }

    private void OnDisable()
    {
        // 씬/스텝 전환 시 드래그 상태 정리 (잔상 방지)
        if (_isDragging)
        {
            _isDragging = false;
            ReturnToSlot();
        }
    }

    private void InitOnce()
    {
        if (iconRect == null && iconImage != null)
            iconRect = iconImage.rectTransform;

        if (iconRect != null && _originalScale == Vector3.zero)
            _originalScale = iconRect.localScale;

        if (rootCanvas == null)
            rootCanvas = GetComponentInParent<Canvas>();

        _cachedCanvas = rootCanvas;

        if (backgroundImage != null)
        {
            var c = backgroundImage.color;
            c.a = 0f;
            backgroundImage.color = c;
        }
        // 1순위: 인스펙터에 직접 연결된 타겟
        if (dragHandlerTarget is IStepInventoryDragHandler handlerFromField)
        {
            _dragHandler = handlerFromField;
        }
        else
        {
            // 2순위: 부모 트랜스폼에서 자동으로 찾기
            var handlerFromParent = GetComponentInParent<IStepInventoryDragHandler>();
            if (handlerFromParent != null)
            {
                _dragHandler = handlerFromParent;
                dragHandlerTarget = _dragHandler as MonoBehaviour;
            }
        }
    }

    // 🔒 락/언락 비주얼
    public void SetUnlockedVisual(bool isUnlocked)
    {
        if (lockedRoot != null)
            lockedRoot.SetActive(!isUnlocked);

        if (unlockedRoot != null)
            unlockedRoot.SetActive(isUnlocked);
    }

    /// <summary>
    /// 이 스텝에서 드래그 가능 여부 설정 (StepInventoryPanel 에서 호출).
    /// </summary>
    public void SetDraggable(bool canDrag)
    {
        _canDrag = canDrag;
    }

    /// <summary>
    /// 드래그 가능한 아이템에 wiggle 연출 켜기/끄기.
    /// </summary>
    public void SetWiggleActive(bool active)
    {
        wiggleEnabled = active;

        StopAllCoroutines();

        if (wiggleEnabled)
        {
            if (iconRect != null && _originalScale == Vector3.zero)
                _originalScale = iconRect.localScale;

            StartCoroutine(WiggleRoutine());
        }
        else
        {
            if (iconRect != null)
                iconRect.localScale = _originalScale;
        }
    }

    // --- Hover 처리 ---

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isHover = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isHover = false;
    }

    private void Update()
    {
        if (iconRect == null) return;
        Vector3 targetScale = _originalScale;

        // Hover 시 살짝 키우기
        if (_isHover)
        {
            targetScale = _originalScale * hoverScale;
        }

        iconRect.localScale = Vector3.Lerp(
            iconRect.localScale,
            targetScale,
            Time.deltaTime * hoverSpeed
        );
    }

    private IEnumerator WiggleRoutine()
    {
        if (iconRect == null)
            yield break;

        float t = 0f;
        while (true)
        {
            t += Time.deltaTime * wiggleSpeed;
            float s = 1f + Mathf.Sin(t) * wiggleAmplitude;
            iconRect.localScale = _originalScale * s;
          //  Debug.Log("  iconRect.localScale : " + iconRect.localScale);
            yield return null;
        }
    }

    // --- Drag 처리 ---

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!_canDrag || iconRect == null)
            return;

        _isDragging = true;

        _originalAnchoredPos = iconRect.anchoredPosition;
        _originalParent = iconRect.parent;

        // 슬롯에 남길 고스트 알파 올리기
        if (backgroundImage != null)
        {
            var c = backgroundImage.color;
            c.a = 0.3f;
            backgroundImage.color = c;
        }

        // 드래그 중에는 Canvas 최상단으로
        if (_cachedCanvas != null)
        {
            iconRect.SetParent(_cachedCanvas.transform, true);
        }

        _dragHandler?.OnInventoryDragBegin(this, eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_canDrag || !_isDragging || iconRect == null)
            return;

        Vector2 pos;
        if (_cachedCanvas != null && _cachedCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _cachedCanvas.transform as RectTransform,
                eventData.position,
                _cachedCanvas.worldCamera,
                out pos
            );
            iconRect.localPosition = pos;
        }
        else
        {
            iconRect.position = eventData.position;
        }

        _dragHandler?.OnInventoryDragging(this, eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!_canDrag || !_isDragging)
            return;

        _isDragging = false;

        _dragHandler?.OnInventoryDragEnd(this, eventData);

        // 기본값: 실패 가정 → 슬롯으로 복귀
        // 성공 드롭이면 Step 쪽에서 따로 HideIconKeepGhost 호출해주면 됨
        ReturnToSlot();
    }

    /// <summary>
    /// 드랍 실패 시, 슬롯 원위치로 되돌릴 때 사용.
    /// </summary>
    public void ReturnToSlot()
    {
        if (iconRect == null)
            return;

        if (_originalParent != null)
            iconRect.SetParent(_originalParent, true);

        iconRect.anchoredPosition = _originalAnchoredPos;

        // 배경 고스트도 원상복귀
        if (backgroundImage != null)
        {
            var c = backgroundImage.color;
            c.a = 0f;
            backgroundImage.color = c;
        }
    }

    /// <summary>
    /// 드롭 성공 시, 슬롯에는 반투명 배경만 남기고 아이콘만 숨길 때 사용.
    /// </summary>
   /* public void HideIconKeepGhost()
    {
        if (iconImage != null)
            iconImage.gameObject.SetActive(false);

        if (backgroundImage != null)
        {
            var c = backgroundImage.color;
            c.a = 0.3f;
            backgroundImage.color = c;
        }
    }*/
}

/// <summary>
/// 인벤토리 드래그를 받기 위한 인터페이스.
/// </summary>
public interface IStepInventoryDragHandler
{
    void OnInventoryDragBegin(StepInventoryItem item, PointerEventData eventData);
    void OnInventoryDragging(StepInventoryItem item, PointerEventData eventData);
    void OnInventoryDragEnd(StepInventoryItem item, PointerEventData eventData);
}
