using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Director / Problem8 / Step2 로직 베이스
/// - "5단계 스토리보드 채우기"
/// - 5개의 장면 카드를 드래그 앤 드롭으로 올바른 슬롯에 배치
/// - 카드는 Ghost(알파0.5, 원래자리) + Draggable(알파1, 드래그됨) 2개 레이어로 구성
/// </summary>
public abstract class Director_Problem8_Step2_Logic : ProblemStepBase
{
    // =========================
    // 장면 카드 데이터 구조
    // =========================

    /// <summary>
    /// 장면 카드 아이템
    /// UI 구조: CardRoot > Ghost (CanvasGroup+Image) + Draggable (CanvasGroup+Image)
    /// </summary>
    [Serializable]
    public class SceneCardItem
    {
        [Header("기본 정보")]
        public string id;               // DB 저장용 ID (예: "step1", "step2")
        public string text;             // 표시 텍스트 (예: "문제 정의", "대안 탐색")
        public int correctSlotIndex;    // 올바른 슬롯 인덱스 (0~4)

        [Header("UI 참조")]
        public GameObject cardRoot;

        [Tooltip("원래 자리의 반투명 카드 (고스트 레이어)")]
        public CanvasGroup ghostCanvasGroup;

        [Tooltip("드래그되는 실제 카드 (불투명 레이어)")]
        public CanvasGroup draggableCanvasGroup;
    }

    /// <summary>
    /// 슬롯 아이템
    /// UI 구조: SlotRoot > EmptyState + FilledState
    /// </summary>
    [Serializable]
    public class SlotItem
    {
        [Header("슬롯 인덱스 (0~4)")]
        public int slotIndex;

        [Header("슬롯 루트")]
        public GameObject slotRoot;

        [Header("빈 상태 UI")]
        public GameObject emptyState;        // 빈 상태일 때 보이는 UI

        [Header("채워진 상태 UI")]
        public GameObject filledState;       // 카드가 채워졌을 때 보이는 UI
        public Text filledText;              // 카드 텍스트 복사용
        public Image filledImage;            // 카드 이미지 복사용

        [Header("드롭 감지용 영역")]
        public RectTransform dropArea;       // 드래그 종료 시 이 영역 위인지 체크
    }

    /// <summary>
    /// 카드 배치 로그 DTO (DB 저장용)
    /// </summary>
    [Serializable]
    public class CardPlacementDto
    {
        public string cardId;            // 카드 ID
        public int slotIndex;            // 배치된 슬롯 인덱스
        public bool isCorrect;           // 정답 여부
        public float placedAtSeconds;    // 스텝 시작 후 경과 시간(초)
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    [Header("장면 카드들")]
    protected abstract SceneCardItem[] SceneCards { get; }

    [Header("슬롯들")]
    protected abstract SlotItem[] Slots { get; }

    [Header("카드 선택 영역 루트 (모든 카드 배치 시 숨김)")]
    protected abstract GameObject CardSelectionRoot { get; }

    [Header("드래그용 Canvas (최상위 렌더링)")]
    protected abstract Canvas DragCanvas { get; }

    [Header("완료 게이트")]
    protected abstract StepCompletionGate CompletionGateRef { get; }

    [Header("Step2 완료 Fill 연출용 UI")]
    protected abstract GameObject FillImageRoot { get; }
    protected abstract Image FillImage { get; }

    #endregion

    #region Virtual Config

    protected virtual float GhostAlpha => 0.5f;             // 드래그 중 Ghost 알파값
    protected virtual float ReturnDuration => 0.3f;         // 원위치 복귀 시간
    protected virtual float CompleteDelay => 5.0f;          // (이전용, 지금은 안 씀)
    protected virtual float FillDuration => 1.0f;           // 모든 슬롯 채운 뒤 FillImage 0→1까지 걸리는 시간(초)

    #endregion

    // 내부 상태
    private Dictionary<int, SceneCardItem> _slotToCard;
    private HashSet<string> _placedCardIds;
    private List<CardPlacementDto> _placements;
    private float _stepStartTime;
    private Coroutine _completeRoutine;
    private Coroutine _fillRoutine;
    private bool _isComplete;

