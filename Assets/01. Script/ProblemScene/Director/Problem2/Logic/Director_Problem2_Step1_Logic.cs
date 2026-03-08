using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Director / Problem2~9 / Step1 공통 로직.
/// - DialogueSequencer 대사 → 인벤토리 드래그 → 드롭 → 완료 대사.
/// </summary>
public abstract class Director_Problem2_Step1_Logic : ProblemStepBase
{
    [Header("Drop Box 영역")]
    protected abstract UIDropBoxArea DropBoxArea { get; }

    [Header("UI After Drop")]
    protected abstract GameObject ResultPanelRoot { get; }

    [Header("Intro Animation Roots")]
    protected abstract RectTransform LeftEnterRoot { get; }
    protected abstract RectTransform RightEnterRoot { get; }

    [Header("Intro Animation Settings")]
    protected abstract float IntroDuration { get; }
    protected abstract float LeftStartOffsetX { get; }
    protected abstract float RightStartOffsetX { get; }
    protected abstract float IntroDelay { get; }

    [Header("완료 게이트 (Next 버튼용)")]
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    [Header("Inventory (Step1 전용)")]
    [SerializeField] private StepInventory stepInventory;
    [SerializeField] private GameObject hanamBox;

    private bool _interactionLocked = true;

    // ===== 인트로 캐시 =====
    private bool _leftInit;
    private bool _rightInit;
    private Vector2 _leftBasePos;
    private Vector2 _rightBasePos;
    private CanvasGroup _leftCg;
    private CanvasGroup _rightCg;

    private bool _isCompleted;

    // =========================================
    // ProblemStepBase 구현
    // =========================================
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
    private void InitState()
    {
        _isCompleted = false;

        var dropBoxArea = DropBoxArea;
        var resultPanelRoot = ResultPanelRoot;
        var gate = CompletionGate;

        // 드롭 박스 초기화
        if (dropBoxArea != null)
            dropBoxArea.ResetVisual();

        // 결과 패널 숨기기
        if (resultPanelRoot != null)
            resultPanelRoot.SetActive(false);

        // 인벤토리 초기화
        if (stepInventory != null)
        {
            stepInventory.gameObject.SetActive(false);

            if (stepInventory.slots != null)
            {
                foreach (var slot in stepInventory.slots)
                {
                    if (slot.itemComponent != null)
                    {
                        slot.itemComponent.draggable = slot.draggableThisStep;
                        slot.itemComponent.SetLocked(!slot.draggableThisStep);
                    }
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

    private void OnInventoryItemDropped(StepInventoryItem item)
    {
        item.ResetIconPosition();

        // 인벤토리 숨기고 hanamBox 다시 표시
        if (stepInventory != null)
            stepInventory.gameObject.SetActive(false);
        if (hanamBox != null)
            hanamBox.SetActive(true);

        var resultPanelRoot = ResultPanelRoot;
        if (resultPanelRoot != null)
            resultPanelRoot.SetActive(true);

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
