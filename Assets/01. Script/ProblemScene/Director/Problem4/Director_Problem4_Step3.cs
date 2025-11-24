using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem4 / Step3
/// - Q1, Q2 두 개의 질문에 대해 '네 / 아니오'로 답하는 스텝
/// - 각 질문은 mainText + subText + submainText 한 쌍으로 구성
/// - 각 질문마다 정답(Yes/No)을 설정
/// - 오답일 경우 errorRoot를 잠깐 보여줌
/// - 정답일 경우 잠깐 AnswerEcho UI에 사용자가 누른 답을 보여줌
/// - 모든 질문을 올바르게 끝내면 Attempt를 저장하고 Gate 완료
/// </summary>
public class Director_Problem4_Step3 : ProblemStepBase
{
    [Serializable]
    public class QuestionData
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
    }

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
        if (questions == null || questions.Length == 0)
        {
            Debug.LogWarning("[Problem4_Step3] questions 가 비어 있음");
            if (mainTextLabel != null)
                mainTextLabel.text = "(질문이 설정되지 않았습니다)";
            if (subTextLabel != null)
                subTextLabel.text = "";
            if (stepCompletionGate != null)
                stepCompletionGate.ResetGate(1);
            return;
        }

        _currentIndex = 0;
        _stepCompleted = false;
        _actionLogs.Clear();

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

        if (yesButton != null) yesButton.interactable = true;
        if (noButton != null) noButton.interactable = true;

        if (stepCompletionGate != null)
            stepCompletionGate.ResetGate(1);

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
        if (questions == null || index < 0 || index >= questions.Length)
        {
            Debug.LogWarning("[Problem4_Step3] ApplyQuestionUI: 잘못된 인덱스 " + index);
            return;
        }

        var q = questions[index];

        if (questionRoot != null)
            questionRoot.SetActive(true);

        if (mainTextLabel != null)
            mainTextLabel.text = q.mainText;

        if (subTextLabel != null)
            subTextLabel.text = q.subText;

        if (submainTextLabel != null)
            submainTextLabel.text = q.submainText;

        if (errorRoot != null)
            errorRoot.SetActive(false);

        // 새로운 질문으로 넘어올 때, 이전 echo는 끔
        if (answerEchoRoot != null)
            answerEchoRoot.SetActive(false);
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
        //Debug.Log("HandleAnswer");
        if (_stepCompleted) return;
      //  Debug.Log("_stepCompleted");
        if (questions == null || questions.Length == 0) return;
        //Debug.Log("questions");

        if (_currentIndex < 0 || _currentIndex >= questions.Length)
            return;
      // Debug.Log("_currentIndex");
        var q = questions[_currentIndex];
        bool isCorrect = (isYes == q.isYesCorrect);
        string answerStr = isYes ? "yes" : "no";

        // 로그 쌓기
        _actionLogs.Add(new QuestionActionLog
        {
            questionId = q.questionId,
            answer = answerStr,
            wasCorrect = isCorrect
        });

        if (isCorrect)
        {
            // 정답이면 잠깐 AnswerEcho 에 "네" / "아니오" 보여주기
            if (_answerEchoRoutine != null)
            {
                StopCoroutine(_answerEchoRoutine);
                _answerEchoRoutine = null;
            }
            _answerEchoRoutine = StartCoroutine(AnswerEchoAndNext(isYes));
        }
        else
        {
            // 오답: 에러 메시지 출력
            ShowError(defaultErrorMessage);
        }
    }

    private IEnumerator AnswerEchoAndNext(bool isYes)
    {
        // 버튼 잠깐 막기
        if (yesButton != null) yesButton.interactable = false;
        if (noButton != null) noButton.interactable = false;

        if (answerEchoLabel != null)
            answerEchoLabel.text = isYes ? "네" : "아니오";

        if (answerEchoRoot != null)
            answerEchoRoot.SetActive(true);

        // 잠깐 보여줌
        if (answerEchoDuration > 0f)
            yield return new WaitForSeconds(answerEchoDuration);

        // Echo 끄기
        if (answerEchoRoot != null)
            answerEchoRoot.SetActive(false);

        // 마지막 질문인지 체크
        if (_currentIndex >= questions.Length - 1)
        {
            CompleteStep();
        }
        else
        {
            _currentIndex++;
            ApplyQuestionUI(_currentIndex);

            // 다음 질문에서 다시 버튼 활성화
            if (yesButton != null) yesButton.interactable = true;
            if (noButton != null) noButton.interactable = true;
        }

        _answerEchoRoutine = null;
    }

    // ==================================================
    // 에러 메시지
    // ==================================================

    private void ShowError(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            msg = defaultErrorMessage;

        if (errorLabel != null)
            errorLabel.text = msg;

        if (errorRoot != null)
            errorRoot.SetActive(true);

        if (_errorRoutine != null)
            StopCoroutine(_errorRoutine);

        if (errorShowDuration > 0f)
            _errorRoutine = StartCoroutine(HideErrorAfterDelay());
    }

    private IEnumerator HideErrorAfterDelay()
    {
        yield return new WaitForSeconds(errorShowDuration);

        if (errorRoot != null)
            errorRoot.SetActive(false);

        _errorRoutine = null;
    }

    // ==================================================
    // 완료 처리 + Attempt 저장
    // ==================================================


    private void CompleteStep()
    {
        // 버튼 막기
        if (yesButton != null) yesButton.interactable = false;
        if (noButton != null) noButton.interactable = false;

        SaveRebuttalAttempt();

        if (stepCompletionGate != null)
            stepCompletionGate.MarkOneDone();

        Debug.Log("[Problem4_Step3] 반박 질문 스텝 완료");
    }

    private void SaveRebuttalAttempt()
    {
        var body = new AttemptBody
        {
            actions = _actionLogs.ToArray()
        };

        SaveAttempt(body); // ProblemStepBase에 구현된 DB 저장 호출
    }
}