    // 드래그 상태
    private SceneCardItem _draggingCard;
    private Vector3 _draggableOriginalPosition;
    private Transform _draggableOriginalParent;
    private int _draggableOriginalSiblingIndex;
    private bool _isDragging;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _slotToCard = new Dictionary<int, SceneCardItem>();
        _placedCardIds = new HashSet<string>();
        _placements = new List<CardPlacementDto>();
        _stepStartTime = Time.time;
        _isComplete = false;
        _isDragging = false;
        _draggingCard = null;

        var gate = CompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);

        // Step2 전용 Fill UI 초기화
        if (FillImageRoot != null)
            FillImageRoot.SetActive(false);
        if (FillImage != null)
            FillImage.fillAmount = 0f;

        SetupAllUI();
        SetupDragHandlers();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_completeRoutine != null)
        {
            StopCoroutine(_completeRoutine);
            _completeRoutine = null;
        }

        if (_fillRoutine != null)
        {
            StopCoroutine(_fillRoutine);
            _fillRoutine = null;
        }

        RemoveDragHandlers();
    }

    // =========================
    // 초기 설정
    // =========================

    private void SetupAllUI()
    {
        // 슬롯 초기화 (모두 빈 상태)
        var slots = Slots;
        if (slots != null)
        {
            foreach (var slot in slots)
            {
                if (slot == null) continue;

                if (slot.emptyState != null)
                    slot.emptyState.SetActive(true);

                if (slot.filledState != null)
                    slot.filledState.SetActive(false);

                if (slot.filledText != null)
                    slot.filledText.text = string.Empty;

                if (slot.filledImage != null)
                    slot.filledImage.sprite = null;
            }
        }

        // 카드 초기화
        var cards = SceneCards;
        if (cards != null)
        {
            foreach (var card in cards)
            {
                if (card == null) continue;

                if (card.cardRoot != null)
                    card.cardRoot.SetActive(true);

                if (card.ghostCanvasGroup != null)
                {
                    card.ghostCanvasGroup.alpha = 1f;
                    card.ghostCanvasGroup.blocksRaycasts = false;
                }

                if (card.draggableCanvasGroup != null)
                {
                    card.draggableCanvasGroup.alpha = 1f;
                    card.draggableCanvasGroup.blocksRaycasts = true;
                }
            }
        }

        // 카드 선택 영역 초기화
        if (CardSelectionRoot != null)
            CardSelectionRoot.SetActive(true);
    }

    // =========================
    // 드래그 핸들러 등록/해제
    // =========================

    private void SetupDragHandlers()
    {
        var cards = SceneCards;
        if (cards == null) return;

        foreach (var card in cards)
        {
            if (card?.draggableCanvasGroup == null) continue;

            var draggableRect = card.draggableCanvasGroup.GetComponent<RectTransform>();
            if (draggableRect == null) continue;

            var eventTrigger = draggableRect.gameObject.GetComponent<EventTrigger>();
            if (eventTrigger == null)
                eventTrigger = draggableRect.gameObject.AddComponent<EventTrigger>();

            eventTrigger.triggers ??= new List<EventTrigger.Entry>();
            eventTrigger.triggers.Clear();

            // BeginDrag
            var beginEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
            beginEntry.callback.AddListener(data =>
            {
                var ped = (PointerEventData)data;
                OnBeginDrag(card, ped);
            });
            eventTrigger.triggers.Add(beginEntry);

            // Drag
            var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
            dragEntry.callback.AddListener(data =>
            {
                var ped = (PointerEventData)data;
                OnDrag(card, ped);
            });
            eventTrigger.triggers.Add(dragEntry);

            // EndDrag
            var endEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
            endEntry.callback.AddListener(data =>
            {
                var ped = (PointerEventData)data;
                OnEndDrag(card, ped);
            });
            eventTrigger.triggers.Add(endEntry);
        }
    }

    private void RemoveDragHandlers()
    {
        var cards = SceneCards;
        if (cards == null) return;

        foreach (var card in cards)
        {
            if (card?.draggableCanvasGroup == null) continue;

            var draggableRect = card.draggableCanvasGroup.GetComponent<RectTransform>();
            if (draggableRect == null) continue;

            var eventTrigger = draggableRect.gameObject.GetComponent<EventTrigger>();
            if (eventTrigger != null)
                eventTrigger.triggers.Clear();
        }
    }

    // =========================
    // 드래그 앤 드롭 핸들러
    // =========================

    private void OnBeginDrag(SceneCardItem card, PointerEventData eventData)
    {
        if (_isComplete) return;
        if (card == null) return;
        if (_placedCardIds.Contains(card.id)) return;

        var draggableRect = card.draggableCanvasGroup?.GetComponent<RectTransform>();
        if (draggableRect == null) return;

        _isDragging = true;
        _draggingCard = card;

        _draggableOriginalPosition = draggableRect.position;
        _draggableOriginalParent = draggableRect.parent;
        _draggableOriginalSiblingIndex = draggableRect.GetSiblingIndex();

        var dragCanvas = DragCanvas;
        if (dragCanvas != null)
        {
            draggableRect.SetParent(dragCanvas.transform, true);
            draggableRect.SetAsLastSibling();
        }

        if (card.ghostCanvasGroup != null)
            card.ghostCanvasGroup.alpha = GhostAlpha;

        OnDragStartedVisual(card);
    }

    private void OnDrag(SceneCardItem card, PointerEventData eventData)
    {
        if (!_isDragging) return;
        if (card != _draggingCard) return;
        if (card.draggableCanvasGroup == null) return;

        var draggableRect = card.draggableCanvasGroup.GetComponent<RectTransform>();
        if (draggableRect == null) return;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            draggableRect,
            eventData.position,
            eventData.pressEventCamera,
            out var worldPos
        );

        draggableRect.position = worldPos;

        HighlightSlotUnderPointer(eventData);
    }

    private void OnEndDrag(SceneCardItem card, PointerEventData eventData)
    {
        if (!_isDragging) return;
        if (card != _draggingCard) return;

        _isDragging = false;

        ResetAllSlotHighlights();

        var targetSlot = FindSlotUnderPointer(eventData);
        if (targetSlot != null)
        {
            TryPlaceCardInSlot(card, targetSlot);
        }
        else
        {
            ReturnCardToOriginal(card);
        }

        _draggingCard = null;
    }

    // =========================
    // 슬롯 하이라이트
    // =========================

    private void HighlightSlotUnderPointer(PointerEventData eventData)
    {
        var slots = Slots;
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot?.dropArea == null) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                slot.dropArea,
                eventData.position,
                eventData.pressEventCamera))
            {
                OnSlotHighlight(slot, true);
            }
            else
            {
                OnSlotHighlight(slot, false);
            }
        }
    }

    private void ResetAllSlotHighlights()
    {
        var slots = Slots;
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot == null) continue;
            OnSlotHighlight(slot, false);
        }
    }

    private SlotItem FindSlotUnderPointer(PointerEventData eventData)
    {
        var slots = Slots;
        if (slots == null) return null;

        foreach (var slot in slots)
        {
            if (slot?.dropArea == null) continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(
                slot.dropArea,
                eventData.position,
                eventData.pressEventCamera))
            {
                return slot;
            }
        }

        return null;
    }

    // =========================
    // 카드 배치 / 되돌리기
    // =========================

    private void TryPlaceCardInSlot(SceneCardItem card, SlotItem slot)
    {
        if (card == null || slot == null) return;
        if (_placedCardIds.Contains(card.id)) return;

        // 정답 슬롯이 아니면 드롭 거부 → 원위치 복귀
        if (card.correctSlotIndex != slot.slotIndex)
        {
            OnDropFailedVisual(card);
            ReturnCardToOriginal(card);
            return;
        }

        var newPlacement = new CardPlacementDto
        {
            cardId = card.id,
            slotIndex = slot.slotIndex,
            isCorrect = true,
            placedAtSeconds = Time.time - _stepStartTime
        };
        _placements.Add(newPlacement);

        _slotToCard[slot.slotIndex] = card;
        _placedCardIds.Add(card.id);

        UpdateSlotUI(slot, card);

        HideCard(card);

        OnCardPlacedVisual(card, slot.slotIndex);

        CheckAllPlaced();
    }

    private void UpdateSlotUI(SlotItem slot, SceneCardItem card)
    {
        if (slot.emptyState != null) slot.emptyState.SetActive(false);
        if (slot.filledState != null) slot.filledState.SetActive(true);
        if (slot.filledText != null) slot.filledText.text = card.text;

        if (slot.filledImage != null && card.draggableCanvasGroup != null)
        {
            var draggableImage = card.draggableCanvasGroup.GetComponent<Image>();
            if (draggableImage != null)
                slot.filledImage.sprite = draggableImage.sprite;
        }
    }

    private void HideCard(SceneCardItem card)
    {
        if (card.ghostCanvasGroup != null)
            card.ghostCanvasGroup.alpha = 0f;

        if (card.draggableCanvasGroup != null)
        {
            var draggableRect = card.draggableCanvasGroup.GetComponent<RectTransform>();
            if (draggableRect != null)
            {
                draggableRect.SetParent(_draggableOriginalParent, true);
                draggableRect.gameObject.SetActive(false);
            }
        }
    }

    private void ReturnCardToOriginal(SceneCardItem card)
    {
        var draggableRect = card?.draggableCanvasGroup?.GetComponent<RectTransform>();
        if (draggableRect == null) return;

        if (card.ghostCanvasGroup != null)
            card.ghostCanvasGroup.alpha = 0f;

        draggableRect.SetParent(_draggableOriginalParent, true);
        draggableRect.SetSiblingIndex(_draggableOriginalSiblingIndex);

        StartCoroutine(AnimateReturn(draggableRect, _draggableOriginalPosition));
    }

    private IEnumerator AnimateReturn(RectTransform rect, Vector3 targetPos)
    {
        Vector3 startPos = rect.position;
        float elapsed = 0f;

        while (elapsed < ReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ReturnDuration;
            t = 1f - Mathf.Pow(1f - t, 3f);

            rect.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        rect.position = targetPos;
    }

    // =========================
    // 완료 체크 + Fill 애니메이션
    // =========================

    private void CheckAllPlaced()
    {
        var cards = SceneCards;
        if (cards == null) return;

        if (_placedCardIds.Count >= cards.Length)
        {
            _isComplete = true;

            if (CardSelectionRoot != null)
                CardSelectionRoot.SetActive(false);

            OnAllCardsPlacedVisual();

            // Fill 애니메이션 후 완료 처리
            if (_fillRoutine != null)
                StopCoroutine(_fillRoutine);
            _fillRoutine = StartCoroutine(FillAndComplete());
        }
    }

    private IEnumerator FillAndComplete()
    {
        var root = FillImageRoot;
        var img = FillImage;
        var gate = CompletionGateRef;

        // 1) Fill UI 켜기
        if (root != null)
            root.SetActive(true);

        // 2) FillAmount 0 → 1 애니메이션
        if (img != null)
        {
            img.fillAmount = 0f;

            float elapsed = 0f;
            while (elapsed < FillDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / FillDuration);
                img.fillAmount = t;
                yield return null;
            }

            img.fillAmount = 1f;
        }

        // 3) 게이트 완료 처리
        if (gate != null)
            gate.MarkOneDone();

        _fillRoutine = null;
    }

    // =========================
    // 시각 효과 (파생 클래스에서 override 가능)
    // =========================

    protected virtual void OnDragStartedVisual(SceneCardItem card)
    {
        // 드래그 시작 시 효과 (파생 클래스에서 override)
    }

    protected virtual void OnSlotHighlight(SlotItem slot, bool isHighlighted)
    {
        // 슬롯 하이라이트 효과 (파생 클래스에서 override)
    }

    protected virtual void OnCardPlacedVisual(SceneCardItem card, int slotIndex)
    {
        // 카드 배치 성공 시 효과 (파생 클래스에서 override)
    }

    protected virtual void OnDropFailedVisual(SceneCardItem card)
    {
        // 드롭 실패 시 효과 (파생 클래스에서 override)
    }

    protected virtual void OnAllCardsPlacedVisual()
    {
        // 모든 카드 배치 완료 시 효과 (파생 클래스에서 override)
    }
}
