using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem4 / Step3
/// - 인스펙터에서 질문 데이터 + UI 레퍼런스를 갖는 바인더.
/// - 비즈니스 로직은 Director_Problem4_Step3_Logic(부모)에 위임.
/// </summary>
public class Director_Problem4_Step3 : Director_Problem4_Step3_Logic
{
    [Serializable]
    public class QuestionData : IYesNoQuestionData
    {
        [Tooltip("질문 ID (로그용, 예: Q1, Q2 등)")]
        public string questionId;

        [Tooltip("DataTable textId - 메인 지문 (카드에 표시)")]
        public int mainTextId;

        [Tooltip("DataTable textId - 안내 텍스트 (HanamBox에 표시, 0이면 비움)")]
        public int subTextId;

        [Tooltip("이 질문에서 '네' 버튼이 정답이면 true, '아니오'가 정답이면 false")]
        public bool isYesCorrect;

        public string QuestionId => questionId;
        public string MainText => ProblemRuntime.L(mainTextId);
        public string SubText => subTextId > 0 ? ProblemRuntime.L(subTextId) : "";
        public int SubTextId => subTextId;
        public bool IsYesCorrect => isYesCorrect;
    }

    [Header("질문 데이터")]
    [SerializeField] private QuestionData[] questions;

    [Header("질문 UI")]
    [SerializeField] private Text mainTextLabel;
    [SerializeField] private Text hanamTextLabel;

    [Header("버튼")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("에러 메시지")]
    [SerializeField] private int defaultErrorTextId;
    [SerializeField] private float errorShowDuration = 1f;

    [Header("버튼 이미지")]
    [SerializeField] private GameObject buttonImageRoot;

    [Header("하남 버튼 (완료 시 활성화)")]
    [SerializeField] private GameObject hanamBtn;

    [Header("마이크 STT (옵션)")]
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem4_Step3_EffectController effectController;

    [Header("완료 텍스트 설정")]
    [SerializeField] private int completeTextId;
    [SerializeField] private TTSTrigger hanamTTSTrigger;

    // ==========================
    // 베이스 클래스 프로퍼티 override
    // ==========================

    protected override IYesNoQuestionData[] Questions => questions;

    protected override Text MainTextLabel => mainTextLabel;
    protected override Text HanamTextLabel => hanamTextLabel;

    protected override Button YesButton => yesButton;
    protected override Button NoButton => noButton;

    protected override string DefaultErrorMessage => ProblemRuntime.L(defaultErrorTextId);
    protected override float ErrorShowDuration => errorShowDuration;

    protected override GameObject ButtonImageRoot => buttonImageRoot;

    protected override GameObject HanamBtn => hanamBtn;

    protected override MicRecordingIndicator MicIndicator => micIndicator;

    protected override Problem4_Step3_EffectController EffectController => effectController;

    protected override int CompleteTextId => completeTextId;
    protected override TTSTrigger HanamTTSTrigger => hanamTTSTrigger;
}
