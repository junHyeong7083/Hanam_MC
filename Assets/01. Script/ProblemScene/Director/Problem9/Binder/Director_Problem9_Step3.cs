using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem9 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem9_Step3_Logic(부모)에 있음.
///
/// [흐름]
/// 1. situation(상황) 단계: 마이크 클릭(녹음) → 클릭(완료)
/// 2. feeling(감정) 단계: 마이크 클릭(녹음) → 클릭(완료)
/// 3. request(바람) 단계: 마이크 클릭(녹음) → 클릭(완료)
/// 4. complete 화면: 합쳐진 대사 표시 → "다음으로" 버튼 → Gate 완료
///
/// [TODO] STT 기능 추후 추가 예정
/// </summary>
public class Director_Problem9_Step3 : Director_Problem9_Step3_Logic
{
    [Header("===== 연습 단계 데이터 =====")]
    [SerializeField] private PracticeStepData[] practiceSteps = new PracticeStepData[]
    {
        new PracticeStepData
        {
            id = "situation",
            emoji = "📍",
            title = "상황",
            question = "먼저, \"당신이 퉁명스럽게 말했을 때 (상황)\"을 따라 말해주세요.",
            placeholder = "당신이 퉁명스럽게 말했을 때..."
        },
        new PracticeStepData
        {
            id = "feeling",
            emoji = "💭",
            title = "감정",
            question = "이제 \"나는 조금 당황스러웠어요 (감정)\"를 따라 말해주세요.",
            placeholder = "나는 조금 당황스러웠어요..."
        },
        new PracticeStepData
        {
            id = "request",
            emoji = "🎯",
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

    [Tooltip("단계 이모지 (📍, 💭, 🎯)")]
    [SerializeField] private Text stepIndicatorEmoji;

    [Tooltip("단계 제목 (상황, 감정, 바람)")]
    [SerializeField] private Text stepIndicatorTitle;

    [Tooltip("마이크 버튼")]
    [SerializeField] private Button micButton;

    [Tooltip("마이크 버튼 이미지 (색상 변경용)")]
    [SerializeField] private Image micButtonImage;

    [Tooltip("녹음 상태 텍스트")]
    [SerializeField] private Text recordingStatusText;

    [Tooltip("사용자 입력 표시 영역 (STT 결과 표시용)")]
    [SerializeField] private GameObject userInputDisplayRoot;

    [Tooltip("사용자 입력 텍스트")]
    [SerializeField] private Text userInputDisplayText;

    [Header("===== 진행도 UI (3개) =====")]
    [SerializeField] private ProgressUI[] progressIndicators;

    [Header("===== 완료 화면 UI (Gate의 completeRoot 안에 배치) =====")]
    [Tooltip("최종 합쳐진 대사 표시")]
    [SerializeField] private Text combinedDialogueText;

    [Header("===== 완료 게이트 =====")]
    [SerializeField] private StepCompletionGate completionGate;

    #region 부모 추상 프로퍼티 구현

    protected override PracticeStepData[] PracticeSteps => practiceSteps;
    protected override GameObject RecordingPracticeRoot => recordingPracticeRoot;
    protected override Text QuestionText => questionText;
    protected override Text StepIndicatorEmoji => stepIndicatorEmoji;
    protected override Text StepIndicatorTitle => stepIndicatorTitle;
    protected override Button MicButton => micButton;
    protected override Image MicButtonImage => micButtonImage;
    protected override Text RecordingStatusText => recordingStatusText;
    protected override GameObject UserInputDisplayRoot => userInputDisplayRoot;
    protected override Text UserInputDisplayText => userInputDisplayText;
    protected override ProgressUI[] ProgressIndicators => progressIndicators;
    protected override Text CombinedDialogueText => combinedDialogueText;
    protected override StepCompletionGate CompletionGateRef => completionGate;

    #endregion
}
