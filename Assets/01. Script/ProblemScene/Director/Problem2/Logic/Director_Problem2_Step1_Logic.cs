using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Director / Problem2 / Step1 공통 로직 베이스.
/// - 드롭 박스 + 드래그 아이템 + 인트로 애니메이션 + 게이트 처리.
/// - 실제 스텝 스크립트(Director_Problem2_Step1)는
///   여기서 요구하는 프로퍼티들만 SerializeField로 바인딩해서 넘겨준다.
/// </summary>
public abstract class Director_Problem2_Step1_Logic : ProblemStepBase
{
    // ====== 자식에서 주입할 프로퍼티들 ======

    [Header("Drop Box 영역 (공통 컴포넌트)")]
    protected abstract UIDropBoxArea DropBoxArea { get; }

    [Header("Items")]
    protected abstract Director_Problem2_DragItem[] DragItems { get; }

    [Header("UI After Drop")]
    protected abstract GameObject ResultPanelRoot { get; }

    [Header("Icon Images")]
    protected abstract Image IconImageBackground { get; }
    protected abstract Image IconImage { get; }

    // ===== 등장 애니메이션용 루트 =====
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

    // ===== 내부 캐시 =====
    private bool _leftInit;
    private bool _rightInit;
    private Vector2 _leftBasePos;
    private Vector2 _rightBasePos;
    private CanvasGroup _leftCg;
    private CanvasGroup _rightCg;

    // 최종 드롭된 아이템(선택된 장면)
    private Director_Problem2_DragItem _selectedItem;
    // 한 번 드롭 성공했는지 (게이트 중복 호출 방지)
    private bool _isCompleted;

    // =========================================
    // ProblemStepBase 구현
    // =========================================
    protected override void OnStepEnter()
    {
        InitState();
        StartCoroutine(PlayIntroAnimationRoutine());
    }

    protected override void OnStepExit()
    {
        // 필요하면 여기서 상태 정리
    }

    // =========================================
    // 초기화 / 인트로 애니메이션
    // =========================================

    private void InitState()
    {
        _selectedItem = null;
        _isCompleted = false;

        var dropBoxArea = DropBoxArea;
        var dragItems = DragItems;
        var resultPanelRoot = ResultPanelRoot;
        var iconBg = IconImageBackground;
        var icon = IconImage;
        var gate = CompletionGate;

        // 드롭 박스 외곽선/상태 초기화
        if (dropBoxArea != null)
            dropBoxArea.ResetVisual();

        // 결과 패널(설명 글 + 버튼들) 숨기기
        if (resultPanelRoot != null)
            resultPanelRoot.SetActive(false);

        // 아이템 원위치/비주얼 초기화
        if (dragItems != null)
        {
            foreach (var item in dragItems)
            {
                if (item != null)
                {
                    item.ResetToOriginalState();
                    item.SetStepController(this);
                }
            }
        }

        // 등장 애니메이션용 루트 초기 위치/알파 세팅
        InitIntroRoot(LeftEnterRoot, ref _leftInit, ref _leftBasePos, LeftStartOffsetX, ref _leftCg);
        InitIntroRoot(RightEnterRoot, ref _rightInit, ref _rightBasePos, RightStartOffsetX, ref _rightCg);

        // 아이콘/배경 초기 상태
        if (iconBg != null)
            iconBg.gameObject.SetActive(true);

        if (icon != null)
            icon.gameObject.SetActive(true);

        // 완료 게이트 초기화 (드롭 성공 1번이면 완료)
        if (gate != null)
            gate.ResetGate(1);
    }

    private void InitIntroRoot(
        RectTransform root,
        ref bool inited,
        ref Vector2 basePos,
        float offsetX,
        ref CanvasGroup cg
    )
    {
        if (root == null)
            return;

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

    // ====================================================
    //   DragItem 에서 콜백으로 불리는 API
    // ====================================================

    public void NotifyDragBegin(Director_Problem2_DragItem item)
    {
        var dropBoxArea = DropBoxArea;
        if (dropBoxArea != null)
            dropBoxArea.SetOutlineVisible(false);
    }

    public void NotifyDragging(Director_Problem2_DragItem item, PointerEventData eventData)
    {
        var dropBoxArea = DropBoxArea;
        if (dropBoxArea == null) return;

        dropBoxArea.UpdateHighlight(eventData);
    }

    public void NotifyDragEnd(Director_Problem2_DragItem item, PointerEventData eventData)
    {
        var dropBoxArea = DropBoxArea;
        if (dropBoxArea == null) return;

        dropBoxArea.SetOutlineVisible(false);

        bool isOver = dropBoxArea.IsPointerOver(eventData);
        if (isOver)
        {
            OnItemDroppedIntoBox(item);
        }
        else
        {
            item.ReturnToOriginalPosition();
            item.RestoreOriginalAlpha();
        }
    }

    private void OnItemDroppedIntoBox(Director_Problem2_DragItem item)
    {
        _selectedItem = item;

        var dropBoxArea = DropBoxArea;
        var resultPanelRoot = ResultPanelRoot;
        var iconImage = IconImage;
        var iconImageBg = IconImageBackground;
        var gate = CompletionGate;

        if (resultPanelRoot != null)
            resultPanelRoot.SetActive(true);

        if (dropBoxArea != null)
        {
            item.SnapToDropBoxCenter(dropBoxArea.transform as RectTransform);
        }

        if (iconImage != null)
            iconImage.gameObject.SetActive(false);

        if (iconImageBg != null)
            iconImageBg.gameObject.SetActive(true);

        if (!_isCompleted)
        {
            _isCompleted = true;
            if (gate != null)
                gate.MarkOneDone();
        }
    }
}
