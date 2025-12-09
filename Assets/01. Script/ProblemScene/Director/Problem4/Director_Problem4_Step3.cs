using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem4 / Step3
/// - �ν����Ϳ��� ���� ������ + UI ������ ��� �ִ� ����.
/// - ���� ������ Director_Problem4_Step3_Logic(�θ�)�� ����.
/// </summary>
public class Director_Problem4_Step3 : Director_Problem4_Step3_Logic
{
    [Serializable]
    public class QuestionData : IYesNoQuestionData
    {
        [Tooltip("���� ID (�α׿�, ��: Q1, Q2 ��)")]
        public string questionId;

        [TextArea]
        [Tooltip("���� ��ܿ� ������ ���� ���� (��: '���� �ٽô� ��������...')")]
        public string mainText;

        [Tooltip("���� �Ʒ� �� ��¥�� ���� ����")]
        public string submainText;

        [TextArea]
        [Tooltip("�ڽ� �ȿ� ���� ���� ���� (��: �ݹ� ���� / ���ο� �ó����� ����)")]
        public string subText;

        [Tooltip("�� �������� '��' ��ư�� �����̸� true, '�ƴϿ�'�� �����̸� false")]
        public bool isYesCorrect;

        // �������̽� ����
        public string QuestionId => questionId;
        public string MainText => mainText;
        public string SubText => subText;
        public string SubmainText => submainText;
        public bool IsYesCorrect => isYesCorrect;
    }

    [Header("���� ����")]
    [SerializeField] private QuestionData[] questions;

    [Header("���� UI")]
    [SerializeField] private GameObject questionRoot;
    [SerializeField] private Text mainTextLabel;       // ��� ���� �ؽ�Ʈ
    [SerializeField] private Text subTextLabel;        // �ڽ� �� ����/�ó����� �ؽ�Ʈ
    [SerializeField] private Text submainTextLabel;    // ���� �Ʒ� �� ��

    [Header("��ư")]
    [SerializeField] private Button yesButton;
    [SerializeField] private Button noButton;

    [Header("���� �޽���")]
    [SerializeField] private GameObject errorRoot;
    [SerializeField] private Text errorLabel;
    [SerializeField] private string defaultErrorMessage = "�ٽ� �����غ����?";
    [SerializeField] private float errorShowDuration = 1f;

    [Header("���� Echo UI")]
    [SerializeField] private GameObject answerEchoRoot;   // "��", "�ƴϿ�" ��� ������ �г�
    [SerializeField] private Text answerEchoLabel;        // ���� �ؽ�Ʈ ��
    [SerializeField] private float answerEchoDuration = 0.8f;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate stepCompletionGate;

    [Header("시나리오 카드")]
    [SerializeField] private GameObject scenarioCardRoot;
    [SerializeField] private float scenarioDisplayDuration = 3f;

    // ==========================
    // 베이스 클래스 프로퍼티 override
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

    protected override GameObject ScenarioCardRoot => scenarioCardRoot;
    protected override float ScenarioDisplayDuration => scenarioDisplayDuration;
}


