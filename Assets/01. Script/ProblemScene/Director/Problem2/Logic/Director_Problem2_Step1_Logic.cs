using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Director / Problem2 / Step1 ���� ���� ���̽�.
/// - ��� �ڽ� + �巡�� ������ + ��Ʈ�� �ִϸ��̼� + ����Ʈ ó��.
/// - ���� ���� ��ũ��Ʈ(Director_Problem2_Step1)��
///   ���⼭ �䱸�ϴ� ������Ƽ�鸸 SerializeField�� ���ε��ؼ� �Ѱ��ش�.
/// </summary>
public abstract class Director_Problem2_Step1_Logic : ProblemStepBase
{
    // ====== �ڽĿ��� ������ ������Ƽ�� ======

    [Header("Drop Box ���� (���� ������Ʈ)")]
    protected abstract UIDropBoxArea DropBoxArea { get; }

    [Header("Items")]
    protected abstract Director_Problem2_DragItem[] DragItems { get; }

    [Header("UI After Drop")]
    protected abstract GameObject ResultPanelRoot { get; }

    [Header("Icon Images")]
    protected abstract Image IconImageBackground { get; }
    protected abstract Image IconImage { get; }

    // ===== ���� �ִϸ��̼ǿ� ��Ʈ =====
    [Header("Intro Animation Roots")]
    protected abstract RectTransform LeftEnterRoot { get; }
    protected abstract RectTransform RightEnterRoot { get; }

    [Header("Intro Animation Settings")]
    protected abstract float IntroDuration { get; }
    protected abstract float LeftStartOffsetX { get; }
    protected abstract float RightStartOffsetX { get; }
    protected abstract float IntroDelay { get; }

    [Header("�Ϸ� ����Ʈ (Next ��ư��)")]
    protected abstract StepCompletionGate CompletionGate { get; }

    // ===== ���� ĳ�� =====
    private bool _leftInit;
    private bool _rightInit;
    private Vector2 _leftBasePos;
    private Vector2 _rightBasePos;
    private CanvasGroup _leftCg;
    private CanvasGroup _rightCg;

    // ���� ��ӵ� ������(���õ� ���)
    private Director_Problem2_DragItem _selectedItem;
    // �� �� ��� �����ߴ��� (����Ʈ �ߺ� ȣ�� ����)
    private bool _isCompleted;

    // =========================================
    // ProblemStepBase ����
    // =========================================
    protected override void OnStepEnter()
    {
        Debug.Log("[Step1] OnStepEnter 호출됨");
        InitState();
        StartCoroutine(PlayIntroAnimationRoutine());
    }

    protected override void OnStepExit()
    {
        // �ʿ��ϸ� ���⼭ ���� ����
    }

    // =========================================
    // �ʱ�ȭ / ��Ʈ�� �ִϸ��̼�
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

        Debug.Log($"[Step1] InitState - dropBoxArea={dropBoxArea != null}, dragItems={dragItems?.Length ?? 0}개");

        // 드롭 박스 초기화
        if (dropBoxArea != null)
            dropBoxArea.ResetVisual();
        else
            Debug.LogWarning("[Step1] dropBoxArea가 null! 인스펙터에서 할당하세요.");

        // 결과 패널 숨기기
        if (resultPanelRoot != null)
            resultPanelRoot.SetActive(false);

        // 드래그 아이템 초기화
        if (dragItems != null && dragItems.Length > 0)
        {
            foreach (var item in dragItems)
            {
                if (item != null)
                {
                    item.ResetToOriginalState();
                    item.SetStepController(this);
                    Debug.Log($"[Step1] SetStepController 호출: {item.name}");
                }
            }
        }
        else
        {
            Debug.LogWarning("[Step1] dragItems가 null이거나 비어있음! 인스펙터에서 할당하세요.");
        }

        // ���� �ִϸ��̼ǿ� ��Ʈ �ʱ� ��ġ/���� ����
        InitIntroRoot(LeftEnterRoot, ref _leftInit, ref _leftBasePos, LeftStartOffsetX, ref _leftCg);
        InitIntroRoot(RightEnterRoot, ref _rightInit, ref _rightBasePos, RightStartOffsetX, ref _rightCg);

        // ������/��� �ʱ� ����
        if (iconBg != null)
            iconBg.gameObject.SetActive(true);

        if (icon != null)
            icon.gameObject.SetActive(true);

        // �Ϸ� ����Ʈ �ʱ�ȭ (��� ���� 1���̸� �Ϸ�)
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
    //   DragItem ���� �ݹ����� �Ҹ��� API
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
        if (dropBoxArea == null)
        {
            Debug.LogWarning("[Step1] NotifyDragging: dropBoxArea가 null!");
            return;
        }

        dropBoxArea.UpdateHighlight(eventData);
    }

    public void NotifyDragEnd(Director_Problem2_DragItem item, PointerEventData eventData)
    {
        var dropBoxArea = DropBoxArea;
        if (dropBoxArea == null)
        {
            Debug.LogWarning("[Step1] NotifyDragEnd: dropBoxArea가 null!");
            return;
        }

        dropBoxArea.SetOutlineVisible(false);

        bool isOver = dropBoxArea.IsPointerOver(eventData);
        Debug.Log($"[Step1] NotifyDragEnd - isOver={isOver}, position={eventData.position}");

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
