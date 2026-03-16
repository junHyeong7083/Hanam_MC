using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Director_Problem2_Step1_Logic - 문제2~9 공통 스텝1 비즈니스 로직 베이스 클래스.
///
/// 【역할】 인트로 대사 재생 → 인벤토리에서 아이템 드래그 → 드롭 박스에 놓기 → 완료 대사.
///         이 공통 로직을 Problem2~9의 Step1이 모두 상속하여 사용한다.
///         인트로 애니메이션(좌/우 슬라이드인), DB 기반 인벤토리 잠금/해제,
///         드래그 콜백 처리, 드롭 완료 시 UI 전환 등을 수행한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 측. DropBoxArea, IntroDuration 등 추상 프로퍼티.
/// 【문제/스텝】 Director 테마 / 문제2~9 / 스텝1 (도입부 - 아이템 드래그앤드롭)
/// 【부모 클래스】 ProblemStepBase → OnStepEnter()/OnStepExit()
/// 【참조하는 곳】 Director_Problem2_Step1, Director_Problem3_Step1, Director_Problem4_Step1,
///               Director_Problem5_Step1 등 (Binder 자식 클래스들)
/// 【참조되는 곳】 DialogueSequencer (대사), StepInventory/StepInventoryItem (인벤토리 시스템),
///               UIDropBoxArea (드롭 영역), StepCompletionGate (완료 판정),
///               DataService (DB 인벤토리 조회)
/// 【흐름】 스텝 진입 → 인트로 애니메이션 → enter 대사 → 대사 완료 → 인벤토리 표시 →
///         사용자 드래그 → 드롭 박스에 놓기 → 인벤토리 숨김 → completed 대사 → 다음 스텝
/// </summary>
public abstract class Director_Problem2_Step1_Logic : ProblemStepBase
{
    #region Abstract Properties

    [Header("Drop Box 영역")]
    protected abstract UIDropBoxArea DropBoxArea { get; }            // 아이템을 드롭할 수 있는 영역

    [Header("Intro Animation Roots")]
    protected abstract RectTransform LeftEnterRoot { get; }          // 왼쪽에서 슬라이드인 할 루트
    protected abstract RectTransform RightEnterRoot { get; }         // 오른쪽에서 슬라이드인 할 루트

    [Header("Intro Animation Settings")]
    protected abstract float IntroDuration { get; }                  // 인트로 애니메이션 시간
    protected abstract float LeftStartOffsetX { get; }               // 왼쪽 시작 오프셋 (음수=왼쪽)
    protected abstract float RightStartOffsetX { get; }              // 오른쪽 시작 오프셋 (양수=오른쪽)
    protected abstract float IntroDelay { get; }                     // 인트로 시작 전 지연 시간

    [Header("완료 게이트 (Next 버튼용)")]
    protected abstract StepCompletionGate CompletionGate { get; }    // 드롭 완료 시 다음 스텝 진행

    #endregion

    #region Virtual Config

    // ===== 드래그 상태 텍스트 (옵션, 자식에서 override 가능) =====
    protected virtual Text DragStateText => null;                    // 드래그 전/후 상태 표시 텍스트
    protected virtual int BeforeDragTextId => 0;                     // 드래그 전 텍스트 ID
    protected virtual int AfterDragTextId => 0;                      // 드래그 후 텍스트 ID
    protected virtual Color AfterDragTextColor => Color.white;       // 드래그 후 텍스트 색상

    #endregion

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;    // 대사 시퀀서 (enter/completed)

    [Header("Inventory (Step1 전용)")]
    [SerializeField] private StepInventory stepInventory;            // 스텝 전용 인벤토리 UI
    [SerializeField] private GameObject hanamBox;                    // 하남이 대사 박스 (대사 중 표시, 인벤토리와 교대)

    [Header("드롭 후 전환 (옵션 - 할당 시에만 동작)")]
    [SerializeField] private GameObject hideAfterDrop;               // 드롭 완료 후 숨길 오브젝트
    [SerializeField] private GameObject showAfterDrop;               // 드롭 완료 후 표시할 오브젝트

