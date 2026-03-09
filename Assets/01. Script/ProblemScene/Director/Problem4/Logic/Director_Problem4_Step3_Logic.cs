using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IYesNoQuestionData
{
    string QuestionId { get; }
    string MainText { get; }
    bool IsYesCorrect { get; }
}

/// <summary>
/// Problem4 / Step3 로직 베이스.
/// - Q1~Q3 순서대로 '네 / 아니오' 로 답하는 반박 질문
/// - 필름 애니메이션: Right→Center 등장, Center→Left 퇴장
/// - 에러 메시지는 HanamText 에 일시 표시 후 복원
/// - 모든 질문 완료 시 DialogueSequencer 완료 텍스트 표시
/// </summary>
public abstract class Director_Problem4_Step3_Logic : ProblemStepBase
{
    [Serializable]
    private class QuestionActionLog
    {
        public string questionId;
        public string answer;
        public bool wasCorrect;
    }

    [Serializable]
    private class AttemptBody
    {
        public QuestionActionLog[] actions;
    }

    // ==========================
    // 자식에서 제공할 추상 프로퍼티
    // ==========================

    protected abstract IYesNoQuestionData[] Questions { get; }

    protected abstract Text MainTextLabel { get; }

    protected abstract Button YesButton { get; }
    protected abstract Button NoButton { get; }

    protected abstract int ErrorTextId { get; }

    protected abstract GameObject ButtonImageRoot { get; }

    protected abstract StepCompletionGate StepCompletionGate { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    protected virtual Problem4_Step3_EffectController EffectController => null;


    // ==========================
    // 내부 상태
    // ==========================

    private bool _interactionLocked = true;
    private int _currentIndex;
    private bool _stepCompleted;
    private readonly List<QuestionActionLog> _actionLogs = new List<QuestionActionLog>();

    // ==================================================
    // ProblemStepBase
    // ==================================================

    protected override void OnStepEnter()
    {
        var questions = Questions;

        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("[Problem4_Step3] questions 배열 비어있음");

            if (MainTextLabel != null)
                MainTextLabel.text = "";

            return;
        }

        _currentIndex = 0;
        _stepCompleted = false;
        _actionLogs.Clear();

        if (StepCompletionGate != null)
            StepCompletionGate.ResetGate(1);

        if (YesButton != null) YesButton.interactable = true;
        if (NoButton != null) NoButton.interactable = true;

        // 버튼 이미지 표시
        if (ButtonImageRoot != null)
            ButtonImageRoot.SetActive(true);

        // 첫 질문 텍스트 설정 + 등장 애니메이션
        ApplyQuestionUI(_currentIndex);

        var effect = EffectController;
        if (effect != null)
            effect.PlayQuestionEnter();

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    protected override void OnStepExit()
    {
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;
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

        if (MainTextLabel != null)
            MainTextLabel.text = q.MainText;
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
        if (_interactionLocked) return;
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
            OnCorrectAnswer();
        }
        else
        {
            if (dialogueSequencer != null && ErrorTextId > 0)
                dialogueSequencer.SetText(ErrorTextId);
        }
    }

    private void OnCorrectAnswer()
    {
        if (YesButton != null) YesButton.interactable = false;
        if (NoButton != null) NoButton.interactable = false;

        var questions = Questions;
        var effect = EffectController;

        if (effect != null)
        {
            effect.PlayQuestionExit(() =>
            {
                if (_currentIndex >= questions.Length - 1)
                {
                    CompleteStep();
                }
                else
                {
                    _currentIndex++;
                    ApplyQuestionUI(_currentIndex);

                    effect.PlayQuestionEnter(() =>
                    {
                        if (YesButton != null) YesButton.interactable = true;
                        if (NoButton != null) NoButton.interactable = true;
                    });
                }
            });
        }
        else
        {
            if (_currentIndex >= questions.Length - 1)
            {
                CompleteStep();
            }
            else
            {
                _currentIndex++;
                StartCoroutine(NextQuestionFallback());
            }
        }
    }

    private IEnumerator NextQuestionFallback()
    {
        yield return new WaitForSeconds(0.5f);

        ApplyQuestionUI(_currentIndex);

        if (YesButton != null) YesButton.interactable = true;
        if (NoButton != null) NoButton.interactable = true;
    }

    // ==================================================
    // 완료 처리
    // ==================================================

    private void CompleteStep()
    {
        if (YesButton != null) YesButton.interactable = false;
        if (NoButton != null) NoButton.interactable = false;

        SaveRebuttalAttempt();

        _stepCompleted = true;

        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();

        if (StepCompletionGate != null)
            StepCompletionGate.MarkOneDone();
    }

    private void SaveRebuttalAttempt()
    {
        var body = new AttemptBody
        {
            actions = _actionLogs.ToArray()
        };

        SaveAttempt(body);
    }
}
