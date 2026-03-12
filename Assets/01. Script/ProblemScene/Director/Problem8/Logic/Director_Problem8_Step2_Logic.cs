using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Director_Problem8_Step2_Logic - 문제8 스텝2 스토리보드 채우기 로직 (추상 클래스)
///
/// 【역할】 5장의 씬 카드를 캐러셀로 탐색하고, 드래그&드롭으로 올바른 슬롯에 배치하는 메인 활동.
///          좌/우 버튼으로 미배치 카드를 순환하며, 드래그 프록시를 통해 슬롯에 배치한다.
///          정답 슬롯에 배치 시 카드가 고정되고, 오답 시 snap-back 애니메이션으로 복귀한다.
///          모든 카드를 배치하면 완료 처리 및 DB 저장.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층. SerializeField는 Binder(Director_Problem8_Step2)에서 바인딩.
/// 【문제/스텝】 Director 테마 > 문제8 > 스텝2 (메인 활동 - 스토리보드 드래그&드롭)
/// 【부모 클래스】 ProblemStepBase
/// 【참조하는 곳】 Director_Problem8_Step2 (Binder)
/// 【참조되는 곳】 ProblemStepBase, DialogueSequencer, EventSystem (드래그 이벤트)
/// 【흐름】 스텝 진입 → 대화 재생 → 캐러셀로 카드 탐색 → 카드 드래그 → 슬롯에 드롭
///         → 정답: 카드 고정 + 슬롯 업데이트 / 오답: snap-back → 모두 배치 시 완료
/// </summary>
public abstract class Director_Problem8_Step2_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    /// <summary>씬 카드 한 장의 데이터 (ID, 텍스트, 스프라이트, 정답 슬롯 인덱스)</summary>
    [Serializable]
    public class SceneCardItem
    {
        public string id;               // DB 저장용 카드 식별자
        public int textId;              // CSV textId (카드에 표시할 텍스트)
        public Sprite cardSprite;       // 카드 이미지 스프라이트
        public int correctSlotIndex;    // 올바른 슬롯 인덱스 (0~4)
    }

    /// <summary>스토리보드 슬롯 한 칸의 UI 참조 (빈 상태/채워진 상태)</summary>
    [Serializable]
    public class SlotItem
    {
        public int slotIndex;           // 슬롯 번호 (0~4)
        public GameObject slotRoot;     // 슬롯 루트 오브젝트
        public GameObject emptyState;   // 비어있는 상태 UI
        public GameObject filledState;  // 카드가 배치된 상태 UI
        public RectTransform dropArea;  // 드롭 감지 영역
    }

    /// <summary>카드 배치 기록 DTO (DB 저장용)</summary>
    [Serializable]
    private class CardPlacementDto
    {
        public string cardId;           // 배치된 카드 ID
        public int slotIndex;           // 배치된 슬롯 인덱스
        public bool isCorrect;          // 정답 여부
        public float placedAtSeconds;   // 스텝 시작 후 배치 시간 (초)
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    // ----- 캐러셀 UI -----
    /// <summary>이전 카드 버튼</summary>
    protected abstract Button PrevButton { get; }
    /// <summary>다음 카드 버튼</summary>
    protected abstract Button NextButton { get; }
    /// <summary>현재 카드를 표시하는 이미지</summary>
    protected abstract Image CardDisplayImage { get; }
    /// <summary>카드 표시 영역의 CanvasGroup (드래그 시 고스트 알파 적용)</summary>
    protected abstract CanvasGroup CardDisplayCanvasGroup { get; }

    // ----- 드래그 프록시 -----
    /// <summary>드래그 중 카드를 따라다니는 프록시 RectTransform</summary>
    protected abstract RectTransform DragProxy { get; }
    /// <summary>드래그 프록시에 표시할 이미지</summary>
    protected abstract Image DragProxyImage { get; }
    /// <summary>드래그 프록시의 부모 Canvas (최상위 레이어에 표시하기 위함)</summary>
    protected abstract Canvas DragCanvas { get; }

    // ----- 카드/슬롯 데이터 -----
    /// <summary>씬 카드 배열 (5장)</summary>
    protected abstract SceneCardItem[] SceneCards { get; }
    /// <summary>스토리보드 슬롯 배열 (5칸)</summary>
    protected abstract SlotItem[] Slots { get; }

    // ----- 가이드 텍스트 -----
    /// <summary>가이드 텍스트 UI</summary>
    protected abstract Text GuideText { get; }
    /// <summary>메인 안내 텍스트의 CSV textId</summary>
    protected abstract int GuideTextId_Main { get; }
    /// <summary>오답(드롭 실패) 시 표시할 텍스트의 CSV textId</summary>
    protected abstract int GuideTextId_Fail { get; }
    /// <summary>모든 카드 배치 성공 시 표시할 텍스트의 CSV textId</summary>
    protected abstract int GuideTextId_Success { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 대사 시퀀서

    #endregion

    #region Virtual Config

    /// <summary>오답 시 snap-back 애니메이션 지속 시간 (초)</summary>
    protected virtual float ReturnDuration => 0.3f;
    /// <summary>드래그 시작 시 원본 카드의 고스트 알파값</summary>
    protected virtual float GhostAlpha => 0.5f;

    #endregion

    // ===== 내부 상태 =====
    private List<int> _unplacedIndices;                    // 아직 배치되지 않은 카드의 인덱스 목록
    private int _currentCarouselIndex;                     // 캐러셀에서 현재 표시 중인 위치
    private Dictionary<int, SceneCardItem> _slotToCard;    // 슬롯 인덱스 → 배치된 카드 매핑
    private List<CardPlacementDto> _placements;            // DB 저장용 배치 기록 리스트
    private float _stepStartTime;                          // 스텝 시작 시간 (배치 시간 계산용)
    private bool _isComplete;                              // 모든 카드 배치 완료 여부

    // ===== 드래그 상태 =====
    private bool _isDragging;                              // 현재 드래그 중인지
    private SceneCardItem _draggingCard;                   // 드래그 중인 카드 참조
    private Coroutine _snapBackRoutine;                    // snap-back 코루틴 핸들
    private bool _interactionLocked = true;                // 대화 재생 중 상호작용 잠금

    // =========================
    // ProblemStepBase 생명주기 구현
    // =========================

    /// <summary>
    /// 스텝 진입 시 호출. 미배치 카드 목록 초기화, 슬롯/캐러셀/드래그 핸들러 세팅.
    /// 대화 재생 완료 대기.
    /// </summary>
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

    /// <summary>대화 진입 완료 시 상호작용 잠금 해제.</summary>
    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    /// <summary>스텝 퇴장 시 호출. snap-back 코루틴 정지, 캐러셀/드래그 리스너 정리.</summary>
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

    /// <summary>모든 슬롯을 빈 상태(emptyState ON, filledState OFF)로 초기화한다.</summary>
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

    /// <summary>캐러셀 좌/우 버튼에 클릭 리스너를 등록한다.</summary>
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

    /// <summary>캐러셀 버튼의 리스너를 제거한다.</summary>
    private void RemoveCarouselButtons()
    {
        var prev = PrevButton;
        if (prev != null) prev.onClick.RemoveAllListeners();

        var next = NextButton;
        if (next != null) next.onClick.RemoveAllListeners();
    }

    /// <summary>카드 이미지에 EventTrigger를 추가하여 BeginDrag/Drag/EndDrag 이벤트를 수신한다.</summary>
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

    /// <summary>카드 이미지의 EventTrigger를 정리한다.</summary>
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

    /// <summary>
    /// 현재 캐러셀 인덱스의 카드를 화면에 표시한다.
    /// 카드가 1장만 남으면 좌/우 버튼을 숨긴다.
    /// </summary>
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

    /// <summary>이전 카드 버튼 클릭. 캐러셀 인덱스를 감소하여 이전 카드를 표시한다.</summary>
    private void OnPrevClicked()
    {
        if (_interactionLocked) return;
        if (_isDragging || _isComplete) return;

        _currentCarouselIndex--;
        if (_currentCarouselIndex < 0)
            _currentCarouselIndex = _unplacedIndices.Count - 1;

        UpdateCarouselDisplay();
    }

    /// <summary>다음 카드 버튼 클릭. 캐러셀 인덱스를 증가하여 다음 카드를 표시한다.</summary>
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

    /// <summary>드래그 시작. 원본 카드를 고스트(반투명)로 바꾸고 드래그 프록시를 활성화한다.</summary>
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

    /// <summary>드래그 중. 프록시를 포인터 위치로 이동시킨다.</summary>
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

    /// <summary>드래그 종료. 포인터 아래 슬롯을 검출하여 정답이면 배치, 아니면 snap-back.</summary>
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

    /// <summary>
    /// EventSystem Raycast로 포인터 아래의 빈 슬롯을 검출한다.
    /// 이미 카드가 배치된 슬롯은 제외한다.
    /// </summary>
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

    /// <summary>
    /// 카드를 올바른 슬롯에 배치한다. 슬롯 UI를 채운 상태로 전환하고,
    /// 미배치 목록에서 제거한 뒤, 모두 배치 완료 시 OnAllPlaced를 호출한다.
    /// </summary>
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

    /// <summary>오답 슬롯에 드롭 시 snap-back 애니메이션을 재생하고 실패 TTS를 재생한다.</summary>
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

    /// <summary>
    /// 드래그 프록시를 원래 카드 위치로 ease-out cubic 애니메이션으로 되돌린 뒤 숨긴다.
    /// </summary>
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

    /// <summary>
    /// 모든 카드 배치 완료. 캐러셀을 숨기고, 성공 가이드 텍스트를 표시하며,
    /// DialogueSequencer 완료 텍스트 표시 후 DB에 배치 기록을 저장한다.
    /// </summary>
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
