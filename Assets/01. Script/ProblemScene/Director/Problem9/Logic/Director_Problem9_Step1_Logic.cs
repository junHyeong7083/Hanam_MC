using UnityEngine;

/// <summary>
/// Director / Problem9 / Step1 로직 베이스
/// - DB에서 대본 보유 확인 후 자동 활성화
/// - 활성화 완료 시 introRoot 숨기고 DB 저장
/// </summary>
public abstract class Director_Problem9_Step1_Logic : InventoryDropTargetStepBase
{
    protected abstract RectTransform ConflictVisualRoot { get; }
    protected abstract GameObject IntroRoot { get; }
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    protected override RectTransform TargetVisualRoot => ConflictVisualRoot;
    protected override GameObject InstructionRoot => null;
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    protected override float ActivateScale => 1.1f;
    protected override float ActivateDuration => 0.5f;
    protected override float DelayBeforeComplete => 1.0f;

    protected override void OnStepEnterExtra()
    {
        if (IntroRoot != null) IntroRoot.SetActive(true);
    }

    protected override void OnActivateComplete()
    {
        if (IntroRoot != null) IntroRoot.SetActive(false);

        SaveAttempt(new
        {
            action = "script_dropped",
            targetItem = "conflict_icon"
        });
    }
}