    /// <summary>대사 재생 중 상호작용 잠금 플래그</summary>
    private bool _interactionLocked = true;

    // ===== 인트로 애니메이션 캐시 =====
    private bool _leftInit;           // 왼쪽 루트 초기화 여부
    private bool _rightInit;          // 오른쪽 루트 초기화 여부
    private Vector2 _leftBasePos;     // 왼쪽 루트 기본 위치
    private Vector2 _rightBasePos;    // 오른쪽 루트 기본 위치
    private CanvasGroup _leftCg;      // 왼쪽 루트 CanvasGroup (페이드용)
    private CanvasGroup _rightCg;     // 오른쪽 루트 CanvasGroup (페이드용)

    /// <summary>드롭 완료 여부 (중복 처리 방지)</summary>
    private bool _isCompleted;

    // =========================================
    // ProblemStepBase 구현
    // =========================================

    /// <summary>
    /// 스텝 진입 시 호출. 상태 초기화, 인트로 애니메이션 시작,
    /// 대사 시퀀서 이벤트 바인딩을 수행한다.
    /// </summary>
    protected override void OnStepEnter()
    {
        Debug.Log("[Step1] OnStepEnter 호출됨");
        InitState();

        _interactionLocked = true;

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterSequenceDone += OnEnterSequenceDone;
        else
            _interactionLocked = false;

        StartCoroutine(PlayIntroAnimationRoutine());
    }

    /// <summary>
    /// enter 대사 시퀀스 완료 콜백. 하남박스를 숨기고 인벤토리를 표시하며,
    /// 드래그 콜백을 설정하고 드래그 전 텍스트를 표시한다.
    /// </summary>
    private void OnEnterSequenceDone()
    {
        _interactionLocked = false;

        // hanamBox 숨기고 인벤토리 표시
        if (hanamBox != null)
            hanamBox.SetActive(false);

        if (stepInventory != null)
        {
            stepInventory.gameObject.SetActive(true);
            SetupInventoryDragCallbacks();
        }

        // 드래그 전 텍스트 설정 (옵션)
        if (DragStateText != null && BeforeDragTextId > 0)
            DragStateText.text = ProblemRuntime.L(BeforeDragTextId);
    }

    protected override void OnStepExit()
    {
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterSequenceDone -= OnEnterSequenceDone;

        CleanupInventoryDragCallbacks();
        _interactionLocked = true;
    }

    // =========================================
    // 초기화
    // =========================================

    /// <summary>
    /// 전체 상태 초기화. 드롭 박스 리셋, 인벤토리 아이템 잠금/해제 설정,
    /// 하남박스 표시, 인트로 루트 위치/투명 설정, 완료 게이트 리셋을 수행한다.
    /// </summary>
    private void InitState()
    {
        _isCompleted = false;

        var dropBoxArea = DropBoxArea;
        var gate = CompletionGate;

        // 드롭 박스 초기화
        if (dropBoxArea != null)
            dropBoxArea.ResetVisual();

        // 인벤토리 초기화
        if (stepInventory != null)
        {
            stepInventory.gameObject.SetActive(false);

            // DB에서 획득한 아이템 목록 조회
            var ownedItemIds = LoadOwnedItemIds();

            if (stepInventory.slots != null)
            {
                foreach (var slot in stepInventory.slots)
                {
                    if (slot.itemComponent == null) continue;

                    // 이번 스텝에서 드래그 가능한 아이템 설정
                    slot.itemComponent.draggable = slot.draggableThisStep;

                    // DB에 있거나 이번 스텝 드래그 대상이면 잠금 해제
                    bool owned = ProblemSession.DemoMode
                        || (ownedItemIds != null
                            && !string.IsNullOrEmpty(slot.itemId)
                            && ownedItemIds.Contains(slot.itemId));

                    slot.itemComponent.SetLocked(!slot.draggableThisStep && !owned);
                }
            }
        }

        // hanamBox 초기 표시
        if (hanamBox != null)
            hanamBox.SetActive(true);

        // 인트로 애니메이션용 루트 초기 위치/투명 설정
        InitIntroRoot(LeftEnterRoot, ref _leftInit, ref _leftBasePos, LeftStartOffsetX, ref _leftCg);
        InitIntroRoot(RightEnterRoot, ref _rightInit, ref _rightBasePos, RightStartOffsetX, ref _rightCg);

        // 완료 게이트 초기화 (목표 1개 드롭)
        if (gate != null)
            gate.ResetGate(1);
    }

