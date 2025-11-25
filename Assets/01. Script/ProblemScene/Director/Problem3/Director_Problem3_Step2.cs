using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem3 / Step2
/// - 인스펙터에서 데이터/UI만 설정.
/// - 실질적인 로직은 Director_Problem3_Step2_Logic(부모)에 있음.
/// </summary>
public class Director_Problem3_Step2 : Director_Problem3_Step2_Logic
{
    [Serializable]
    public class RewriteStepData : IRewriteStepData
    {
        public int id = 1;

        [TextArea]
        public string originalText;

        [TextArea]
        public string rewrittenText;

        [TextArea]
        public string[] options;

        int IRewriteStepData.Id => id;
        string IRewriteStepData.OriginalText => originalText;
        string IRewriteStepData.RewrittenText => rewrittenText;
        string[] IRewriteStepData.Options => options;
    }

    [Header("재해석 단계 데이터")]
    [SerializeField] private RewriteStepData[] steps;

    [Header("문장 UI")]
    [SerializeField] private Text sentenceText;
    [SerializeField] private Color originalTextColor = new Color(0.24f, 0.18f, 0.14f);
    [SerializeField] private Color rewrittenTextColor = new Color(1f, 0.54f, 0.24f);
    [SerializeField] private CanvasGroup sentenceCanvasGroup;   // 선택

    [Header("펜 아이콘 연출 (옵션)")]
    [SerializeField] private RectTransform penIcon;
    [SerializeField] private float penAnimDelay = 0.5f;
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private float fadeInDuration = 0.25f;

    [Header("옵션 버튼들 (최대 N개)")]
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private Text[] optionLabels;

    [Header("옵션 색상")]
    [SerializeField] private Color optionNormalColor = Color.white;
    [SerializeField] private Color optionSelectedColor = new Color(1f, 0.54f, 0.24f);
    [SerializeField] private Color optionDisabledColor = new Color(1f, 1f, 1f, 0.4f);

    [Header("상단 진행도 점들 (옵션)")]
    [SerializeField] private Image[] progressDots;
    [SerializeField] private Color progressDoneColor = new Color(0.22f, 0.8f, 0.4f);
    [SerializeField] private Color progressCurrentColor = new Color(1f, 0.54f, 0.24f);
    [SerializeField] private Color progressPendingColor = new Color(1f, 1f, 1f, 0.2f);

    [Header("하단 네비게이션 버튼")]
    [SerializeField] private GameObject nextButtonRoot;    // 하나의 버튼 루트
    [SerializeField] private Text nextButtonLabel;
    [SerializeField] private string middleStepLabel = "다음 장면";
    [SerializeField] private string lastStepLabel = "강점 찾기 단계로";

    [Header("완료 게이트 (옵션)")]
    [SerializeField] private StepCompletionGate completionGate;

    // ==== 베이스에 값 주입용 override 프로퍼티 ====

    protected override IRewriteStepData[] Steps => steps;

    protected override Text SentenceText => sentenceText;
    protected override Color OriginalTextColor => originalTextColor;
    protected override Color RewrittenTextColor => rewrittenTextColor;
    protected override CanvasGroup SentenceCanvasGroup => sentenceCanvasGroup;

    protected override RectTransform PenIcon => penIcon;
    protected override float PenAnimDelay => penAnimDelay;
    protected override float FadeOutDuration => fadeOutDuration;
    protected override float FadeInDuration => fadeInDuration;

    protected override Button[] OptionButtons => optionButtons;
    protected override Text[] OptionLabels => optionLabels;

    protected override Color OptionNormalColor => optionNormalColor;
    protected override Color OptionSelectedColor => optionSelectedColor;
    protected override Color OptionDisabledColor => optionDisabledColor;

    protected override Image[] ProgressDots => progressDots;
    protected override Color ProgressDoneColor => progressDoneColor;
    protected override Color ProgressCurrentColor => progressCurrentColor;
    protected override Color ProgressPendingColor => progressPendingColor;

    protected override GameObject NextButtonRoot => nextButtonRoot;
    protected override Text NextButtonLabel => nextButtonLabel;
    protected override string MiddleStepLabel => middleStepLabel;
    protected override string LastStepLabel => lastStepLabel;

    protected override StepCompletionGate CompletionGate => completionGate;
}
