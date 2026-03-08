using UnityEngine;

/// <summary>
/// Director / Problem8 / Step1
/// - 대본을 책상 위에 드래그하여 드롭하는 스텝
/// </summary>
public class Director_Problem8_Step1 : Director_Problem2_Step1_Logic
{
    [Header("Drop Box 영역")]
    [SerializeField] private UIDropBoxArea dropBoxArea;

    [Header("드롭 후 결과 패널")]
    [SerializeField] private GameObject resultPanelRoot;

    [Header("인트로 애니메이션")]
    [SerializeField] private RectTransform leftEnterRoot;
    [SerializeField] private RectTransform rightEnterRoot;
    [SerializeField] private float introDuration = 0.5f;
    [SerializeField] private float leftStartOffsetX = -300f;
    [SerializeField] private float rightStartOffsetX = 300f;
    [SerializeField] private float introDelay = 0.1f;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    // === 베이스 프로퍼티 구현 ===
    protected override UIDropBoxArea DropBoxArea => dropBoxArea;
    protected override GameObject ResultPanelRoot => resultPanelRoot;
    protected override RectTransform LeftEnterRoot => leftEnterRoot;
    protected override RectTransform RightEnterRoot => rightEnterRoot;
    protected override float IntroDuration => introDuration;
    protected override float LeftStartOffsetX => leftStartOffsetX;
    protected override float RightStartOffsetX => rightStartOffsetX;
    protected override float IntroDelay => introDelay;
    protected override StepCompletionGate CompletionGate => completionGate;
}
