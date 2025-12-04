using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem2 / Step3
/// - �ν����Ϳ��� UI ������Ʈ + �����͸� ���ε�.
/// - ���� ������ Director_Problem2_Step3_Logic(�θ�)���� ó��.
/// </summary>
public class Director_Problem2_Step3 : Director_Problem2_Step3_Logic
{
    [Serializable]
    public class PerspectiveOption : IDirectorProblem2PerspectiveOption
    {
        public int id;          // 1..N (�ν����Ϳ��� �ο�)
        [TextArea]
        public string text;     // ��: "��뵵 �������� ���� �־�"

        // �������̽� ���� (���� ���̽����� �б������ ���)
        int IDirectorProblem2PerspectiveOption.Id => id;
        string IDirectorProblem2PerspectiveOption.Text => text;
    }

    [Header("���� ����")]
    [TextArea]
    [SerializeField] private string ngSentence = "��� ���� �̻��ϰ� ������ �ž�";
    [SerializeField] private PerspectiveOption[] perspectives;

    [Header("�ʱ� �ؽ�Ʈ ���� �ɼ�")]
    [Tooltip("true�� Reset �� �׻� ngSentence�� ���, false�� �ܺο��� �̸� �־�� sceneText�� �״�� ���")]
    [SerializeField] private bool overwriteSceneTextOnReset = false;

    [Header("�� ī�� UI (NG / OK)")]
    [SerializeField] private Text sceneText;                // ī�� �ȿ� �� ���� �ؽ�Ʈ
    [SerializeField] private GameObject ngBadgeRoot;        // "NG" ���� ������Ʈ
    [SerializeField] private GameObject okBadgeRoot;        // "OK" ���� ������Ʈ
    [SerializeField] private RectTransform sceneCardRect;

    [Header("ī�� �ø� ������Ʈ ")]
    [SerializeField] private CardFlip cardFlip;

    [Header("���� ������ UI")]
    [SerializeField] private GameObject perspectiveButtonsRoot;      // ��ü ������ ���� ��Ʈ
    [SerializeField] private Button[] perspectiveButtons;            // �� ��ư
    [SerializeField] private Text[] perspectiveTexts;                // ��ư �ȿ� �� �ؽ�Ʈ
    [SerializeField] private GameObject[] perspectiveSelectedMarks;  // üũ��ũ �� ���� ǥ��

    [Header("����ũ UI")]
    [SerializeField] private GameObject micButtonRoot;          // ����ũ ��ư ��Ʈ
    [SerializeField] private MicRecordingIndicator micIndicator; // Indicator

    [Header("�г� ��ȯ")]
    [SerializeField] private GameObject stepRoot;               // ���� Step3 �г� ��Ʈ
    [SerializeField] private GameObject summaryPanelRoot;       // ��� �г� ��Ʈ

    [Header("�Ϸ� ����Ʈ (��� ��ư / ��Ÿ ������ Gate���� ó��)")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("���� �ɼ�")]
    [SerializeField] private float flipDelay = 0.3f;    // ���ϱ� �� ~ �ø� ���۱��� ��� �ð�
    [SerializeField] private float flipDuration = 0.5f; // ī�� �� �� �������� ��ü �ð�


    // ===== ���̽��� �� ���Կ� ������Ƽ ���� =====

    protected override string NgSentence => ngSentence;
    protected override IDirectorProblem2PerspectiveOption[] Perspectives => perspectives;
    protected override bool OverwriteSceneTextOnReset => overwriteSceneTextOnReset;

    protected override Text SceneText => sceneText;
    protected override GameObject NgBadgeRoot => ngBadgeRoot;
    protected override GameObject OkBadgeRoot => okBadgeRoot;
    protected override RectTransform SceneCardRect => sceneCardRect;

    protected override CardFlip CardFlip => cardFlip;

    protected override GameObject PerspectiveButtonsRoot => perspectiveButtonsRoot;
    protected override Button[] PerspectiveButtons => perspectiveButtons;
    protected override Text[] PerspectiveTexts => perspectiveTexts;
    protected override GameObject[] PerspectiveSelectedMarks => perspectiveSelectedMarks;

    protected override GameObject MicButtonRoot => micButtonRoot;
    protected override MicRecordingIndicator MicIndicator => micIndicator;

    protected override GameObject StepRoot => stepRoot;
    protected override GameObject SummaryPanelRoot => summaryPanelRoot;

    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float FlipDelay => flipDelay;
    protected override float FlipDuration => flipDuration;
}
