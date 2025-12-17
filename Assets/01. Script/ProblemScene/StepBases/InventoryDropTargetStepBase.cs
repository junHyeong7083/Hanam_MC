using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Base class for inventory drag-drop to specific UI target.
/// - ProblemStepBase + IStepInventoryDragHandler implementation
/// - When dropped within radius, item is consumed and target scale animation + Gate completion.
/// - Actual references (target Rect, etc.) are provided via protected properties from derived classes.
/// </summary>
public abstract class InventoryDropTargetStepBase : ProblemStepBase, IStepInventoryDragHandler
{
    // Abstract/Virtual properties to be provided by derived classes
    #region Property
    /// <summary>Drop target RectTransform for distance check</summary>
    protected abstract RectTransform DropTargetRect { get; }

    /// <summary>Indicator shown during drag (highlight, etc.)</summary>
    protected abstract GameObject DropIndicatorRoot { get; }

    /// <summary>Visual root for scale animation on drop success</summary>
    protected abstract RectTransform TargetVisualRoot { get; }

    /// <summary>Instruction text/root (hidden on success)</summary>
    protected abstract GameObject InstructionRoot { get; }

    /// <summary>Completion gate (optional, can be null)</summary>
    protected abstract StepCompletionGate CompletionGate { get; }

    /// <summary>Drop acceptance radius from target center (default 200)</summary>
    protected virtual float DropRadius => 200f;

    /// <summary>Max scale ratio during activation animation</summary>
    protected virtual float ActivateScale => 1.05f;

    /// <summary>Activation animation duration</summary>
    protected virtual float ActivateDuration => 0.6f;

    /// <summary>Delay before Gate completion after animation</summary>
    protected virtual float DelayBeforeComplete => 1.5f;

    // Internal state
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
        // Add cleanup logic if needed
    }

    /// <summary>
    /// Override in derived class for additional initialization.
    /// Called after base state reset.
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
    // IStepInventoryDragHandler Implementation
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
    /// Additional processing on drag begin (override if needed)
    /// </summary>
    protected virtual void OnInventoryDragBeginExtra(StepInventoryItem item, PointerEventData eventData) { }

    /// <summary>
    /// Additional processing during drag (e.g., glow effect)
    /// </summary>
    protected virtual void OnInventoryDraggingExtra(StepInventoryItem item, PointerEventData eventData) { }

    /// <summary>
    /// Check if pointer is inside drop area (default: distance from center)
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
    /// Called on successful drop.
    /// - Consume item visual effect
    /// - Start activation coroutine
    /// </summary>
    protected virtual void OnDropSuccess(StepInventoryItem item, PointerEventData eventData)
    {
        // 드롭 성공 후 다시 드래그 못하게 비활성화
        item?.SetDraggable(false);

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
    /// Target activation scale animation (default: slight grow then shrink)
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
    /// Called after Gate completion for additional work.
    /// (e.g., sound effect, additional UI)
    /// </summary>
    protected virtual void OnDropComplete() { }
}
