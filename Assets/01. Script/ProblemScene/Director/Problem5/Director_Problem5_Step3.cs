using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Part5 / Step3
/// - 인스펙터에서 선택지/버튼/UI 참조만 들고 있는 래퍼.
/// - 실제 행동/상태머신은 Director_Problem5_Step3_Logic 쪽에서 처리.
/// </summary>
public class Director_Problem5_Step3 : Director_Problem5_Step3_Logic
{
    [Serializable]
    public class DialogueOptionData : IDialogueOptionData
    {
        [Tooltip("옵션 ID (로그용)")]
        public int id = 1;

        [TextArea]
        [Tooltip("화면에 표시될 대사 텍스트")]
        public string text;

        [Tooltip("대사 타입 (회피형 / 건강 / 도전적)")]
        public DialogueOptionType type = DialogueOptionType.Avoidant;

        [TextArea]
        [Tooltip("선택 후 보여줄 피드백 문구")]
        public string feedback;

        [Tooltip("이 옵션이 정답(건강한 대사)인지 여부")]
        public bool isCorrect = false;

        [Header("UI 참조")]
        public Button button;   // 클릭 버튼 (카드 전체)
        public Text label;      // 버튼 안 텍스트

        // ==== 인터페이스 구현 ====
        public int Id => id;
        public string Text => text;
        public DialogueOptionType Type => type;
        public string Feedback => feedback;
        public bool IsCorrect => isCorrect;

        public Button Button => button;
        public Text Label => label;
    }

    [Header("선택지들")]
    [SerializeField] private DialogueOptionData[] options;

    [Header("NPC / 상대 캐릭터 UI")]
    [SerializeField] private Text npcEmojiLabel;
    [SerializeField] private GameObject npcWaitingRoot;
    [SerializeField] private GameObject npcResponseRoot;

    [Header("NPC 반응 대사")]
    [SerializeField] private Text npcResponseTextLabel;

    [Header("선택지 피드백 UI")]
    [SerializeField] private GameObject feedbackRoot;
    [SerializeField] private Text feedbackLabel;

    [Header("색상 설정")]
    [SerializeField] private Color optionNormalColor = Color.white;
    [SerializeField] private Color optionHealthyColor = Color.green;
    [SerializeField] private Color optionWrongColor = Color.red;

    [Header("마이크 입력 UI")]
    [SerializeField] private Button micButton;
    [SerializeField] private GameObject micRecordingIndicatorRoot;

    [Header("타이밍 설정")]
    [SerializeField] private float optionSelectDelay = 1.5f;
    [SerializeField] private float npcResponseDelay = 1.0f;
    [SerializeField] private float voiceRecognitionDuration = 2.0f;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("오답 피드백 연출")]
    [SerializeField] private Image wrongFeedbackImage;
    [SerializeField] private float wrongFeedbackImageHeightOnWrong = -300f;
    [SerializeField] private float wrongFeedbackShowDuration = 1.0f;
    [SerializeField] private GameObject feedbackNextButtonRoot;
    // ===== 베이스에 값 주입용 override =====

    protected override IDialogueOptionData[] Options => options;

    protected override Text NpcEmojiLabel => npcEmojiLabel;
    protected override GameObject NpcWaitingRoot => npcWaitingRoot;
    protected override GameObject NpcResponseRoot => npcResponseRoot;
    protected override Text NpcResponseTextLabel => npcResponseTextLabel;

    protected override GameObject FeedbackRoot => feedbackRoot;
    protected override Text FeedbackLabel => feedbackLabel;


    protected override Color OptionNormalColor => optionNormalColor;
    protected override Color OptionHealthyColor => optionHealthyColor;
    protected override Color OptionWrongColor => optionWrongColor;

    protected override Button MicButton => micButton;
    protected override GameObject MicRecordingIndicatorRoot => micRecordingIndicatorRoot;

    protected override float OptionSelectDelay => optionSelectDelay;
    protected override float NpcResponseDelay => npcResponseDelay;
    protected override float VoiceRecognitionDuration => voiceRecognitionDuration;

    protected override StepCompletionGate CompletionGate => completionGate;


    protected override float WrongFeedbackShowDuration => wrongFeedbackShowDuration;

    protected override GameObject FeedbackNextButtonRoot => feedbackNextButtonRoot;
}
