using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem4 / Step2
/// - 필름 컷 분류 (생각 vs 사실)
/// - 애니메이션은 EffectController에 위임
/// </summary>
public class Director_Problem4_Step2 : Director_Problem4_Step2_Logic
{
    [Serializable]
    public class FilmCutData : IFilmCutData
    {
        [Tooltip("컷 ID")]
        public string cutID;

        [TextArea]
        [Tooltip("화면에 표시할 컷 문장")]
        public string text;

        [Tooltip("생각 컷이면 true, 사실이면 false")]
        public bool isThinking;

        string IFilmCutData.CutId => cutID;
        string IFilmCutData.Text => text;
        bool IFilmCutData.IsThinking => isThinking;
    }

    [Header("컷 데이터")]
    [SerializeField] private FilmCutData[] filmCuts;

    [Header("필름 카드 UI")]
    [SerializeField] private Text filmSentenceLabel;
    [SerializeField] private Text filmIndexLabel;

    [Header("하단 버튼")]
    [SerializeField] private Button cutBtn;
    [SerializeField] private Button passBtn;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate stepCompletionGate;

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem4_Step2_EffectController effectController;

    [Header("오답 표시 UI")]
    [SerializeField] private GameObject errorRoot;

    [Header("완료 시 UI")]
    [SerializeField] private GameObject hideObjectOnComplete;
    [SerializeField] private RectTransform showImageOnComplete;
    [SerializeField] private Text completionLabel;
    [TextArea]
    [SerializeField] private string completionText;
    [SerializeField] private float completionDelayDuration = 4f;

    // ====== 베이스 주입용 override 프로퍼티 ======

    protected override IFilmCutData[] FilmCuts => filmCuts;

    protected override Text FilmSentenceLabel => filmSentenceLabel;
    protected override Text FilmIndexLabel => filmIndexLabel;

    protected override Button CutBtn => cutBtn;
    protected override Button PassBtn => passBtn;

    protected override StepCompletionGate StepCompletionGate => stepCompletionGate;

    protected override Problem4_Step2_EffectController EffectController => effectController;

    protected override GameObject ErrorRoot => errorRoot;

    protected override GameObject HideObjectOnComplete => hideObjectOnComplete;
    protected override RectTransform ShowImageOnComplete => showImageOnComplete;
    protected override Text CompletionLabel => completionLabel;
    protected override string CompletionText => completionText;
    protected override float CompletionDelayDuration => completionDelayDuration;
}
