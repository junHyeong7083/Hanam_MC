using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem5 / Step3
/// - 3라운드: 필름(이미지+텍스트) + 하남 대사(TTS) + 초록색 대사(STT)
/// </summary>
public class Director_Problem5_Step3 : Director_Problem5_Step3_Logic
{
    [Serializable]
    public class ScenarioData : IScenarioCardData
    {
        [Tooltip("시나리오 ID (로그용)")]
        public int id = 1;

        [Tooltip("필름 이미지")]
        public Sprite filmSprite;

        [Tooltip("필름 내 텍스트 textId")]
        public int filmTextId;

        [Tooltip("하남 텍스트 textId (하단 대사 + TTS)")]
        public int hanamTextId;

        [Tooltip("초록색 네모 textId (사용자가 말해야 하는 대사 / STT 키워드)")]
        public int responseTextId;

        // ==== 인터페이스 구현 ====
        public int Id => id;
        public Sprite FilmSprite => filmSprite;
        public int FilmTextId => filmTextId;
        public int HanamTextId => hanamTextId;
        public int ResponseTextId => responseTextId;
    }

    [Header("시나리오 데이터")]
    [SerializeField] private ScenarioData[] scenarios;

    [Header("필름 UI")]
    [SerializeField] private Image filmImage;
    [SerializeField] private Text filmText;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("마이크 (STT)")]
    [SerializeField] private MicRecordingIndicator micIndicator;

    // ===== 베이스에 값 주입용 override =====

    protected override IScenarioCardData[] Scenarios => scenarios;

    protected override Image FilmImage => filmImage;
    protected override Text FilmText => filmText;

    protected override StepCompletionGate CompletionGate => completionGate;

    protected override MicRecordingIndicator MicIndicator => micIndicator;
}
