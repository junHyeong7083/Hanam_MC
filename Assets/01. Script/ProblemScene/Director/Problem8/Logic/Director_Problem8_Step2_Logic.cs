using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Director / Problem8 / Step2 로직 베이스
/// - "5단계 스토리보드 채우기" (캐러셀 + 재사용 드래그 프록시)
/// - 좌/우 버튼으로 카드 탐색, 가운데 카드를 드래그하여 올바른 슬롯에 배치
/// </summary>
public abstract class Director_Problem8_Step2_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    [Serializable]
    public class SceneCardItem
    {
        public string id;               // DB 저장용 ID
        public int textId;              // CSV textId (카드 텍스트)
        public Sprite cardSprite;       // 카드 이미지
        public int correctSlotIndex;    // 올바른 슬롯 인덱스 (0~4)
    }

    [Serializable]
    public class SlotItem
    {
        public int slotIndex;
        public GameObject slotRoot;
        public GameObject emptyState;
        public GameObject filledState;
        public RectTransform dropArea;
    }

    [Serializable]
    private class CardPlacementDto
    {
        public string cardId;
        public int slotIndex;
        public bool isCorrect;
        public float placedAtSeconds;
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    // 캐러셀
    protected abstract Button PrevButton { get; }
    protected abstract Button NextButton { get; }
    protected abstract Image CardDisplayImage { get; }
    protected abstract CanvasGroup CardDisplayCanvasGroup { get; }

    // 드래그 프록시
    protected abstract RectTransform DragProxy { get; }
    protected abstract Image DragProxyImage { get; }
    protected abstract Canvas DragCanvas { get; }

    // 카드/슬롯 데이터
    protected abstract SceneCardItem[] SceneCards { get; }
    protected abstract SlotItem[] Slots { get; }

    // 가이드 텍스트
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId_Main { get; }
    protected abstract int GuideTextId_Fail { get; }
    protected abstract int GuideTextId_Success { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    #endregion

    #region Virtual Config

    protected virtual float ReturnDuration => 0.3f;
    protected virtual float GhostAlpha => 0.5f;

    #endregion

    // 내부 상태
    private List<int> _unplacedIndices;
    private int _currentCarouselIndex;
    private Dictionary<int, SceneCardItem> _slotToCard;
    private List<CardPlacementDto> _placements;
    private float _stepStartTime;
    private bool _isComplete;

    // 드래그 상태
    private bool _isDragging;
    private SceneCardItem _draggingCard;
    private Coroutine _snapBackRoutine;
    private bool _interactionLocked = true;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        var cards = SceneCards;
        int count = cards?.Length ?? 0;

        _unplacedIndices = new List<int>(count);
        for (int i = 0; i < count; i++)
            _unplacedIndices.Add(i);

        _currentCarouselIndex = 0;
        _slotToCard = new Dictionary<int, SceneCardItem>();
        _placements = new List<CardPlacementDto>();
        _stepStartTime = Time.time;
        _isComplete = false;
        _isDragging = false;
        _draggingCard = null;

        // UI 초기화
        InitSlots();
        SetupCarouselButtons();
        SetupDragHandler();

        if (DragProxy != null)
            DragProxy.gameObject.SetActive(false);

        UpdateCarouselDisplay();

        if (GuideText != null && GuideTextId_Main > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Main);

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;

        if (_snapBackRoutine != null)
        {
            StopCoroutine(_snapBackRoutine);
            _snapBackRoutine = null;
        }

        RemoveCarouselButtons();
        RemoveDragHandler();
    }

    // =========================
    // 초기 설정
    // =========================

    private void InitSlots()
    {
        var slots = Slots;
        if (slots == null) return;

        foreach (var slot in slots)
        {
            if (slot == null) continue;
            if (slot.emptyState != null) slot.emptyState.SetActive(true);
            if (slot.filledState != null) slot.filledState.SetActive(false);
        }
    }

    private void SetupCarouselButtons()
    {
        var prev = PrevButton;
        if (prev != null)
        {
            prev.onClick.RemoveAllListeners();
            prev.onClick.AddListener(OnPrevClicked);
        }

        var next = NextButton;
        if (next != null)
        {
            next.onClick.RemoveAllListeners();
            next.onClick.AddListener(OnNextClicked);
        }
    }

    private void RemoveCarouselButtons()
    {
        var prev = PrevButton;
        if (prev != null) prev.onClick.RemoveAllListeners();

        var next = NextButton;
        if (next != null) next.onClick.RemoveAllListeners();
    }

    private void SetupDragHandler()
    {
        var displayImg = CardDisplayImage;
        if (displayImg == null) return;

        var go = displayImg.gameObject;
        var trigger = go.GetComponent<EventTrigger>();
        if (trigger == null)
            trigger = go.AddComponent<EventTrigger>();

        trigger.triggers ??= new List<EventTrigger.Entry>();
        trigger.triggers.Clear();

        // BeginDrag
        var beginEntry = new EventTrigger.Entry { eventID = EventTriggerType.BeginDrag };
        beginEntry.callback.AddListener(data => OnBeginDrag((PointerEventData)data));
        trigger.triggers.Add(beginEntry);

        // Drag
        var dragEntry = new EventTrigger.Entry { eventID = EventTriggerType.Drag };
        dragEntry.callback.AddListener(data => OnDrag((PointerEventData)data));
        trigger.triggers.Add(dragEntry);

        // EndDrag
        var endEntry = new EventTrigger.Entry { eventID = EventTriggerType.EndDrag };
        endEntry.callback.AddListener(data => OnEndDrag((PointerEventData)data));
        trigger.triggers.Add(endEntry);
    }

    private void RemoveDragHandler()
    {
        var displayImg = CardDisplayImage;
        if (displayImg == null) return;

        var trigger = displayImg.gameObject.GetComponent<EventTrigger>();
        if (trigger != null)
            trigger.triggers.Clear();
    }

    // =========================
    // 캐러셀 네비게이션
    // =========================

    private void UpdateCarouselDisplay()
    {
        if (_unplacedIndices == null || _unplacedIndices.Count == 0) return;

        // 인덱스 보정
        if (_currentCarouselIndex >= _unplacedIndices.Count)
            _currentCarouselIndex = 0;
        if (_currentCarouselIndex < 0)
            _currentCarouselIndex = _unplacedIndices.Count - 1;

        var cards = SceneCards;
        if (cards == null) return;

        int cardIndex = _unplacedIndices[_currentCarouselIndex];
        var card = cards[cardIndex];

        if (CardDisplayImage != null && card.cardSprite != null)
            CardDisplayImage.sprite = card.cardSprite;

        if (CardDisplayCanvasGroup != null)
            CardDisplayCanvasGroup.alpha = 1f;

        // 버튼 상태
        bool hasMultiple = _unplacedIndices.Count > 1;
        if (PrevButton != null) PrevButton.gameObject.SetActive(hasMultiple);
        if (NextButton != null) NextButton.gameObject.SetActive(hasMultiple);
    }

    private void OnPrevClicked()
    {
        if (_interactionLocked) return;
        if (_isDragging || _isComplete) return;

        _currentCarouselIndex--;
        if (_currentCarouselIndex < 0)
            _currentCarouselIndex = _unplacedIndices.Count - 1;

        UpdateCarouselDisplay();
    }

    private void OnNextClicked()
    {
        if (_interactionLocked) return;
        if (_isDragging || _isComplete) return;

        _currentCarouselIndex++;
        if (_currentCarouselIndex >= _unplacedIndices.Count)
            _currentCarouselIndex = 0;

        UpdateCarouselDisplay();
    }

    // =========================
    // 드래그 핸들러
    // =========================

    private void OnBeginDrag(PointerEventData eventData)
    {
        if (_interactionLocked) return;
        if (_isComplete || _isDragging) return;
        if (_unplacedIndices == null || _unplacedIndices.Count == 0) return;

        var cards = SceneCards;
        if (cards == null) return;

        int cardIndex = _unplacedIndices[_currentCarouselIndex];
        _draggingCard = cards[cardIndex];
        _isDragging = true;

        // 고스트 표시 (알파 0.5)
        if (CardDisplayCanvasGroup != null)
            CardDisplayCanvasGroup.alpha = GhostAlpha;

        // 프록시 활성화
        var proxy = DragProxy;
        if (proxy != null)
        {
            proxy.gameObject.SetActive(true);

            if (DragProxyImage != null && _draggingCard.cardSprite != null)
                DragProxyImage.sprite = _draggingCard.cardSprite;

            var dragCanvas = DragCanvas;
            if (dragCanvas != null)
            {
                proxy.SetParent(dragCanvas.transform, true);
                proxy.SetAsLastSibling();
            }

            // 초기 위치: CardDisplay 위치
            if (CardDisplayImage != null)
                proxy.position = CardDisplayImage.rectTransform.position;
        }
    }

    private void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        var proxy = DragProxy;
        if (proxy == null) return;

        RectTransformUtility.ScreenPointToWorldPointInRectangle(
            proxy,
            eventData.position,
            eventData.pressEventCamera,
            out var worldPos
        );

        proxy.position = worldPos;
    }

    private void OnEndDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        _isDragging = false;

        var targetSlot = FindSlotUnderPointer(eventData);

        if (targetSlot != null && _draggingCard != null)
        {
            if (_draggingCard.correctSlotIndex == targetSlot.slotIndex)
            {
                PlaceCard(_draggingCard, targetSlot);
            }
            else
            {
                OnDropFailed();
            }
        }
        else
        {
            OnDropFailed();
        }

        _draggingCard = null;
    }

    // =========================
    // 슬롯 검출
    // =========================

    private SlotItem FindSlotUnderPointer(PointerEventData eventData)
    {
        var slots = Slots;
        if (slots == null) return null;

        // EventSystem Raycast로 포인터 아래 모든 UI 검출
        var rayResults = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, rayResults);

        foreach (var result in rayResults)
        {
            var hitGo = result.gameObject;
            foreach (var slot in slots)
            {
                if (slot == null) continue;
                if (_slotToCard.ContainsKey(slot.slotIndex)) continue;

                // emptyState 또는 그 자식에 hit
                if (slot.emptyState != null && slot.emptyState.activeInHierarchy)
                {
                    if (hitGo == slot.emptyState ||
                        hitGo.transform.IsChildOf(slot.emptyState.transform))
                        return slot;
                }

                // slotRoot 또는 그 자식에 hit (폴백)
                if (slot.slotRoot != null)
                {
                    if (hitGo == slot.slotRoot ||
                        hitGo.transform.IsChildOf(slot.slotRoot.transform))
                        return slot;
                }
            }
        }

        return null;
    }

    // =========================
    // 카드 배치
    // =========================

    private void PlaceCard(SceneCardItem card, SlotItem slot)
    {
        // DB 기록
        _placements.Add(new CardPlacementDto
        {
            cardId = card.id,
            slotIndex = slot.slotIndex,
            isCorrect = true,
            placedAtSeconds = Time.time - _stepStartTime
        });

        _slotToCard[slot.slotIndex] = card;

        // 슬롯 UI 업데이트
        if (slot.emptyState != null) slot.emptyState.SetActive(false);
        if (slot.filledState != null) slot.filledState.SetActive(true);

        // 프록시 숨기기
        if (DragProxy != null)
            DragProxy.gameObject.SetActive(false);

        // unplaced 리스트에서 제거
        int cardArrayIndex = Array.IndexOf(SceneCards, card);
        _unplacedIndices.Remove(cardArrayIndex);

        // 캐러셀 인덱스 보정
        if (_unplacedIndices.Count > 0)
        {
            if (_currentCarouselIndex >= _unplacedIndices.Count)
                _currentCarouselIndex = 0;

            UpdateCarouselDisplay();
        }

        // 모든 카드 배치 완료?
        if (_unplacedIndices.Count == 0)
        {
            OnAllPlaced();
        }
    }

    private void OnDropFailed()
    {
        // snap-back 애니메이션
        if (_snapBackRoutine != null)
            StopCoroutine(_snapBackRoutine);
        _snapBackRoutine = StartCoroutine(SnapBackProxy());

        // TTS 재생
        if (GuideTextId_Fail > 0)
            SoundManager.Instance.PlayTTS(GuideTextId_Fail);
    }

    private IEnumerator SnapBackProxy()
    {
        var proxy = DragProxy;
        if (proxy == null) yield break;

        Vector3 targetPos = CardDisplayImage != null
            ? CardDisplayImage.rectTransform.position
            : proxy.position;

        Vector3 startPos = proxy.position;
        float elapsed = 0f;

        while (elapsed < ReturnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / ReturnDuration;
            t = 1f - Mathf.Pow(1f - t, 3f); // ease-out cubic

            proxy.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        proxy.position = targetPos;
        proxy.gameObject.SetActive(false);

        // 고스트 복원
        if (CardDisplayCanvasGroup != null)
            CardDisplayCanvasGroup.alpha = 1f;

        _snapBackRoutine = null;
    }

    // =========================
    // 완료
    // =========================

    private void OnAllPlaced()
    {
        _isComplete = true;

        // 캐러셀 숨기기
        if (CardDisplayImage != null)
            CardDisplayImage.gameObject.SetActive(false);
        if (PrevButton != null) PrevButton.gameObject.SetActive(false);
        if (NextButton != null) NextButton.gameObject.SetActive(false);

        // 가이드 텍스트
        if (GuideText != null && GuideTextId_Success > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Success);

        // 완료 처리
        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();

        SaveAttempt(_placements);
    }
}
