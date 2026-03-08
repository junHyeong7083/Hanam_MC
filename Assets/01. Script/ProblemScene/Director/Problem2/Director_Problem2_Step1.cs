using UnityEngine;

/// <summary>
/// Director / Problem2 / Step1
/// - 인스펙터에서 UI 오브젝트만 바인딩.
/// - 실제 동작은 Director_Problem2_Step1_Logic(부모)에서 처리.
/// </summary>
public class Director_Problem2_Step1 : Director_Problem2_Step1_Logic
{
    [Header("Drop Box 영역 (공통 컴포넌트)")]
    [SerializeField] private UIDropBoxArea dropBoxArea;

    [Header("Intro Animation Roots")]
    [SerializeField] private RectTransform leftEnterRoot;
    [SerializeField] private RectTransform rightEnterRoot;

    [Header("Intro Animation Settings")]
    [SerializeField] private float introDuration = 0.5f;
    [SerializeField] private float leftStartOffsetX = -300f;
    [SerializeField] private float rightStartOffsetX = 300f;
    [SerializeField] private float introDelay = 0.1f;

    [Header("완료 게이트 (Next 버튼용)")]
    [SerializeField] private StepCompletionGate completionGate;

    protected override UIDropBoxArea DropBoxArea => dropBoxArea;
    protected override RectTransform LeftEnterRoot => leftEnterRoot;
    protected override RectTransform RightEnterRoot => rightEnterRoot;
    protected override float IntroDuration => introDuration;
    protected override float LeftStartOffsetX => leftStartOffsetX;
    protected override float RightStartOffsetX => rightStartOffsetX;
    protected override float IntroDelay => introDelay;
    protected override StepCompletionGate CompletionGate => completionGate;
}
