using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 네/아니오 질문 데이터 인터페이스.
/// 질문 ID, 메인 텍스트, 정답이 "네"인지 여부를 정의한다.
/// </summary>
public interface IYesNoQuestionData
{
    string QuestionId { get; }   // 질문 고유 ID (로그용, 예: Q1)
    string MainText { get; }     // 질문 메인 텍스트 (카드에 표시)
    bool IsYesCorrect { get; }   // true="네"가 정답, false="아니오"가 정답
}

/// <summary>
/// Director_Problem4_Step3_Logic - 문제4 스텝3의 비즈니스 로직 베이스 클래스.
///
/// 【역할】 "반박 질문" 활동을 담당한다. 여러 질문(Q1~Q3 등)을 순서대로 표시하고,
///         각 질문에 대해 "네" 또는 "아니오"로 답하도록 한다.
///         정답이면 퇴장 애니메이션 → 다음 질문 등장, 오답이면 에러 메시지 표시.
///         모든 질문 완료 시 결과를 DB에 저장하고 DialogueSequencer 완료 텍스트를 표시한다.
///         필름 카드 등장/퇴장 애니메이션은 Problem4_Step3_EffectController에 위임한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 측.
/// 【문제/스텝】 Director 테마 / 문제4 / 스텝3 (마무리 - 네/아니오 반박 질문)
/// 【부모 클래스】 ProblemStepBase → OnStepEnter()/OnStepExit()
/// 【참조하는 곳】 Director_Problem4_Step3 (Binder 자식 클래스)
/// 【참조되는 곳】 IYesNoQuestionData (질문 데이터 인터페이스),
///               Problem4_Step3_EffectController (등장/퇴장 애니메이션),
///               DialogueSequencer (대사/에러), StepCompletionGate (완료 판정)
/// 【흐름】 스텝 진입 → 첫 질문 표시 + 등장 애니메이션 → enter 대사 →
///         네/아니오 선택 → 정답: 퇴장→다음 질문 등장 / 오답: 에러 메시지 →
///         마지막 질문 정답 → DB 저장 → completed 대사 → 다음 스텝
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

    #region Abstract Properties

    protected abstract IYesNoQuestionData[] Questions { get; }

    protected abstract Text MainTextLabel { get; }

    protected abstract Button YesButton { get; }
    protected abstract Button NoButton { get; }

    protected abstract int ErrorTextId { get; }

    protected abstract GameObject ButtonImageRoot { get; }

    protected abstract StepCompletionGate StepCompletionGate { get; }

    #endregion

    #region Virtual Config

    protected virtual Problem4_Step3_EffectController EffectController => null;

    #endregion

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;


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
