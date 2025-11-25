using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem6 / Step2
/// - 인스펙터에서 카드 / 조명 / 버튼 / 라벨을 세팅해주고,
///   나머지 로직은 Director_Problem6_Step2_Logic(부모)에서 처리.
/// </summary>
public class Director_Problem6_Step2 : Director_Problem6_Step2_Logic
{
    [Header("스트레스 카드 슬롯들 (8개)")]
    [SerializeField] private StressCardSlot[] cardSlots;

    [Header("위쪽 조명들 (4개)")]
    [SerializeField] private StudioLightSlot[] studioLights;

    [Header("UI 참조")]
    [SerializeField] private Text progressLabel;
    [SerializeField] private StepCompletionGate completionGate;

    // ---- Logic 베이스에 넘겨줄 프로퍼티 구현 ----
    protected override StressCardSlot[] Cards => cardSlots;
    protected override StudioLightSlot[] Lights => studioLights;
    protected override Text ProgressLabelUI => progressLabel;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
}
