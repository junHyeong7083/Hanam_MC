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

    [Header("옵션 버튼 이미지 (인덱스별)")]
    [SerializeField] private Image[] optionButtonImages;       // 버튼의 Image 컴포넌트
    [SerializeField] private Sprite[] optionNormalSprites;     // 기본 상태 스프라이트
    [SerializeField] private Sprite[] optionSelectedSprites;   // 선택 상태 스프라이트
    [SerializeField] private GameObject[] optionSelectedMarkers; // 선택 시 체크 마커

    [Header("넘버링 이미지 (인덱스별)")]
    [SerializeField] private Image[] numberingImages;          // 넘버링 Image 컴포넌트
    [SerializeField] private Sprite[] numberingNormalSprites;  // 기본 상태 넘버링 스프라이트
    [SerializeField] private Sprite[] numberingSelectedSprites; // 선택 상태 넘버링 스프라이트

    [Header("상단 진행도 점들 (옵션)")]
    [SerializeField] private GameObject[] progressDots;

    [Header("하단 네비게이션 버튼")]
    [SerializeField] private GameObject nextProblemButton;  // 다음문제 버튼 (중간 단계용)
    [SerializeField] private GameObject nextStepButton;     // 다음스텝 버튼 (마지막 단계용)

    [Header("완료 게이트 (옵션)")]
    [SerializeField] private StepCompletionGate completionGate;

    // ==== 베이스로 값 전달을 위한 override 프로퍼티 ====

    protected override IRewriteStepData[] Steps => steps;

    protected override Problem3_Step2_EffectController EffectController => effectController;

    protected override Button[] OptionButtons => optionButtons;
    protected override Text[] OptionLabels => optionLabels;

    protected override Image[] OptionButtonImages => optionButtonImages;
    protected override Sprite[] OptionNormalSprites => optionNormalSprites;
    protected override Sprite[] OptionSelectedSprites => optionSelectedSprites;
    protected override GameObject[] OptionSelectedMarkers => optionSelectedMarkers;

    protected override Image[] NumberingImages => numberingImages;
    protected override Sprite[] NumberingNormalSprites => numberingNormalSprites;
    protected override Sprite[] NumberingSelectedSprites => numberingSelectedSprites;

    protected override GameObject[] ProgressDots => progressDots;

    protected override GameObject NextProblemButton => nextProblemButton;
    protected override GameObject NextStepButton => nextStepButton;

    protected override StepCompletionGate CompletionGate => completionGate;
}
