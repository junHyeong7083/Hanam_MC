using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem6 / Step2
/// - �ν����Ϳ��� ī�� / ���� / ��ư / ���� �������ְ�,
///   ������ ������ Director_Problem6_Step2_Logic(�θ�)���� ó��.
/// </summary>
public class Director_Problem6_Step2 : Director_Problem6_Step2_Logic
{
    [Header("��Ʈ���� ī�� ���Ե� (8��)")]
    [SerializeField] private StressCardSlot[] cardSlots;

    [Header("조명 슬롯 (3개)")]
    [SerializeField] private StudioLightSlot[] studioLights;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("하남박스")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextId;

    // ---- Logic 베이스에 넘겨줄 프로퍼티 구현 ----
    protected override StressCardSlot[] Cards => cardSlots;
    protected override StudioLightSlot[] Lights => studioLights;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
    protected override Text GuideText => guideText;
    protected override int GuideTextId => guideTextId;
}
