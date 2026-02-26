using UnityEngine;

/// <summary>
/// Director / Problem6 / Step1 로직 베이스
/// - DB에서 휴식용 의자 보유 확인 후 자동 활성화
/// - 박스 내 아이콘이 빈 상태에서 의자 아이콘으로 바뀜
/// </summary>
public abstract class Director_Problem6_Step1_Logic : InventoryDropTargetStepBase
{
    // 파생 클래스에서 넘겨줄 UI 프로퍼티
    protected abstract RectTransform ChairTargetVisualRoot { get; }
    protected abstract GameObject InstructionRootObject { get; }
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    protected abstract GameObject ChairPlacedIconRoot { get; }
    protected abstract GameObject GlowImage { get; }
    protected abstract GameObject SparkleImage { get; }

    // InventoryDropTargetStepBase 속성 연결
    protected override RectTransform TargetVisualRoot => ChairTargetVisualRoot;
    protected override GameObject InstructionRoot => InstructionRootObject;
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    protected override float ActivateScale => 1.02f;
    protected override float ActivateDuration => 0.5f;
    protected override float DelayBeforeComplete => 2.5f;

    protected override void OnStepEnterExtra()
    {
        if (ChairPlacedIconRoot != null)
            ChairPlacedIconRoot.SetActive(false);
        if (GlowImage != null)
            GlowImage.SetActive(false);
        if (SparkleImage != null)
            SparkleImage.SetActive(false);
    }

    protected override void OnActivateComplete()
    {
        if (ChairPlacedIconRoot != null)
            ChairPlacedIconRoot.SetActive(true);
        if (GlowImage != null)
            GlowImage.SetActive(true);
        if (SparkleImage != null)
            SparkleImage.SetActive(true);
    }
}
