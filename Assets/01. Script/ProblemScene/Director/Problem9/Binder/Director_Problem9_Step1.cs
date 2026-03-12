using UnityEngine;

/// <summary>
/// Director_Problem9_Step1 - 문제9 스텝1 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 드롭 박스, 인트로 애니메이션, 완료 게이트의 UI 참조를 바인딩한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층. Director_Problem2_Step1_Logic을 상속하여 공통 로직 재사용.
/// 【문제/스텝】 Director 테마 > 문제9 > 스텝1 (인트로/도입 - 드롭 박스 + 좌우 슬라이드)
/// 【부모 클래스】 Director_Problem2_Step1_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem9 Step1 GameObject에 부착
/// 【흐름】 스텝 진입 → 좌/우 패널 슬라이드 인트로 → 드롭 박스에 아이템 드롭 → 완료
/// </summary>
public class Director_Problem9_Step1 : Director_Problem2_Step1_Logic
{
    [Header("Drop Box 영역")]
    [SerializeField] private UIDropBoxArea dropBoxArea;            // 아이템 드롭 감지 영역

    [Header("인트로 애니메이션")]
    [SerializeField] private RectTransform leftEnterRoot;          // 좌측 슬라이드 인 패널
    [SerializeField] private RectTransform rightEnterRoot;         // 우측 슬라이드 인 패널
    [SerializeField] private float introDuration = 0.5f;           // 슬라이드 시간
    [SerializeField] private float leftStartOffsetX = -300f;       // 좌측 시작 오프셋
    [SerializeField] private float rightStartOffsetX = 300f;       // 우측 시작 오프셋
    [SerializeField] private float introDelay = 0.1f;              // 인트로 지연

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;    // 스텝 완료 게이트

    protected override UIDropBoxArea DropBoxArea => dropBoxArea;
    protected override RectTransform LeftEnterRoot => leftEnterRoot;
    protected override RectTransform RightEnterRoot => rightEnterRoot;
    protected override float IntroDuration => introDuration;
    protected override float LeftStartOffsetX => leftStartOffsetX;
    protected override float RightStartOffsetX => rightStartOffsetX;
    protected override float IntroDelay => introDelay;
    protected override StepCompletionGate CompletionGate => completionGate;
}
