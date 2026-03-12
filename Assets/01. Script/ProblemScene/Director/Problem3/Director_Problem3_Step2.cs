using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem3_Step2 - 문제3 스텝2의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 StepData 배열(IRewriteStepData 구현), 가이드 텍스트,
///         이펙트 컨트롤러, 캐러셀 UI, 마이크 UI, 진행도 점, 완료 게이트 등을 바인딩한다.
///         실제 캐러셀 탐색/STT 재작성 로직은 부모(Director_Problem3_Step2_Logic)에 있다.
///         StepData 내부 클래스가 IRewriteStepData를 구현하여 textId 기반으로
///         런타임에 CSV에서 텍스트를 가져온다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제3 / 스텝2 (메인 활동 - 캐러셀 + STT 재작성)
/// 【부모 클래스】 Director_Problem3_Step2_Logic → ProblemStepBase
/// </summary>
public class Director_Problem3_Step2 : Director_Problem3_Step2_Logic
{
    /// <summary>
    /// 개별 재작성 라운드 데이터. IRewriteStepData 인터페이스를 구현하여
    /// textId 기반으로 CSV에서 원본/재작성/옵션 텍스트를 가져온다.
    /// optionKeywords는 "키1|키2" 형식의 파이프 구분 문자열로, STT 매칭에 사용된다.
    /// </summary>
    [Serializable]
    private class StepData : IRewriteStepData
    {
        public int id;                       // 라운드 ID (로그용)
        public int originalTextId;           // 원본 텍스트 CSV textId
        public int rewrittenTextId;          // 재작성된 텍스트 CSV textId

        public int[] optionTextIds;          // 캐러셀 옵션들의 CSV textId 배열
        public string[] optionKeywords;      // 각 옵션의 STT 키워드 ("키1|키2" 파이프 구분)
        public Sprite[] optionSprites;       // 각 옵션에 대응하는 스프라이트
        public int afterCompleteTextId;      // 라운드 완료 후 가이드 텍스트 ID (0이면 공통값 사용)

        int IRewriteStepData.Id => id;
        string IRewriteStepData.OriginalText => ProblemRuntime.L(originalTextId);
        string IRewriteStepData.RewrittenText => ProblemRuntime.L(rewrittenTextId);

        string[] IRewriteStepData.Options
        {
            get
            {
                if (optionTextIds == null) return Array.Empty<string>();
                var arr = new string[optionTextIds.Length];
                for (int i = 0; i < arr.Length; i++)
                    arr[i] = ProblemRuntime.L(optionTextIds[i]);
                return arr;
            }
        }

        Sprite[] IRewriteStepData.OptionSprites => optionSprites;
        int IRewriteStepData.AfterCompleteTextId => afterCompleteTextId;

        string[][] IRewriteStepData.OptionKeywords
        {
            get
            {
                // optionKeywords가 없으면 null -> 베이스에서 Options 텍스트를 키워드로 사용
                if (optionKeywords == null || optionKeywords.Length == 0)
                    return null;

                // optionKeywords[i] = "키1|키2|키3"
                var result = new string[optionKeywords.Length][];
                for (int i = 0; i < optionKeywords.Length; i++)
                {
                    var raw = optionKeywords[i];
                    if (string.IsNullOrWhiteSpace(raw))
                    {
                        result[i] = null;
                        continue;
                    }

                    var parts = raw.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries);
                    for (int p = 0; p < parts.Length; p++)
                        parts[p] = (parts[p] ?? "").Trim();

                    result[i] = parts;
                }
                return result;
            }
        }
    }

    [Header("Steps (textId 기반)")]
    [SerializeField] private StepData[] steps;

    [Header("상단 가이드 텍스트")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextIdBefore = 0;
    [SerializeField] private int guideTextIdAfter = 0;
    [SerializeField] private int guideTextIdBetweenRounds = 0; // 마지막 스텝 제외, 중간 스텝 완료 시 표시

    protected override Text GuideText => guideText;
    protected override int GuideTextId_Before => guideTextIdBefore;
    protected override int GuideTextId_After => guideTextIdAfter;
    protected override int GuideTextId_BetweenRounds => guideTextIdBetweenRounds;

    [Header("Effect Controller")]
    [SerializeField] private Problem3_Step2_EffectController effectController;

    [Header("Carousel UI")]
    [SerializeField] private GameObject carouselRoot;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Text carouselText;
    [SerializeField] private Text carouselIndexText;
    [SerializeField] private Image optionImage;

    [Header("Mic UI")]
    [SerializeField] private GameObject micButtonRoot;
    [SerializeField] private MicRecordingIndicator micIndicator;
    [SerializeField] private GameObject recordingOverlay;

    [Header("Progress Dots (optional)")]
    [SerializeField] private GameObject[] progressDots;

    [Header("Next Buttons")]
    [SerializeField] private GameObject nextDialogButtonRoot; // "다음 대사" 버튼 루트

    [Header("Completion Gate (optional)")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("Options")]
    [SerializeField] private float rewriteDelay = 0.0f;

    protected override IRewriteStepData[] Steps => steps;
    protected override Problem3_Step2_EffectController EffectController => effectController;

    protected override GameObject CarouselRoot => carouselRoot;
    protected override Button PrevButton => prevButton;
    protected override Button NextButton => nextButton;
    protected override Text CarouselText => carouselText;
    protected override Text CarouselIndexText => carouselIndexText;
    protected override Image OptionImage => optionImage;

    protected override GameObject MicButtonRoot => micButtonRoot;
    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override GameObject RecordingOverlay => recordingOverlay;

    protected override GameObject[] ProgressDots => progressDots;

    protected override GameObject NextDialogButtonRoot => nextDialogButtonRoot;

    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float RewriteDelay => rewriteDelay;
}