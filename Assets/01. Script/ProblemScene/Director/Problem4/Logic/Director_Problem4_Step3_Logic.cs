using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem4 / Step3 ���� ���̽�.
/// - Q1, Q2 � ���� '�� / �ƴϿ�'�� ���ϴ� ����
/// - �ڽĿ��� ���� ������ + UI�� ����.
/// </summary>
public interface IYesNoQuestionData
{
    string QuestionId { get; }
    string MainText { get; }
    string SubText { get; }
    string SubmainText { get; }
    bool IsYesCorrect { get; }
}

public abstract class Director_Problem4_Step3_Logic : ProblemStepBase
{
    [Serializable]
    private class QuestionActionLog
    {
        public string questionId; // � ������ ���� ��������
        public string answer;     // "yes" �Ǵ� "no"
        public bool wasCorrect;   // ���� ����
    }

    [Serializable]
    private class AttemptBody
    {
        public QuestionActionLog[] actions;
    }

    // ==========================
    // �ڽĿ��� ������ �߻� ������Ƽ
    // ==========================

    [Header("���� ���� (�ڽ� ����)")]
    protected abstract IYesNoQuestionData[] Questions { get; }

    [Header("���� UI")]
    protected abstract GameObject QuestionRoot { get; }
    protected abstract Text MainTextLabel { get; }
    protected abstract Text SubTextLabel { get; }
    protected abstract Text SubmainTextLabel { get; }

    [Header("��ư")]
    protected abstract Button YesButton { get; }
    protected abstract Button NoButton { get; }

    [Header("���� �޽���")]
    protected abstract GameObject ErrorRoot { get; }
    protected abstract Text ErrorLabel { get; }
    protected abstract string DefaultErrorMessage { get; }
    protected abstract float ErrorShowDuration { get; }

    [Header("���� Echo UI")]
    protected abstract GameObject AnswerEchoRoot { get; }
    protected abstract Text AnswerEchoLabel { get; }
    protected abstract float AnswerEchoDuration { get; }

    [Header("�Ϸ� ����Ʈ")]
    protected abstract StepCompletionGate StepCompletionGate { get; }

    [Header("�ó����� ī��")]
    protected abstract GameObject ScenarioCardRoot { get; }
    protected abstract float ScenarioDisplayDuration { get; }

    [Header("버튼 이미지 (Echo와 반대 동작)")]
    protected abstract GameObject ButtonImageRoot { get; }

    [Header("마이크 STT (옵션)")]
    protected abstract MicRecordingIndicator MicIndicator { get; }

    // ==========================
    // ���� ����
    // ==========================

    private int _currentIndex;
    private bool _stepCompleted;
    private Coroutine _errorRoutine;
    private Coroutine _answerEchoRoutine;
    private readonly List<QuestionActionLog> _actionLogs = new List<QuestionActionLog>();


    // ==================================================
    // ProblemStepBase
    // ==================================================

    protected override void OnStepEnter()
    {
        var questions = Questions;

        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("[Problem4_Step3] questions �� ��� ����");

            if (MainTextLabel != null)
                MainTextLabel.text = "(������ �������� �ʾҽ��ϴ�)";
            if (SubTextLabel != null)
                SubTextLabel.text = "";

            if (StepCompletionGate != null)
                StepCompletionGate.ResetGate(1);

            return;
        }

        _currentIndex = 0;
        _stepCompleted = false;
        _actionLogs.Clear();

        var errorRoot = ErrorRoot;
        var answerEchoRoot = AnswerEchoRoot;

        if (errorRoot != null)
            errorRoot.SetActive(false);

        if (answerEchoRoot != null)
            answerEchoRoot.SetActive(false);

        if (_errorRoutine != null)
        {
            StopCoroutine(_errorRoutine);
            _errorRoutine = null;
        }

        if (_answerEchoRoutine != null)
        {
            StopCoroutine(_answerEchoRoutine);
            _answerEchoRoutine = null;
        }

        if (YesButton != null) YesButton.interactable = true;
        if (NoButton != null) NoButton.interactable = true;

        if (StepCompletionGate != null)
            StepCompletionGate.ResetGate(1);

        // 시나리오 카드 숨김
        if (ScenarioCardRoot != null)
            ScenarioCardRoot.SetActive(false);

        // 버튼 이미지 표시
        if (ButtonImageRoot != null)
            ButtonImageRoot.SetActive(true);

