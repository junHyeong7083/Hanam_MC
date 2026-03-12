using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem4_Step2 - 문제4 스텝2의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 필름 컷 데이터(FilmCutData), UI 라벨, Cut/Pass 버튼,
///         완료 게이트, 이펙트 컨트롤러, 에러 textId, 완료 시 UI 전환 설정을 바인딩한다.
///         실제 필름 컷 분류 로직은 부모(Director_Problem4_Step2_Logic)에 있다.
///         FilmCutData 내부 클래스가 IFilmCutData를 구현하여 textId 기반으로 텍스트를 가져온다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제4 / 스텝2 (메인 활동 - 필름 컷 편집)
/// 【부모 클래스】 Director_Problem4_Step2_Logic → ProblemStepBase
/// </summary>
public class Director_Problem4_Step2 : Director_Problem4_Step2_Logic
{
    /// <summary>
    /// 개별 필름 컷 데이터. IFilmCutData 인터페이스를 구현하여
    /// textId 기반으로 CSV에서 컷 텍스트를 가져온다.
    /// </summary>
    [Serializable]
    public class FilmCutData : IFilmCutData
    {
        [Tooltip("컷 ID")]
        public string cutID;              // 컷 고유 ID (로그용)

        [Tooltip("DataTable textId")]
        public int textId;                // CSV에서 가져올 컷 텍스트 ID

        [Tooltip("생각 컷이면 true, 사실이면 false")]
        public bool isThinking;           // true=편집 대상(생각), false=통과(사실)

        string IFilmCutData.CutId => cutID;
        string IFilmCutData.Text => ProblemRuntime.L(textId);
        bool IFilmCutData.IsThinking => isThinking;
    }

    [Header("컷 데이터")]
    [SerializeField] private FilmCutData[] filmCuts;

    [Header("필름 카드 UI")]
    [SerializeField] private Text filmSentenceLabel;

    [Header("하단 버튼")]
    [SerializeField] private Button cutBtn;
    [SerializeField] private Button passBtn;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate stepCompletionGate;

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem4_Step2_EffectController effectController;

    [Header("오답 피드백")]
    [SerializeField] private int errorTextId;

    [Header("완료 시 UI")]
    [SerializeField] private GameObject hideObjectOnComplete;
    [SerializeField] private RectTransform showImageOnComplete;
    [SerializeField] private float completionDelayDuration = 4f;

    // ====== 베이스 주입용 override 프로퍼티 ======

    protected override IFilmCutData[] FilmCuts => filmCuts;

    protected override Text FilmSentenceLabel => filmSentenceLabel;
    protected override Text FilmIndexLabel => null;

    protected override Button CutBtn => cutBtn;
    protected override Button PassBtn => passBtn;

    protected override StepCompletionGate StepCompletionGate => stepCompletionGate;

    protected override Problem4_Step2_EffectController EffectController => effectController;

    protected override int ErrorTextId => errorTextId;

    protected override GameObject HideObjectOnComplete => hideObjectOnComplete;
    protected override RectTransform ShowImageOnComplete => showImageOnComplete;
    protected override float CompletionDelayDuration => completionDelayDuration;
}
