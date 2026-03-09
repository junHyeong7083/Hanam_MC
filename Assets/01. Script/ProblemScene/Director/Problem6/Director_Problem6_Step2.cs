using UnityEngine;

/// <summary>
/// Director / Problem6 / Step2
/// - 인스펙터에서 카드 / 조명 / 버튼 / 게이트 참조를 받고,
///   실제 로직은 Director_Problem6_Step2_Logic(부모)에서 처리.
/// </summary>
public class Director_Problem6_Step2 : Director_Problem6_Step2_Logic
{
    [Header("스트레스 카드 슬롯 (8개)")]
    [SerializeField] private StressCardSlot[] cardSlots;

    [Header("조명 슬롯 (3개)")]
    [SerializeField] private StudioLightSlot[] studioLights;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    // ---- Logic 베이스에 넘겨줄 프로퍼티 구현 ----
    protected override StressCardSlot[] Cards => cardSlots;
    protected override StudioLightSlot[] Lights => studioLights;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
}
