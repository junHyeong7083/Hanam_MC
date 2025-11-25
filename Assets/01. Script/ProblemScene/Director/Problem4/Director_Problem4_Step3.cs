using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem4 / Step3
/// - 인스펙터에서 질문 데이터 + UI 참조만 들고 있는 래퍼.
/// - 실제 로직은 Director_Problem4_Step3_Logic(부모)에 있음.
/// </summary>
public class Director_Problem4_Step3 : Director_Problem4_Step3_Logic
{
    [Serializable]
    public class QuestionData : IYesNoQuestionData
    {
        [Tooltip("질문 ID (로그용, 예: Q1, Q2 등)")]
        public string questionId;

        [TextArea]
        [Tooltip("질문 상단에 나오는 메인 문장 (예: '이제 다시는 흑백논리에...')")]
        public string mainText;

        [Tooltip("메인 아래 한 줄짜리 서브 문장")]
        public string submainText;

        [TextArea]
        [Tooltip("박스 안에 들어가는 서브 문장 (예: 반박 질문 / 새로운 시나리오 문장)")]
        public string subText;

        [Tooltip("이 질문에서 '네' 버튼이 정답이면 true, '아니오'가 정답이면 false")]
        public bool isYesCorrect;

        // 인터페이스 구현
        public string QuestionId => questionId;
        public string MainText => mainText;
        public string SubText => subText;
        public string SubmainText => submainText;
        public bool IsYesCorrect => isYesCorrect;
    }

    [Header("질문 설정")]
    [SerializeField] private QuestionData[] questions;

    [Header("질문 UI")]
    [SerializeField] private GameObject questionRoot;
    [SerializeField] private Text mainTextLabel;       // 상단 설명 텍스트
    [SerializeField] private Text subTextLabel;        // 박스 안 질문/시나리오 텍스트
    [SerializeField] private Text submainTextLabel;    // 메인 아래 한 줄

    [Header("버튼")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("오류 메시지")]
    [SerializeField] private GameObject errorRoot;
    [SerializeField] private Text errorLabel;
    [SerializeField] private string defaultErrorMessage = "다시 생각해볼까요?";
    [SerializeField] private float errorShowDuration = 1f;

    [Header("정답 Echo UI")]
    [SerializeField] private GameObject answerEchoRoot;   // "네", "아니오" 잠깐 보여줄 패널
    [SerializeField] private Text answerEchoLabel;        // 실제 텍스트 라벨
    [SerializeField] private float answerEchoDuration = 0.8f;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate stepCompletionGate;

    // ==========================
    // 베이스에 값 주입용 override
    // ==========================

    protected override IYesNoQuestionData[] Questions => questions;

    protected override GameObject QuestionRoot => questionRoot;
    protected override Text MainTextLabel => mainTextLabel;
    protected override Text SubTextLabel => subTextLabel;
    protected override Text SubmainTextLabel => submainTextLabel;

    protected override Button YesButton => yesButton;
    protected override Button NoButton => noButton;

    protected override GameObject ErrorRoot => errorRoot;
    protected override Text ErrorLabel => errorLabel;
    protected override string DefaultErrorMessage => defaultErrorMessage;
    protected override float ErrorShowDuration => errorShowDuration;

    protected override GameObject AnswerEchoRoot => answerEchoRoot;
    protected override Text AnswerEchoLabel => answerEchoLabel;
    protected override float AnswerEchoDuration => answerEchoDuration;

    protected override StepCompletionGate StepCompletionGate => stepCompletionGate;
}


