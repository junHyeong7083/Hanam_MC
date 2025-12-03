using UnityEngine;

/// <summary>
/// Director / Problem9 / Step2
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem9_Step2_Logic(부모)에 있음.
///
/// [흐름]
/// 1. 시나리오 화면: 상황 + 3개 선택지 (공격적/수동적/건강한)
/// 2. 건강한 표현(c) 선택 → OK컷 결과 → 다음 시나리오
/// 3. 그 외 선택 → NG 결과 → 다시 시도
/// 4. 3개 시나리오 모두 완료 → Gate 완료 → Step3으로 전환
/// </summary>
public class Director_Problem9_Step2 : Director_Problem9_Step2_Logic
{
    [Header("===== 시나리오 데이터 =====")]
    [SerializeField] private ScenarioData[] scenarios = new ScenarioData[]
    {
        new ScenarioData
        {
            id = 1,
            situation = "동료가 회의 중에 내 의견을 무시하고 자기 의견만 강요했어요.",
            choices = new ChoiceData[]
            {
                new ChoiceData { id = "a", type = ChoiceType.Aggressive, text = "당신은 항상 남의 말을 안 들어요! 이기적이에요!", label = "공격적 표현" },
                new ChoiceData { id = "b", type = ChoiceType.Passive, text = "...아무 말도 하지 않고 속으로 참는다", label = "수동적 표현" },
                new ChoiceData { id = "c", type = ChoiceType.Healthy, text = "회의 중에 제 의견이 무시된 것 같아 속상했어요. 다음엔 제 말도 들어주시면 좋겠어요.", label = "건강한 표현" }
            },
            okResponse = "동료가 미안하다고 말하며 앞으로는 서로 의견을 존중하자고 제안합니다.",
            ngResponse = "관계가 더 나빠지고 서로 불편한 분위기가 이어집니다."
        },
        new ScenarioData
        {
            id = 2,
            situation = "친구가 약속 시간에 30분 늦게 와서 연락도 없었어요.",
            choices = new ChoiceData[]
            {
                new ChoiceData { id = "a", type = ChoiceType.Aggressive, text = "너는 맨날 이래! 시간 약속 못 지키면서 왜 약속을 해?", label = "공격적 표현" },
                new ChoiceData { id = "b", type = ChoiceType.Passive, text = "괜찮아... (속으로는 화가 나지만 참는다)", label = "수동적 표현" },
                new ChoiceData { id = "c", type = ChoiceType.Healthy, text = "연락 없이 늦게 와서 걱정했어. 다음엔 늦을 것 같으면 미리 말해줬으면 좋겠어.", label = "건강한 표현" }
            },
            okResponse = "친구가 사과하며 다음부터는 연락하겠다고 약속합니다.",
            ngResponse = "친구도 기분이 상해서 말다툼이 시작됩니다."
        },
        new ScenarioData
        {
            id = 3,
            situation = "가족이 내가 힘들어하는 걸 알면서도 계속 일을 시켜요.",
            choices = new ChoiceData[]
            {
                new ChoiceData { id = "a", type = ChoiceType.Aggressive, text = "내가 로봇이야? 나도 힘든데 왜 자꾸 시켜?!", label = "공격적 표현" },
                new ChoiceData { id = "b", type = ChoiceType.Passive, text = "...아무 말 없이 계속 일을 한다", label = "수동적 표현" },
                new ChoiceData { id = "c", type = ChoiceType.Healthy, text = "지금 좀 힘들어서 쉬고 싶어요. 나중에 해도 될까요?", label = "건강한 표현" }
            },
            okResponse = "가족이 이해하며 쉬라고 말해줍니다.",
            ngResponse = "서로 언성이 높아지고 감정이 상합니다."
        }
    };

    [Header("===== 화면 루트 =====")]
    [Tooltip("시나리오 화면 (상황 + 선택지)")]
    [SerializeField] private GameObject scenarioRoot;

    [Tooltip("OK컷 결과 화면")]
    [SerializeField] private GameObject okResultRoot;

    [Tooltip("NG 결과 화면")]
    [SerializeField] private GameObject ngResultRoot;

    [Header("===== 시나리오 UI =====")]
    [SerializeField] private ScenarioUI scenarioUI;

    [Header("===== OK 결과 UI =====")]
    [SerializeField] private ResultUI okResultUI;

    [Header("===== NG 결과 UI =====")]
    [SerializeField] private ResultUI ngResultUI;

    [Header("===== 진행도 표시 (3개) =====")]
    [SerializeField] private ProgressDot[] progressDots;

    [Header("===== 완료 게이트 =====")]
    [SerializeField] private StepCompletionGate completionGate;

    #region 부모 추상 프로퍼티 구현

    protected override ScenarioData[] Scenarios => scenarios;
    protected override GameObject ScenarioRoot => scenarioRoot;
    protected override GameObject OkResultRoot => okResultRoot;
    protected override GameObject NgResultRoot => ngResultRoot;
    protected override ScenarioUI ScenarioUIRef => scenarioUI;
    protected override ResultUI OkResultUIRef => okResultUI;
    protected override ResultUI NgResultUIRef => ngResultUI;
    protected override ProgressDot[] ProgressDots => progressDots;
    protected override StepCompletionGate CompletionGateRef => completionGate;

    #endregion
}
