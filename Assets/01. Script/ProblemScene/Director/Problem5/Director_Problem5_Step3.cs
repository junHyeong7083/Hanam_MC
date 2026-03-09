using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem5 / Step3
/// - 시나리오 카드 순차 진행 + NPC 응답
/// </summary>
public class Director_Problem5_Step3 : Director_Problem5_Step3_Logic
{
    [Serializable]
    public class ScenarioData : IScenarioCardData
    {
        [Tooltip("시나리오 ID (로그용)")]
        public int id = 1;

        [Tooltip("CSV textId (시나리오 텍스트)")]
        public int textId;

        [Tooltip("CSV textId (NPC 응답 텍스트)")]
        public int responseTextId;

        // ==== 인터페이스 구현 ====
        public int Id => id;
        public int TextId => textId;
        public int ResponseTextId => responseTextId;
    }

    [Header("시나리오 데이터")]
    [SerializeField] private ScenarioData[] scenarios;

    [Header("NPC 응답 UI")]
    [SerializeField] private GameObject npcResponseRoot;
    [SerializeField] private Text npcResponseText;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("마이크 (STT)")]
    [SerializeField] private MicRecordingIndicator micIndicator;

    // ===== 베이스에 값 주입용 override =====

    protected override IScenarioCardData[] Scenarios => scenarios;

    protected override GameObject NpcResponseRoot => npcResponseRoot;
    protected override Text NpcResponseText => npcResponseText;

    protected override StepCompletionGate CompletionGate => completionGate;

    protected override MicRecordingIndicator MicIndicator => micIndicator;
}
