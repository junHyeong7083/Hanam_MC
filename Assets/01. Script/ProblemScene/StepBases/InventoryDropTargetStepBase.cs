using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 인벤토리 아이템을 끌어서 특정 UI 타겟 근처에 놓으면 성공 처리하는 공통 베이스.
/// - ProblemStepBase + IStepInventoryDragHandler 구현
/// - 반경(dragRadius) 안에 드롭되면 성공으로 보고, 간단한 scale 연출 후 Gate 완료.
/// - 실제 참조(타겟 Rect 등)는 파생 클래스에서 SerializeField로 들고 있다가
///   protected 프로퍼티로 넘겨주는 구조.
/// </summary>
public abstract class InventoryDropTargetStepBase : ProblemStepBase, IStepInventoryDragHandler
{
    // 파생 클래스에서 참조 넘겨줄 추상/가상 프로퍼티들
    #region Property
    /// <summary>드롭 성공 판정 기준이 될 타겟 RectTransform</summary>
    protected abstract RectTransform DropTargetRect { get; }

    /// <summary>드래그 중에 보여줄 인디케이터(하이라이트 등)</summary>
    protected abstract GameObject DropIndicatorRoot { get; }

    /// <summary>성공 시 scale 연출을 줄 비주얼 루트</summary>
    protected abstract RectTransform TargetVisualRoot { get; }

    /// <summary>처음에 보여줄 안내 텍스트/루트 (성공 시 숨김)</summary>
    protected abstract GameObject InstructionRoot { get; }

    /// <summary>완료 게이트 (없으면 null 허용)</summary>
    protected abstract StepCompletionGate CompletionGate { get; }

    /// <summary>타겟 중심으로부터 허용 반경 (기본값 200)</summary>
    protected virtual float DropRadius => 200f;

    /// <summary>활성화 연출 시 최대 스케일 배수</summary>
    protected virtual float ActivateScale => 1.05f;

    /// <summary>활성화 연출 시간</summary>
    protected virtual float ActivateDuration => 0.6f;

    /// <summary>연출이 끝난 뒤 Gate 완료까지 대기 시간</summary>
    protected virtual float DelayBeforeComplete => 1.5f;

    // 내부 상태
    private bool _activated;
    private bool _animPlaying;
    #endregion
    // ================================
    // ProblemStepBase
    // ================================
    protected override void OnStepEnter()
    {
        ResetBaseState();

        var gate = CompletionGate;
        if (gate != null)
            gate.ResetGate(1);

        OnStepEnterExtra();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();
        // 필요하면 나중에 공통 정리 추가
    }

    /// <summary>
    /// 파생 클래스에서 추가 초기화가 필요할 때 오버라이드.
    /// (기본 상태 리셋 이후 호출)
    /// </summary>
    protected virtual void OnStepEnterExtra() { }

    private void ResetBaseState()
    {
        _activated = false;
        _animPlaying = false;

        var indicator = DropIndicatorRoot;
        if (indicator != null)
            indicator.SetActive(false);

        var inst = InstructionRoot;
        if (inst != null)
            inst.SetActive(true);

        var visual = TargetVisualRoot;
        if (visual != null)
            visual.localScale = Vector3.one;
    }

    // ================================
    // IStepInventoryDragHandler 구현
    // ================================

    public void OnInventoryDragBegin(StepInventoryItem item, PointerEventData eventData)
    {
        if (_activated) return;
        if (item == null) return;
        if (!item.IsDraggableThisStep) return;

        var indicator = DropIndicatorRoot;
        if (indicator != null)
            indicator.SetActive(true);

        OnInventoryDragBeginExtra(item, eventData);
    }

    public void OnInventoryDragging(StepInventoryItem item, PointerEventData eventData)
    {
        OnInventoryDraggingExtra(item, eventData);
    }

    public void OnInventoryDragEnd(StepInventoryItem item, PointerEventData eventData)
    {
        var indicator = DropIndicatorRoot;
        if (indicator != null)
            indicator.SetActive(false);

        if (item == null || eventData == null)
        {
            item?.ReturnToSlot();
            return;
        }

        if (_activated)
        {
            item.ReturnToSlot();
            return;
        }

        var targetRect = DropTargetRect;
        if (targetRect == null)
        {
            item.ReturnToSlot();
            return;
        }

        bool success = IsPointerInsideDropArea(eventData);

        if (success)
        {
            OnDropSuccess(item, eventData);
        }
        else
        {
            item.ReturnToSlot();
        }
    }

    /// <summary>
    /// 드래그 시작 시 추가 처리 (파생 클래스에서 필요하면 사용)
    /// </summary>
    protected virtual void OnInventoryDragBeginExtra(StepInventoryItem item, PointerEventData eventData) { }

    /// <summary>
    /// 드래그 중 추가 처리 (보통 안 써도 됨)
    /// </summary>
    protected virtual void OnInventoryDraggingExtra(StepInventoryItem item, PointerEventData eventData) { }

    /// <summary>
    /// 포인터가 드롭 영역 안에 있는지 여부 (기본: 중심 기준 원형 반경)
    /// </summary>
    protected virtual bool IsPointerInsideDropArea(PointerEventData eventData)
    {
        var targetRect = DropTargetRect;
        if (targetRect == null || eventData == null)
            return false;

        var cam = eventData.pressEventCamera;
        Vector2 targetScreenPos = RectTransformUtility.WorldToScreenPoint(cam, targetRect.position);
        Vector2 dropPos = eventData.position;

        float dist = Vector2.Distance(targetScreenPos, dropPos);
        return dist <= DropRadius;
    }

    /// <summary>
    /// 드롭 성공 시 기본 처리.
    /// - 아이콘 숨기고 고스트 유지
    /// - 활성화 코루틴 실행
    /// </summary>
    protected virtual void OnDropSuccess(StepInventoryItem item, PointerEventData eventData)
    {
        //item.HideIconKeepGhost();
        StartCoroutine(HandleActivatedRoutine());
    }

    private IEnumerator HandleActivatedRoutine()
    {
        if (_activated || _animPlaying)
            yield break;

        _activated = true;
        _animPlaying = true;

        var inst = InstructionRoot;
        if (inst != null)
            inst.SetActive(false);

        yield return PlayActivateAnimation();

        _animPlaying = false;

        if (DelayBeforeComplete > 0f)
            yield return new WaitForSeconds(DelayBeforeComplete);

        OnDropComplete();

        var gate = CompletionGate;
        if (gate != null)
            gate.MarkOneDone();
    }

    /// <summary>
    /// 타겟 활성화 scale 애니메이션 (기본: 살짝 커졌다가 복귀)
    /// </summary>
    protected virtual IEnumerator PlayActivateAnimation()
    {
        var visual = TargetVisualRoot;
        if (visual == null || ActivateDuration <= 0f)
            yield break;

        float t = 0f;
        Vector3 baseScale = Vector3.one;

        while (t < ActivateDuration)
        {
            t += Time.deltaTime;
            float x = Mathf.Clamp01(t / ActivateDuration);
            float s = Mathf.Sin(x * Mathf.PI);
            float scale = Mathf.Lerp(1f, ActivateScale, s);

            visual.localScale = Vector3.one * scale;
            yield return null;
        }

        visual.localScale = baseScale;
    }

    /// <summary>
    /// Gate 완료 직전에 추가로 뭔가 하고 싶을 때 사용.
    /// (예: 효과음, 추가 UI 등)
    /// </summary>
    protected virtual void OnDropComplete() { }
}
