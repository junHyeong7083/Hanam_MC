using UnityEngine;

/// <summary>
/// Director_Problem6_Step2 - 문제6 스텝2 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 스트레스 카드 슬롯(8개), 스튜디오 조명(3개), 완료 게이트의
///          UI 참조를 SerializeField로 바인딩하고 부모 Logic의 추상 프로퍼티를 override한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제6 > 스텝2 (메인 활동 - 스트레스 반응 카드 선택)
/// 【부모 클래스】 Director_Problem6_Step2_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem6 Step2 GameObject에 부착
/// 【참조되는 곳】 Director_Problem6_Step2_Logic (카드 선택/조명/게이트 로직)
/// </summary>
public class Director_Problem6_Step2 : Director_Problem6_Step2_Logic
{
    [Header("스트레스 카드 슬롯 (8개)")]
    [SerializeField] private StressCardSlot[] cardSlots;       // 8장의 스트레스 반응 카드 데이터 + UI

    [Header("조명 슬롯 (3개)")]
    [SerializeField] private StudioLightSlot[] studioLights;   // 3개의 스튜디오 조명 (선택 개수에 따라 점등)

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate; // 스텝 완료 판정용 게이트

    // ===== 부모 추상 프로퍼티를 인스펙터 필드로 연결 =====
    protected override StressCardSlot[] Cards => cardSlots;
    protected override StudioLightSlot[] Lights => studioLights;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
}
