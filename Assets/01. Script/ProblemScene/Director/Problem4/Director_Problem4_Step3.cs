using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem4_Step3 - 문제4 스텝3의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 질문 데이터(QuestionData), UI 라벨, 네/아니오 버튼,
///         에러 textId, 완료 게이트, 이펙트 컨트롤러를 바인딩한다.
///         실제 반박 질문 로직은 부모(Director_Problem4_Step3_Logic)에 있다.
///         QuestionData 내부 클래스가 IYesNoQuestionData를 구현하여 textId 기반으로 텍스트를 가져온다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제4 / 스텝3 (마무리 - 네/아니오 반박 질문)
/// 【부모 클래스】 Director_Problem4_Step3_Logic → ProblemStepBase
/// </summary>
public class Director_Problem4_Step3 : Director_Problem4_Step3_Logic
{
    /// <summary>
    /// 개별 네/아니오 질문 데이터. IYesNoQuestionData 인터페이스를 구현한다.
    /// textId 기반으로 CSV에서 질문 텍스트를 가져온다.
    /// </summary>
    [Serializable]
    public class QuestionData : IYesNoQuestionData
    {
        [Tooltip("질문 ID (로그용, 예: Q1, Q2 등)")]
        public string questionId;         // 질문 고유 ID (로그/식별용)

        [Tooltip("DataTable textId - 메인 지문 (카드에 표시)")]
        public int mainTextId;            // CSV에서 가져올 질문 텍스트 ID

        [Tooltip("이 질문에서 '네' 버튼이 정답이면 true, '아니오'가 정답이면 false")]
        public bool isYesCorrect;         // true="네"가 정답, false="아니오"가 정답

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
