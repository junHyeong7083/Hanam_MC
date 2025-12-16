using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Part5 / Step3
/// - �ν����Ϳ��� ������/��ư/UI ������ ��� �ִ� ����.
/// - ���� �ൿ/���¸ӽ��� Director_Problem5_Step3_Logic �ʿ��� ó��.
/// </summary>
public class Director_Problem5_Step3 : Director_Problem5_Step3_Logic
{
    [Serializable]
    public class DialogueOptionData : IDialogueOptionData
    {
        [Tooltip("�ɼ� ID (�α׿�)")]
        public int id = 1;

        [TextArea]
        [Tooltip("ȭ�鿡 ǥ�õ� ��� �ؽ�Ʈ")]
        public string text;

        [Tooltip("��� Ÿ�� (ȸ���� / �ǰ� / ������)")]
        public DialogueOptionType type = DialogueOptionType.Avoidant;

        [TextArea]
        [Tooltip("���� �� ������ �ǵ�� ����")]
        public string feedback;

        [Tooltip("�� �ɼ��� ����(�ǰ��� ���)���� ����")]
        public bool isCorrect = false;

        [Header("UI ����")]
        public Button button;   // Ŭ�� ��ư (ī�� ��ü)
        public Text label;      // ��ư �� �ؽ�Ʈ

        // ==== �������̽� ���� ====
        public int Id => id;
        public string Text => text;
        public DialogueOptionType Type => type;
        public string Feedback => feedback;
        public bool IsCorrect => isCorrect;

        public Button Button => button;
        public Text Label => label;
    }

    [Header("��������")]
    [SerializeField] private DialogueOptionData[] options;

    [Header("NPC 응답 UI")]
    [SerializeField] private GameObject npcResponseRoot;

    [Header("������ �ǵ�� UI")]
    [SerializeField] private GameObject feedbackRoot;
    [SerializeField] private Text feedbackLabel;

    [Header("���� ����")]
    [SerializeField] private Color optionNormalColor = Color.white;
    [SerializeField] private Color optionHealthyColor = Color.green;
    [SerializeField] private Color optionWrongColor = Color.red;

    [Header("마이크 STT")]
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("Ÿ�̹� ����")]
    [SerializeField] private float optionSelectDelay = 1.5f;
    [SerializeField] private float npcResponseDelay = 1.0f;

    [Header("�Ϸ� ����Ʈ")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("���� �ǵ�� ����")]
    [SerializeField] private Image wrongFeedbackImage;
    [SerializeField] private float wrongFeedbackImageHeightOnWrong = -300f;
    [SerializeField] private float wrongFeedbackShowDuration = 1.0f;
    [SerializeField] private GameObject feedbackNextButtonRoot;

    [Header("정답 이미지 연출")]
    [SerializeField] private GameObject originalAnswerImage;
    [SerializeField] private PopupImageDisplay correctAnswerPopup;
    // ===== ���̽��� �� ���Կ� override =====

    protected override IDialogueOptionData[] Options => options;

    protected override GameObject NpcResponseRoot => npcResponseRoot;

    protected override GameObject FeedbackRoot => feedbackRoot;
    protected override Text FeedbackLabel => feedbackLabel;


    protected override Color OptionNormalColor => optionNormalColor;
    protected override Color OptionHealthyColor => optionHealthyColor;
    protected override Color OptionWrongColor => optionWrongColor;

    protected override MicRecordingIndicator MicIndicator => micIndicator;

    protected override float OptionSelectDelay => optionSelectDelay;
    protected override float NpcResponseDelay => npcResponseDelay;

    protected override StepCompletionGate CompletionGate => completionGate;


    protected override float WrongFeedbackShowDuration => wrongFeedbackShowDuration;

    protected override GameObject FeedbackNextButtonRoot => feedbackNextButtonRoot;

    protected override GameObject OriginalAnswerImage => originalAnswerImage;
    protected override PopupImageDisplay CorrectAnswerPopup => correctAnswerPopup;
}