    // =========================================
    // DB 인벤토리 조회
    // =========================================

    /// <summary>
    /// DB에서 현재 사용자가 소유한 아이템 ID 목록을 조회한다.
    /// DataService → RewardRepository → GetInventory 경로로 조회.
    /// </summary>
    /// <returns>소유한 아이템 ID 집합. 실패 시 null.</returns>
    private HashSet<string> LoadOwnedItemIds()
    {
        var ds = DataService.Instance;
        if (ds == null || ds.Reward == null) return null;

        var session = SessionManager.Instance;
        var user = session?.CurrentUser;
        if (user == null || string.IsNullOrEmpty(user.Email)) return null;

        var result = ds.Reward.GetInventory(user.Email);
        if (!result.Ok || result.Value == null) return null;

        var set = new HashSet<string>();
        foreach (var item in result.Value)
        {
            if (item != null && !string.IsNullOrEmpty(item.ItemId))
                set.Add(item.ItemId);
        }

        return set;
    }

    // =========================================
    // 인벤토리 드래그 콜백
    // =========================================
    private void SetupInventoryDragCallbacks()
    {
        if (stepInventory?.slots == null) return;

        foreach (var slot in stepInventory.slots)
        {
            if (slot.itemComponent == null) continue;

            slot.itemComponent.OnItemDragBegin += OnInventoryDragBegin;
            slot.itemComponent.OnItemDragging += OnInventoryDragging;
            slot.itemComponent.OnItemDragEnd += OnInventoryDragEnd;
        }
    }

    private void CleanupInventoryDragCallbacks()
    {
        if (stepInventory?.slots == null) return;

        foreach (var slot in stepInventory.slots)
        {
            if (slot.itemComponent == null) continue;

            slot.itemComponent.OnItemDragBegin -= OnInventoryDragBegin;
            slot.itemComponent.OnItemDragging -= OnInventoryDragging;
            slot.itemComponent.OnItemDragEnd -= OnInventoryDragEnd;
        }
    }

    private void OnInventoryDragBegin(StepInventoryItem item)
    {
        var dropBoxArea = DropBoxArea;
        if (dropBoxArea != null)
            dropBoxArea.SetOutlineVisible(true);
    }

    private void OnInventoryDragging(StepInventoryItem item, PointerEventData eventData)
    {
        var dropBoxArea = DropBoxArea;
        if (dropBoxArea != null)
            dropBoxArea.UpdateHighlight(eventData);
    }

    private void OnInventoryDragEnd(StepInventoryItem item, PointerEventData eventData)
    {
        if (_interactionLocked) return;

        var dropBoxArea = DropBoxArea;
        if (dropBoxArea == null) return;

        dropBoxArea.SetOutlineVisible(false);

        if (dropBoxArea.IsPointerOver(eventData))
        {
            OnInventoryItemDropped(item);
        }
        else
        {
            item.ResetIconPosition();
        }
    }

