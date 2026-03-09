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

        [Tooltip("이 질문에서 '네' 버튼이 정답이면 true, '아니오'가 정답이면 false")]
        public bool isYesCorrect;

        public string QuestionId => questionId;
        public string MainText => ProblemRuntime.L(mainTextId);
        public bool IsYesCorrect => isYesCorrect;
    }

    [Header("질문 데이터")]
    [SerializeField] private QuestionData[] questions;

    [Header("질문 UI")]
    [SerializeField] private Text mainTextLabel;

    [Header("버튼")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("오답 피드백")]
    [SerializeField] private int errorTextId;

    [Header("버튼 이미지")]
    [SerializeField] private GameObject buttonImageRoot;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate stepCompletionGate;

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem4_Step3_EffectController effectController;

    // ==========================
    // 베이스 클래스 프로퍼티 override
    // ==========================

    protected override IYesNoQuestionData[] Questions => questions;

    protected override Text MainTextLabel => mainTextLabel;

    protected override Button YesButton => yesButton;
    protected override Button NoButton => noButton;

    protected override int ErrorTextId => errorTextId;

    protected override GameObject ButtonImageRoot => buttonImageRoot;

    protected override StepCompletionGate StepCompletionGate => stepCompletionGate;

    protected override Problem4_Step3_EffectController EffectController => effectController;
}
