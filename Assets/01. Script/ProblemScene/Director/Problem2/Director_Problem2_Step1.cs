using UnityEngine;

/// <summary>
/// Director_Problem2_Step1 - 문제2 스텝1의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 드롭 박스, 인트로 애니메이션 루트, 완료 게이트 등을 바인딩한다.
///         실제 드래그앤드롭/인벤토리/대사 로직은 부모(Director_Problem2_Step1_Logic)에 있다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제2 / 스텝1 (도입부 - 아이템 드래그앤드롭)
/// 【부모 클래스】 Director_Problem2_Step1_Logic → ProblemStepBase
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
