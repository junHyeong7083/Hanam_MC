using UnityEngine;

/// <summary>
/// Director_Problem10_Step1 - 문제10 스텝1 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 포스터 비주얼 루트, 인트로 루트, 완료 게이트의 참조를 바인딩한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제10 > 스텝1 (인트로/도입 - 포스터 드롭)
/// 【부모 클래스】 Director_Problem10_Step1_Logic → InventoryDropTargetStepBase → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem10 Step1 GameObject에 부착
/// 【참조되는 곳】 Director_Problem10_Step1_Logic (포스터 드롭 로직)
/// </summary>
public class Director_Problem10_Step1 : Director_Problem10_Step1_Logic
{
    [Header("스케일 애니메이션 대상")]
    [SerializeField] private RectTransform posterVisualRoot;       // 포스터 프레임 (스케일 애니메이션 대상)

    [Header("화면 루트 (활성화 시 숨김)")]
    [SerializeField] private GameObject introRoot;                 // 인트로 안내 루트 (드롭 완료 시 숨김)

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;    // 스텝 완료 게이트

    // ===== 부모 추상 프로퍼티를 인스펙터 필드로 연결 =====
    protected override RectTransform PosterVisualRoot => posterVisualRoot;
    protected override GameObject IntroRoot => introRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
}
