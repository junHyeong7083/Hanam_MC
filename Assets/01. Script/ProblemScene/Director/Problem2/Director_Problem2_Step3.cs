using System;
using UnityEngine;
using UnityEngine.UI;

public class Director_Problem2_Step3 : Director_Problem2_Step3_Logic
{
    private sealed class RuntimeOption : IDirectorProblem2PerspectiveOption
    {
        private readonly int _id;
        private readonly int _textId;
        private readonly string[] _keywords;

        public RuntimeOption(int id, int textId, string[] keywords)
        {
            _id = id;
            _textId = textId;
            _keywords = keywords;
        }

        public int Id => _id;
        public string Text => ProblemRuntime.L(_textId);
        public string[] Keywords => _keywords;
    }

    [Header("데이터 (인덱스 기반: 캐러셀 순서대로 넣기)")]
    [SerializeField] private int ngSentenceTextId = 0;
    [SerializeField] private int[] perspectiveTextIds;
    [SerializeField] private int[] perspectiveIds; // 선택 (비워도 됨)
    [SerializeField] private string[] perspectiveKeywords; // 선택: "키1|키2"

    [Header("상단 안내 텍스트 (로컬라이즈 ID)")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextIdBefore = 0;
    [SerializeField] private int guideTextIdAfter = 0;

    [Header("초기 텍스트 덮어쓰기 옵션")]
    [SerializeField] private bool overwriteSceneTextOnReset = false;

    [Header("씬 카드 UI (NG / OK)")]
    [SerializeField] private Text sceneText;
    [SerializeField] private RectTransform sceneCardRect;
    [SerializeField] private GameObject okSceneCard;
    [SerializeField] private Text okSceneText;

    [Header("카드 플립 컴포넌트")]
    [SerializeField] private CardFlip cardFlip;

    [Header("관점 선택 UI (캐러셀)")]
    [SerializeField] private GameObject carouselRoot;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Text carouselText;
    [SerializeField] private Text carouselIndexText;

    [Header("마이크 UI")]
    [SerializeField] private GameObject micButtonRoot;
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("녹음 중 표시 이미지 (SetActive 토글)")]
    [SerializeField] private GameObject recordingOverlay;

    [Header("STT 완료 후 표시할 NextStep 버튼 루트")]
    [SerializeField] private GameObject nextStepButtonRoot;

    [Header("패널 전환")]
    [SerializeField] private GameObject stepRoot;
    [SerializeField] private GameObject summaryPanelRoot;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("플립 옵션")]
    [SerializeField] private float flipDelay = 0.3f;

    private IDirectorProblem2PerspectiveOption[] _options;

    private void Awake()
    {
        BuildOptions();
    }

    private void BuildOptions()
    {
        if (perspectiveTextIds == null || perspectiveTextIds.Length == 0)
        {
            _options = Array.Empty<IDirectorProblem2PerspectiveOption>();
            return;
        }

        int n = perspectiveTextIds.Length;
        _options = new IDirectorProblem2PerspectiveOption[n];

        for (int i = 0; i < n; i++)
        {
            int id = (perspectiveIds != null && i < perspectiveIds.Length && perspectiveIds[i] != 0)
                ? perspectiveIds[i]
                : (i + 1);

            int textId = perspectiveTextIds[i];

            string[] keywords = null;
            if (perspectiveKeywords != null && i < perspectiveKeywords.Length)
            {
                var raw = perspectiveKeywords[i];
                if (!string.IsNullOrWhiteSpace(raw))
                    keywords = SplitKeywords(raw, '|');
            }

            _options[i] = new RuntimeOption(id, textId, keywords);
        }
    }

    private static string[] SplitKeywords(string raw, char sep)
    {
        var parts = raw.Split(new[] { sep }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < parts.Length; i++)
            parts[i] = (parts[i] ?? "").Trim();
        return parts;
    }

    protected override string NgSentence => ProblemRuntime.L(ngSentenceTextId);
    protected override IDirectorProblem2PerspectiveOption[] Perspectives => _options;

    protected override Text GuideText => guideText;
    protected override int GuideTextId_Before => guideTextIdBefore;
    protected override int GuideTextId_After => guideTextIdAfter;

    protected override bool OverwriteSceneTextOnReset => overwriteSceneTextOnReset;

    protected override Text SceneText => sceneText;
    protected override RectTransform SceneCardRect => sceneCardRect;
    protected override GameObject OkSceneCard => okSceneCard;
    protected override Text OkSceneText => okSceneText;

    protected override CardFlip CardFlip => cardFlip;

    protected override GameObject CarouselRoot => carouselRoot;
    protected override Button PrevButton => prevButton;
    protected override Button NextButton => nextButton;
    protected override Text CarouselText => carouselText;
    protected override Text CarouselIndexText => carouselIndexText;

    protected override GameObject MicButtonRoot => micButtonRoot;
    protected override MicRecordingIndicator MicIndicator => micIndicator;

    protected override GameObject RecordingOverlay => recordingOverlay;
    protected override GameObject NextStepButtonRoot => nextStepButtonRoot;

    protected override GameObject StepRoot => stepRoot;
    protected override GameObject SummaryPanelRoot => summaryPanelRoot;

    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float FlipDelay => flipDelay;
}