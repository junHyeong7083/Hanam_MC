using UnityEngine;

/// <summary>
/// Director / Problem6 / Step1
/// - �ν����Ϳ��� UI ������ ��� �ִ� ����.
/// - ���� ������ Director_Problem6_Step1_Logic(�θ�)�� ����.
/// </summary>
public class Director_Problem6_Step1 : Director_Problem6_Step1_Logic
{
    [Header("���� ��� Ÿ�� ���� (�� ���� �ڽ�)")]
    [SerializeField] private RectTransform emptySpaceRect;        // TS�� emptySpaceRef

    [Header("��� �ε������� (�巡�� �� �׵θ� �ڽ�)")]
    [SerializeField] private GameObject dropIndicatorRoot;

    [Header("Ȱ��ȭ ����� ���־� ��Ʈ (������ ƨ�� ���)")]
    [SerializeField] private RectTransform chairTargetVisualRoot; // �ڽ� ��ü or ���� ī��

    [Header("�ȳ� �ؽ�Ʈ ��Ʈ")]
    [SerializeField] private GameObject instructionRoot;          // "���ڸ� �巡���ؼ� �÷��ּ���"

    [Header("드롭 완료 시 등장")]
    [SerializeField] private GameObject chairPlacedIconRoot;      // 의자 아이콘
    [SerializeField] private GameObject glowImage;                // 글로우 이미지
    [SerializeField] private GameObject sparkleImage;             // 스파클 이미지

    [Header("�Ϸ� ����Ʈ")]
    [SerializeField] private StepCompletionGate completionGate;

    // ----- �θ� �߻� ������Ƽ ���� -----
    protected override RectTransform ChairDropTargetRect => emptySpaceRect;
    protected override GameObject ChairDropIndicatorRoot => dropIndicatorRoot;
    protected override RectTransform ChairTargetVisualRoot => chairTargetVisualRoot;
    protected override GameObject InstructionRootObject => instructionRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
    protected override GameObject ChairPlacedIconRoot => chairPlacedIconRoot;
    protected override GameObject GlowImage => glowImage;
    protected override GameObject SparkleImage => sparkleImage;
}
