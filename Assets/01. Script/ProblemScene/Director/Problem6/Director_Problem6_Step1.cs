using UnityEngine;

/// <summary>
/// Director_Problem6_Step1 - 문제6 스텝1 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 드롭 박스, 인트로 애니메이션, 완료 게이트 등의 UI 참조를
///          SerializeField로 바인딩하고 부모 Logic의 추상 프로퍼티를 override한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
///          특이하게 Director_Problem2_Step1_Logic을 상속받아 공통 드롭 박스 + 인트로 로직을 재사용한다.
/// 【문제/스텝】 Director 테마 > 문제6 > 스텝1 (인트로/도입 - 드롭 박스 + 좌우 슬라이드)
/// 【부모 클래스】 Director_Problem2_Step1_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem6 Step1 GameObject에 부착
/// 【참조되는 곳】 Director_Problem2_Step1_Logic (드롭 박스 + 인트로 애니메이션 로직)
/// 【흐름】 스텝 진입 → 좌/우 패널 슬라이드 인트로 → 드롭 박스에 아이템 드롭 → 완료 게이트 열림
/// </summary>
public class Director_Problem6_Step1 : Director_Problem2_Step1_Logic
{
    [Header("Drop Box 영역")]
    [SerializeField] private UIDropBoxArea dropBoxArea;            // 아이템 드롭을 감지하는 UI 드롭 박스 영역

    [Header("인트로 애니메이션")]
    [SerializeField] private RectTransform leftEnterRoot;          // 좌측에서 슬라이드 인 되는 UI 패널
    [SerializeField] private RectTransform rightEnterRoot;         // 우측에서 슬라이드 인 되는 UI 패널
    [SerializeField] private float introDuration = 0.5f;           // 슬라이드 애니메이션 지속 시간 (초)
    [SerializeField] private float leftStartOffsetX = -300f;       // 좌측 패널의 시작 X 오프셋 (화면 밖)
    [SerializeField] private float rightStartOffsetX = 300f;       // 우측 패널의 시작 X 오프셋 (화면 밖)
    [SerializeField] private float introDelay = 0.1f;              // 인트로 시작 전 지연 시간 (초)

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;    // 스텝 완료 판정용 게이트

    // ===== 부모 추상 프로퍼티를 인스펙터 필드로 연결 =====
    protected override UIDropBoxArea DropBoxArea => dropBoxArea;
    protected override RectTransform LeftEnterRoot => leftEnterRoot;
    protected override RectTransform RightEnterRoot => rightEnterRoot;
    protected override float IntroDuration => introDuration;
    protected override float LeftStartOffsetX => leftStartOffsetX;
    protected override float RightStartOffsetX => rightStartOffsetX;
    protected override float IntroDelay => introDelay;
    protected override StepCompletionGate CompletionGate => completionGate;
}