        // MicIndicator STT 이벤트 구독
        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnKeywordMatched += OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;
            mic.OnNoMatch += OnSTTNoMatch;
        }

        ApplyQuestionUI(_currentIndex);
    }

    protected override void OnStepExit()
    {
        if (_errorRoutine != null)
        {
            StopCoroutine(_errorRoutine);
            _errorRoutine = null;
        }

        if (_answerEchoRoutine != null)
        {
            StopCoroutine(_answerEchoRoutine);
            _answerEchoRoutine = null;
        }

        // MicIndicator 이벤트 구독 해제
        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;
        }
    }

    // ==================================================
    // UI ����
    // ==================================================

    private void ApplyQuestionUI(int index)
    {
        var questions = Questions;

        if (questions == null || index < 0 || index >= questions.Length)
        {
            Debug.LogWarning("[Problem4_Step3] ApplyQuestionUI: �߸��� �ε��� " + index);
            return;
        }

        var q = questions[index];

        if (QuestionRoot != null)
            QuestionRoot.SetActive(true);

        if (MainTextLabel != null)
            MainTextLabel.text = q.MainText;

        if (SubTextLabel != null)
            SubTextLabel.text = q.SubText;

        if (SubmainTextLabel != null)
            SubmainTextLabel.text = q.SubmainText;

        if (ErrorRoot != null)
            ErrorRoot.SetActive(false);

        if (AnswerEchoRoot != null)
            AnswerEchoRoot.SetActive(false);
    }

    // ==================================================
    // ��ư Ŭ�� ó��
    // ==================================================

    public void OnClickYes()
    {
        HandleAnswer(true);
    }

    public void OnClickNo()
    {
        HandleAnswer(false);
    }

    private void HandleAnswer(bool isYes)
    {
        if (_stepCompleted) return;

        var questions = Questions;
        if (questions == null || questions.Length == 0) return;

        if (_currentIndex < 0 || _currentIndex >= questions.Length)
            return;

        var q = questions[_currentIndex];
        bool isCorrect = (isYes == q.IsYesCorrect);
        string answerStr = isYes ? "yes" : "no";

        _actionLogs.Add(new QuestionActionLog
        {
            questionId = q.QuestionId,
            answer = answerStr,
            wasCorrect = isCorrect
        });

        if (isCorrect)
        {
            if (_answerEchoRoutine != null)
            {
                StopCoroutine(_answerEchoRoutine);
                _answerEchoRoutine = null;
            }
            _answerEchoRoutine = StartCoroutine(AnswerEchoAndNext(isYes));
        }
        else
        {
            ShowError(DefaultErrorMessage);
        }
    }

    private IEnumerator AnswerEchoAndNext(bool isYes)
    {
        if (YesButton != null) YesButton.interactable = false;
        if (NoButton != null) NoButton.interactable = false;

        if (AnswerEchoLabel != null)
            AnswerEchoLabel.text = isYes ? "예" : "아니오";

        // Echo 표시 시 버튼 이미지 숨김
        if (AnswerEchoRoot != null)
            AnswerEchoRoot.SetActive(true);

        if (ButtonImageRoot != null)
            ButtonImageRoot.SetActive(false);

        if (AnswerEchoDuration > 0f)
            yield return new WaitForSeconds(AnswerEchoDuration);

        // Echo 숨김 시 버튼 이미지 표시
        if (AnswerEchoRoot != null)
            AnswerEchoRoot.SetActive(false);

        if (ButtonImageRoot != null)
            ButtonImageRoot.SetActive(true);

        var questions = Questions;

        if (_currentIndex >= questions.Length - 1)
        {
            CompleteStep();
        }
        else
        {
            _currentIndex++;
            ApplyQuestionUI(_currentIndex);

            if (YesButton != null) YesButton.interactable = true;
            if (NoButton != null) NoButton.interactable = true;
        }

        _answerEchoRoutine = null;
    }

    // ==================================================
    // ���� �޽���
    // ==================================================

    private void ShowError(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            msg = DefaultErrorMessage;

        if (ErrorLabel != null)
            ErrorLabel.text = msg;

        if (ErrorRoot != null)
            ErrorRoot.SetActive(true);

        if (_errorRoutine != null)
            StopCoroutine(_errorRoutine);

        if (ErrorShowDuration > 0f)
            _errorRoutine = StartCoroutine(HideErrorAfterDelay());
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(ErrorShowDuration);

        if (ErrorRoot != null)
            ErrorRoot.SetActive(false);

        _errorRoutine = null;
    }

    // ==================================================
    // STT 이벤트 핸들러
    // ==================================================

    /// <summary>
    /// STT 키워드 매칭 성공 시 호출
    /// index 0 = "예", index 1 = "아니오"
    /// </summary>
    protected void OnSTTKeywordMatched(int matchedIndex)
    {
        Debug.Log($"[Problem4_Step3] STT 매칭: index={matchedIndex}");

        if (_stepCompleted) return;

        // index 0 = "예" → true, index 1 = "아니오" → false
        bool isYes = (matchedIndex == 0);
        HandleAnswer(isYes);
    }

    /// <summary>
    /// STT 매칭 실패 시 호출
    /// </summary>
    protected void OnSTTNoMatch(string sttResult)
    {
        Debug.Log($"[Problem4_Step3] STT 매칭 실패: {sttResult}");
        // 매칭 실패 시에는 아무것도 하지 않음 - 사용자가 다시 녹음하거나 버튼 클릭 가능
    }

    // ==================================================
    // �Ϸ� ó�� + Attempt ����
    // ==================================================

    private void CompleteStep()
    {
        if (YesButton != null) YesButton.interactable = false;
        if (NoButton != null) NoButton.interactable = false;

        SaveRebuttalAttempt();

        Debug.Log("[Problem4_Step3] 반박 질문 완료 - 시나리오 카드 등장");
        _stepCompleted = true;

        // 시나리오 카드 등장 후 대기 → 다음 스텝
        StartCoroutine(ShowScenarioCardAndComplete());
    }

    private IEnumerator ShowScenarioCardAndComplete()
    {
        // 시나리오 카드 등장
        if (ScenarioCardRoot != null)
            ScenarioCardRoot.SetActive(true);

        // 대기
        if (ScenarioDisplayDuration > 0f)
            yield return new WaitForSeconds(ScenarioDisplayDuration);

        // 다음 스텝으로
        if (StepCompletionGate != null)
            StepCompletionGate.MarkOneDone();
    }

    private void SaveRebuttalAttempt()
    {
        var body = new AttemptBody
        {
            actions = _actionLogs.ToArray()
        };

        // ProblemStepBase�� ������ DB ���� ȣ��
        SaveAttempt(body);
    }
}
