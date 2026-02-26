using UnityEngine;

/// <summary>
/// Director / Problem7 / Step1 로직 베이스
/// - DB에서 격려의 메가폰 보유 확인 후 자동 활성화
/// </summary>
public abstract class Director_Problem7_Step1_Logic : InventoryDropTargetStepBase
{
    protected abstract RectTransform MegaphoneTargetVisualRoot { get; }
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    protected override RectTransform TargetVisualRoot => MegaphoneTargetVisualRoot;
    protected override GameObject InstructionRoot => null;
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    protected override float ActivateScale => 1.05f;
    protected override float ActivateDuration => 0.5f;
    protected override float DelayBeforeComplete => 2.0f;
}
