// Director_Problem4_Step1.cs

using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Director / Problem4 / Step1
/// - 인벤토리에서 '가위' 드래그하여 필름에 드롭하는 단계
/// - 이펙트는 EffectController에 위임
/// </summary>
public class Director_Problem4_Step1 : InventoryDropTargetStepBase
{
    [Header("필름 드롭 타겟")]
    [SerializeField] private RectTransform filmDropArea;
    [SerializeField] private float dropRadius = 200f;

    [Header("완료 게이트 (옵션)")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem4_Step1_EffectController effectController;

    [Header("완료 후 딜레이")]
    [SerializeField] private float delayBeforeComplete = 1.5f;

    // === 베이스로 넘겨줄 프로퍼티들 ===
    protected override RectTransform DropTargetRect => filmDropArea;
    protected override GameObject DropIndicatorRoot => null; // EffectController가 관리
    protected override RectTransform TargetVisualRoot => null; // EffectController가 관리
    protected override GameObject InstructionRoot => null; // EffectController가 관리
    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float DropRadius => dropRadius;
    protected override float ActivateScale => 1f; // 사용 안 함
    protected override float ActivateDuration => 0f; // 사용 안 함
    protected override float DelayBeforeComplete => delayBeforeComplete;

    // ====== 이펙트 컨트롤러 사용하도록 Override ======

    protected override void OnStepEnterExtra()
    {
        // 이펙트 컨트롤러 리셋
        if (effectController != null)
        {
            effectController.ResetForNextStep();
        }
    }

    protected override void OnInventoryDragBeginExtra(StepInventoryItem item, PointerEventData eventData)
    {
        // 이펙트 컨트롤러로 드롭 인디케이터 표시
        if (effectController != null)
        {
            effectController.ShowDropIndicator();
        }
    }

    protected override void OnDropSuccess(StepInventoryItem item, PointerEventData eventData)
    {
        // 이펙트 컨트롤러로 활성화 시퀀스 시작
        if (effectController != null)
        {
            effectController.HideDropIndicator();
            effectController.PlayActivateSequence(OnActivateSequenceComplete);
        }
        else
        {
            // 폴백: 베이스 클래스 로직 사용
            base.OnDropSuccess(item, eventData);
        }
    }

    private void OnActivateSequenceComplete()
    {
        // 딜레이 후 완료 처리
        StartCoroutine(DelayedComplete());
    }

    private IEnumerator DelayedComplete()
    {
        if (delayBeforeComplete > 0f)
            yield return new WaitForSeconds(delayBeforeComplete);

        OnDropComplete();

        var gate = CompletionGate;
        if (gate != null)
            gate.MarkOneDone();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        // 이펙트 컨트롤러 정리
        if (effectController != null)
        {
            effectController.HideDropIndicator();
            effectController.HideSparkle();
        }
    }
}
