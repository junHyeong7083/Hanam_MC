using System;
using UnityEngine;
using UnityEngine.UI;

public class Director_Problem3_Step2 : Director_Problem3_Step2_Logic
{
    [Serializable]
    private class StepData : IRewriteStepData
    {
        public int id;
        public int originalTextId;
        public int rewrittenTextId;

        public int[] optionTextIds;          // 캐러셀 옵션들
        public string[] optionKeywords;      // 선택: 각 옵션에 대응하는 "키1|키2" 문자열

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

    protected override Text GuideText => guideText;
    protected override int GuideTextId_Before => guideTextIdBefore;
    protected override int GuideTextId_After => guideTextIdAfter;

    [Header("Effect Controller")]
    [SerializeField] private Problem3_Step2_EffectController effectController;

    [Header("Carousel UI")]
    [SerializeField] private GameObject carouselRoot;
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Text carouselText;
    [SerializeField] private Text carouselIndexText;

    [Header("Mic UI")]
    [SerializeField] private GameObject micButtonRoot;
    [SerializeField] private MicRecordingIndicator micIndicator;
    [SerializeField] private GameObject recordingOverlay;

    [Header("Progress Dots (optional)")]
    [SerializeField] private GameObject[] progressDots;

    [Header("Next Buttons")]
    [SerializeField] private GameObject nextDialogButtonRoot; // "다음 대사" 버튼 루트
    [SerializeField] private GameObject nextStepButtonRoot;   // 마지막 완료 후 "다음 스텝" 루트(선택)

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

    protected override GameObject MicButtonRoot => micButtonRoot;
    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override GameObject RecordingOverlay => recordingOverlay;

    protected override GameObject[] ProgressDots => progressDots;

    protected override GameObject NextDialogButtonRoot => nextDialogButtonRoot;
    protected override GameObject NextStepButtonRoot => nextStepButtonRoot;

    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float RewriteDelay => rewriteDelay;
}