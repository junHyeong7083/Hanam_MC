using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem5_Step3 - 문제5 스텝3의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 시나리오 데이터(ScenarioData), 필름 UI, 완료 게이트,
///         마이크 인디케이터를 바인딩한다.
///         실제 시나리오 순차 진행/STT 로직은 부모(Director_Problem5_Step3_Logic)에 있다.
///         ScenarioData 내부 클래스가 IScenarioCardData를 구현한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제5 / 스텝3 (마무리 - 시나리오 순차 진행 + STT)
/// 【부모 클래스】 Director_Problem5_Step3_Logic → ProblemStepBase
/// </summary>
public class Director_Problem5_Step3 : Director_Problem5_Step3_Logic
{
    /// <summary>
    /// 개별 시나리오 라운드 데이터. IScenarioCardData 인터페이스를 구현한다.
    /// textId 기반으로 CSV에서 필름/하남/응답 텍스트를 가져온다.
    /// </summary>
    [Serializable]
    public class ScenarioData : IScenarioCardData
    {
        [Tooltip("시나리오 ID (로그용)")]
        public int id = 1;                // 시나리오 고유 ID

        [Tooltip("필름 이미지")]
        public Sprite filmSprite;          // 필름 카드 이미지

        [Tooltip("필름 내 텍스트 textId")]
        public int filmTextId;             // 필름 내 표시할 텍스트 CSV ID

        [Tooltip("하남 텍스트 textId (하단 대사 + TTS)")]
        public int hanamTextId;            // 하남이 대사 + TTS 재생용 텍스트 ID

        [Tooltip("초록색 네모 textId (사용자가 말해야 하는 대사 / STT 키워드)")]
        public int responseTextId;         // 사용자 응답 대사 + STT 키워드용 텍스트 ID

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
