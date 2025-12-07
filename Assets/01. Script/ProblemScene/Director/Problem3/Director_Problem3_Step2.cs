using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem3 / Step2
/// - 인스펙터에서 데이터/UI를 바인딩.
/// - 실제 로직은 Director_Problem3_Step2_Logic(부모)에서 처리.
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

    [Header("재작성 단계 데이터")]
    [SerializeField] private RewriteStepData[] steps;

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem3_Step2_EffectController effectController;

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

    // ==== 베이스로 값 전달을 위한 override 프로퍼티 ====

    protected override IRewriteStepData[] Steps => steps;

    protected override Problem3_Step2_EffectController EffectController => effectController;

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
