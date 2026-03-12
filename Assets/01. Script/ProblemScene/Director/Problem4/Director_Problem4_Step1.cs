using UnityEngine;

/// <summary>
/// Director_Problem4_Step1 - 문제4 스텝1의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 드롭 박스, 인트로 애니메이션, 완료 게이트를 바인딩한다.
///         Problem2~9 공통 Step1 로직(Director_Problem2_Step1_Logic)을 상속한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제4 / 스텝1 (도입부 - 아이템 드래그앤드롭)
/// 【부모 클래스】 Director_Problem2_Step1_Logic → ProblemStepBase
/// </summary>
public class Director_Problem4_Step1 : Director_Problem2_Step1_Logic
{
    [Header("Drop Box 영역")]
    [SerializeField] private UIDropBoxArea dropBoxArea;

    [Header("인트로 애니메이션")]
    [SerializeField] private RectTransform leftEnterRoot;
    [SerializeField] private RectTransform rightEnterRoot;
    [SerializeField] private float introDuration = 0.5f;
    [SerializeField] private float leftStartOffsetX = -300f;
    [SerializeField] private float rightStartOffsetX = 300f;
    [SerializeField] private float introDelay = 0.1f;

    [Header("완료 게이트")]
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
