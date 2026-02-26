using UnityEngine;

/// <summary>
/// Director / Problem6 / Step1
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem6_Step1_Logic(부모)에 있음.
/// </summary>
public class Director_Problem6_Step1 : Director_Problem6_Step1_Logic
{
    [Header("활성화 연출용 비주얼 루트")]
    [SerializeField] private RectTransform chairTargetVisualRoot;

    [Header("안내 텍스트 루트")]
    [SerializeField] private GameObject instructionRoot;

    [Header("활성화 완료 시 등장")]
    [SerializeField] private GameObject chairPlacedIconRoot;
    [SerializeField] private GameObject glowImage;
    [SerializeField] private GameObject sparkleImage;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    protected override RectTransform ChairTargetVisualRoot => chairTargetVisualRoot;
    protected override GameObject InstructionRootObject => instructionRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
    protected override GameObject ChairPlacedIconRoot => chairPlacedIconRoot;
    protected override GameObject GlowImage => glowImage;
    protected override GameObject SparkleImage => sparkleImage;
}
