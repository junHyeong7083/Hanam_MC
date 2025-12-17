using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem9 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem9_Step3_Logic(부모)에 있음.
///
/// [흐름]
/// 1. situation(상황) 단계: 마이크 프리팹에서 녹음 → OnRecordingComplete 호출
/// 2. feeling(감정) 단계: 마이크 프리팹에서 녹음 → OnRecordingComplete 호출
/// 3. request(바람) 단계: 마이크 프리팹에서 녹음 → OnRecordingComplete 호출
/// 4. complete 화면: 합쳐진 대사 표시 → Gate 완료
/// </summary>
public class Director_Problem9_Step3 : Director_Problem9_Step3_Logic
{
    [Header("===== 연습 단계 데이터 =====")]
    [SerializeField] private PracticeStepData[] practiceSteps = new PracticeStepData[]
    {
        new PracticeStepData
        {
            id = "situation",
            title = "상황",
            question = "먼저, \"당신이 퉁명스럽게 말했을 때 (상황)\"을 따라 말해주세요.",
            placeholder = "당신이 퉁명스럽게 말했을 때..."
        },
        new PracticeStepData
        {
            id = "feeling",
            title = "감정",
            question = "이제 \"나는 조금 당황스러웠어요 (감정)\"를 따라 말해주세요.",
            placeholder = "나는 조금 당황스러웠어요..."
        },
        new PracticeStepData
        {
            id = "request",
            title = "바람",
            question = "마지막으로 \"구체적으로 알려주시면 좋겠어요 (바람)\"를 따라 말해주세요.",
            placeholder = "구체적으로 알려주시면 좋겠어요..."
        }
    };

    [Header("===== 화면 루트 =====")]
    [Tooltip("녹음 연습 화면 (situation, feeling, request 공용)")]
    [SerializeField] private GameObject recordingPracticeRoot;

    [Header("===== 녹음 화면 UI =====")]
    [Tooltip("조감독 질문 텍스트")]
    [SerializeField] private Text questionText;

    [Tooltip("단계 제목 (상황, 감정, 바람)")]
    [SerializeField] private Text stepIndicatorTitle;

    [Tooltip("사용자 입력 표시 영역 (STT 결과 표시용)")]
    [SerializeField] private GameObject userInputDisplayRoot;

    [Tooltip("사용자 입력 텍스트")]
    [SerializeField] private Text userInputDisplayText;

    [Header("===== 진행도 이미지 (3개) =====")]
    [Tooltip("단계별 진행도 이미지 (0: 상황, 1: 감정, 2: 바람)")]
    [SerializeField] private Image[] progressImages;

    [Header("===== 완료 화면 UI (Gate의 completeRoot 안에 배치) =====")]
    [Tooltip("최종 합쳐진 대사 표시")]
    [SerializeField] private Text combinedDialogueText;

    [Header("===== 완료 게이트 =====")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("===== 마이크 프리팹 =====")]
    [Tooltip("마이크 버튼 클릭 시 녹음 시작/종료")]
    [SerializeField] private MicRecordingIndicator micRecordingIndicator;

    #region 부모 추상 프로퍼티 구현

    protected override PracticeStepData[] PracticeSteps => practiceSteps;
    protected override GameObject RecordingPracticeRoot => recordingPracticeRoot;
    protected override Text QuestionText => questionText;
    protected override Text StepIndicatorTitle => stepIndicatorTitle;
    protected override GameObject UserInputDisplayRoot => userInputDisplayRoot;
    protected override Text UserInputDisplayText => userInputDisplayText;
    protected override Image[] ProgressImages => progressImages;
    protected override Text CombinedDialogueText => combinedDialogueText;
    protected override StepCompletionGate CompletionGateRef => completionGate;

    #endregion

    #region 마이크 이벤트 연결

    protected override void OnStepEnter()
    {
        base.OnStepEnter();

        // 마이크 녹음 종료 이벤트 구독 (STT 결과 상관없이 녹음 종료로 처리)
        if (micRecordingIndicator != null)
        {
            micRecordingIndicator.OnKeywordMatched -= HandleKeywordMatched;
            micRecordingIndicator.OnKeywordMatched += HandleKeywordMatched;
            micRecordingIndicator.OnNoMatch -= HandleNoMatch;
            micRecordingIndicator.OnNoMatch += HandleNoMatch;
        }
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        // 이벤트 구독 해제
        if (micRecordingIndicator != null)
        {
            micRecordingIndicator.OnKeywordMatched -= HandleKeywordMatched;
            micRecordingIndicator.OnNoMatch -= HandleNoMatch;
        }
    }

    private void HandleKeywordMatched(int index)
    {
        // 키워드 매칭 여부 상관없이 녹음 종료로 처리
        string text = GetCurrentPlaceholder();
        OnRecordingComplete(text, 0f);
    }

    private void HandleNoMatch(string result)
    {
        // 매칭 실패도 녹음 종료로 처리
        string text = GetCurrentPlaceholder();
        OnRecordingComplete(text, 0f);
    }

    #endregion
}