    /// <summary>
    /// 아이템이 드롭 박스에 놓였을 때 호출. 인벤토리 숨김, 하남박스 복귀,
    /// UI 전환, 텍스트 변경, 완료 게이트 처리, completed 대사 표시를 수행한다.
    /// </summary>
    private void OnInventoryItemDropped(StepInventoryItem item)
    {
        item.ResetIconPosition();

        // 인벤토리 숨기고 hanamBox 다시 표시
        if (stepInventory != null)
            stepInventory.gameObject.SetActive(false);
        if (hanamBox != null)
            hanamBox.SetActive(true);

        // 드롭 후 전환
        if (hideAfterDrop != null)
            hideAfterDrop.SetActive(false);
        if (showAfterDrop != null)
            showAfterDrop.SetActive(true);

        // 드래그 후 텍스트 변경 (옵션)
        if (DragStateText != null && AfterDragTextId > 0)
        {
            DragStateText.text = ProblemRuntime.L(AfterDragTextId);
            DragStateText.color = AfterDragTextColor;
        }

        if (!_isCompleted)
        {
            _isCompleted = true;

            var gate = CompletionGate;
            if (gate != null)
                gate.MarkOneDone();

            if (dialogueSequencer != null)
                dialogueSequencer.ShowCompletedText();
        }
    }

    // =========================================
    // 인트로 애니메이션
    // =========================================
    /// <summary>
    /// 인트로 애니메이션용 루트의 초기 위치와 CanvasGroup을 설정한다.
    /// 기본 위치를 저장하고, 오프셋 적용 + 투명으로 시작 상태를 만든다.
    /// </summary>
    private void InitIntroRoot(
        RectTransform root,
        ref bool inited,
        ref Vector2 basePos,
        float offsetX,
        ref CanvasGroup cg)
    {
        if (root == null) return;

        if (!inited)
        {
            basePos = root.anchoredPosition;
            inited = true;

            cg = root.GetComponent<CanvasGroup>();
            if (cg == null)
                cg = root.gameObject.AddComponent<CanvasGroup>();
        }

        root.anchoredPosition = basePos + new Vector2(offsetX, 0f);
        if (cg != null)
            cg.alpha = 0f;
    }

    /// <summary>
    /// 좌/우 루트를 오프셋에서 기본 위치로 슬라이드인하면서 페이드인하는 인트로 애니메이션 코루틴.
    /// SmoothStep 보간으로 부드러운 등장 효과를 제공한다.
    /// </summary>
    private IEnumerator PlayIntroAnimationRoutine()
    {
        var leftRoot = LeftEnterRoot;
        var rightRoot = RightEnterRoot;

        if (IntroDelay > 0f)
            yield return new WaitForSeconds(IntroDelay);

        float t = 0f;

        Vector2 leftStartPos = Vector2.zero;
        Vector2 leftEndPos = Vector2.zero;
        Vector2 rightStartPos = Vector2.zero;
        Vector2 rightEndPos = Vector2.zero;

        if (leftRoot != null && _leftInit)
        {
            leftEndPos = _leftBasePos;
            leftStartPos = _leftBasePos + new Vector2(LeftStartOffsetX, 0f);
            leftRoot.anchoredPosition = leftStartPos;
            if (_leftCg != null) _leftCg.alpha = 0f;
        }

        if (rightRoot != null && _rightInit)
        {
            rightEndPos = _rightBasePos;
            rightStartPos = _rightBasePos + new Vector2(RightStartOffsetX, 0f);
            rightRoot.anchoredPosition = rightStartPos;
            if (_rightCg != null) _rightCg.alpha = 0f;
        }

        float duration = Mathf.Max(0.0001f, IntroDuration);

        while (t < duration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, x);

            if (leftRoot != null && _leftInit)
            {
                leftRoot.anchoredPosition = Vector2.Lerp(leftStartPos, leftEndPos, eased);
                if (_leftCg != null) _leftCg.alpha = x;
            }

            if (rightRoot != null && _rightInit)
            {
                rightRoot.anchoredPosition = Vector2.Lerp(rightStartPos, rightEndPos, eased);
                if (_rightCg != null) _rightCg.alpha = x;
            }

            yield return null;
        }

        if (leftRoot != null && _leftInit)
        {
            leftRoot.anchoredPosition = _leftBasePos;
            if (_leftCg != null) _leftCg.alpha = 1f;
        }

        if (rightRoot != null && _rightInit)
        {
            rightRoot.anchoredPosition = _rightBasePos;
            if (_rightCg != null) _rightCg.alpha = 1f;
        }
    }
}
