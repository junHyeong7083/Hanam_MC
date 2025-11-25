using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem4 / Step3 로직 베이스.
/// - Q1, Q2 등에 대해 '네 / 아니오'로 답하는 스텝
/// - 자식에서 질문 데이터 + UI를 주입.
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
        public string questionId; // 어떤 질문에 대한 선택인지
        public string answer;     // "yes" 또는 "no"
        public bool wasCorrect;   // 정답 여부
    }

    [Serializable]
    private class AttemptBody
    {
        public QuestionActionLog[] actions;
    }

    // ==========================
    // 자식에서 주입할 추상 프로퍼티
    // ==========================

    [Header("질문 설정 (자식 주입)")]
    protected abstract IYesNoQuestionData[] Questions { get; }

    [Header("질문 UI")]
    protected abstract GameObject QuestionRoot { get; }
    protected abstract Text MainTextLabel { get; }
    protected abstract Text SubTextLabel { get; }
    protected abstract Text SubmainTextLabel { get; }

    [Header("버튼")]
    protected abstract Button YesButton { get; }
    protected abstract Button NoButton { get; }

    [Header("오류 메시지")]
    protected abstract GameObject ErrorRoot { get; }
    protected abstract Text ErrorLabel { get; }
    protected abstract string DefaultErrorMessage { get; }
    protected abstract float ErrorShowDuration { get; }

    [Header("정답 Echo UI")]
    protected abstract GameObject AnswerEchoRoot { get; }
    protected abstract Text AnswerEchoLabel { get; }
    protected abstract float AnswerEchoDuration { get; }

    [Header("완료 게이트")]
    protected abstract StepCompletionGate StepCompletionGate { get; }


    // ==========================
    // 내부 상태
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
            Debug.LogWarning("[Problem4_Step3] questions 가 비어 있음");

            if (MainTextLabel != null)
                MainTextLabel.text = "(질문이 설정되지 않았습니다)";
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
    }

    // ==================================================
    // UI 갱신
    // ==================================================

    private void ApplyQuestionUI(int index)
    {
        var questions = Questions;

        if (questions == null || index < 0 || index >= questions.Length)
        {
            Debug.LogWarning("[Problem4_Step3] ApplyQuestionUI: 잘못된 인덱스 " + index);
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
    // 버튼 클릭 처리
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
            AnswerEchoLabel.text = isYes ? "네" : "아니오";

        if (AnswerEchoRoot != null)
            AnswerEchoRoot.SetActive(true);

        if (AnswerEchoDuration > 0f)
            yield return new WaitForSeconds(AnswerEchoDuration);

        if (AnswerEchoRoot != null)
            AnswerEchoRoot.SetActive(false);

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
    // 에러 메시지
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
    // 완료 처리 + Attempt 저장
    // ==================================================

    private void CompleteStep()
    {
        if (YesButton != null) YesButton.interactable = false;
        if (NoButton != null) NoButton.interactable = false;

        SaveRebuttalAttempt();

        if (StepCompletionGate != null)
            StepCompletionGate.MarkOneDone();

        Debug.Log("[Problem4_Step3] 반박 질문 스텝 완료");
        _stepCompleted = true;
    }

    private void SaveRebuttalAttempt()
    {
        var body = new AttemptBody
        {
            actions = _actionLogs.ToArray()
        };

        // ProblemStepBase에 구현된 DB 저장 호출
        SaveAttempt(body);
    }
}
