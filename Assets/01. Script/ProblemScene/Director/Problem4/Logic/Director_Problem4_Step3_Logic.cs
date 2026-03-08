using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public interface IYesNoQuestionData
{
    string QuestionId { get; }
    string MainText { get; }
    string SubText { get; }
    int SubTextId { get; }
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
    protected abstract Text HanamTextLabel { get; }

    protected abstract Button YesButton { get; }
    protected abstract Button NoButton { get; }

    protected abstract string DefaultErrorMessage { get; }
    protected abstract float ErrorShowDuration { get; }

    protected abstract GameObject ButtonImageRoot { get; }

    protected abstract MicRecordingIndicator MicIndicator { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    protected virtual Problem4_Step3_EffectController EffectController => null;
    protected virtual TTSTrigger HanamTTSTrigger => null;
    protected virtual int CompleteTextId => 0;


    // ==========================
    // 내부 상태
    // ==========================

    private bool _interactionLocked = true;
    private int _currentIndex;
    private bool _stepCompleted;
    private Coroutine _errorRoutine;
    private string _savedHanamText;
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
            if (HanamTextLabel != null)
                HanamTextLabel.text = "";

            return;
        }

        _currentIndex = 0;
        _stepCompleted = false;
        _actionLogs.Clear();

        if (_errorRoutine != null)
        {
            StopCoroutine(_errorRoutine);
            _errorRoutine = null;
        }

        if (YesButton != null) YesButton.interactable = true;
        if (NoButton != null) NoButton.interactable = true;

        // 버튼 이미지 표시
        if (ButtonImageRoot != null)
            ButtonImageRoot.SetActive(true);

        // MicIndicator 활성화 + STT 이벤트 구독
        var mic = MicIndicator;
        if (mic != null)
        {
            mic.gameObject.SetActive(true);

            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnKeywordMatched += OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;
            mic.OnNoMatch += OnSTTNoMatch;
        }

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

        if (_errorRoutine != null)
        {
            StopCoroutine(_errorRoutine);
            _errorRoutine = null;
        }

        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;
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

        if (MainTextLabel != null)
            MainTextLabel.text = q.MainText;

        if (HanamTextLabel != null)
        {
            HanamTextLabel.text = q.SubText;
            _savedHanamText = q.SubText;
        }

        // TTS 재생
        if (q.SubTextId > 0 && SoundManager.Instance != null)
            SoundManager.Instance.PlayTTS(q.SubTextId);
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
            ShowError(DefaultErrorMessage);
        }
    }

private void OnCorrectAnswer()
    {
        if (YesButton != null) YesButton.interactable = false;
        if (NoButton != null) NoButton.interactable = false;

        var mic = MicIndicator;
        if (mic != null)
        {
            var micBtn = mic.GetComponent<Button>();
            if (micBtn != null) micBtn.interactable = false;
            var hover = mic.GetComponent<ButtonHover>();
            if (hover != null) hover.SetInteractable(false);
        }

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

                        var mic2 = MicIndicator;
                        if (mic2 != null)
                        {
                            var micBtn2 = mic2.GetComponent<Button>();
                            if (micBtn2 != null) micBtn2.interactable = true;
                            var hover2 = mic2.GetComponent<ButtonHover>();
                            if (hover2 != null) hover2.SetInteractable(true);
                        }
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

        var mic = MicIndicator;
        if (mic != null)
        {
            var micBtn = mic.GetComponent<Button>();
            if (micBtn != null) micBtn.interactable = true;
            var hover = mic.GetComponent<ButtonHover>();
            if (hover != null) hover.SetInteractable(true);
        }
    }

    // ==================================================
    // 에러 메시지 (HanamText 에 일시 표시 후 복원)
    // ==================================================

private void ShowError(string msg)
    {
        if (string.IsNullOrEmpty(msg))
            msg = DefaultErrorMessage;

        if (HanamTextLabel != null)
            HanamTextLabel.text = msg;

        // 다음 질문 전환 시 ApplyQuestionUI()에서 자동 갱신됨
        if (_errorRoutine != null)
        {
            StopCoroutine(_errorRoutine);
            _errorRoutine = null;
        }
    }

private IEnumerator RestoreHanamTextAfterDelay()
    {
        yield return new WaitForSeconds(ErrorShowDuration);

        if (HanamTextLabel != null)
        {
            // ttsButton의 TTSTrigger를 일시 비활성화하여 텍스트 복원 시 TTS 재발생 방지
            var tts = HanamTTSTrigger;
            if (tts != null) tts.enabled = false;

            HanamTextLabel.text = _savedHanamText;

            if (tts != null)
            {
                yield return null; // LateUpdate 한 프레임 건너뜀
                tts.enabled = true;
            }
        }

        _errorRoutine = null;
    }

    // ==================================================
    // STT 이벤트 핸들러
    // ==================================================

    protected void OnSTTKeywordMatched(int matchedIndex)
    {
        Debug.Log($"[Problem4_Step3] STT 매칭: index={matchedIndex}");

        if (_stepCompleted) return;

        bool isYes = (matchedIndex == 0);
        HandleAnswer(isYes);
    }

    protected void OnSTTNoMatch(string sttResult)
    {
        Debug.Log($"[Problem4_Step3] STT 매칭 실패: {sttResult}");
    }

    // ==================================================
    // 완료 처리
    // ==================================================

private void CompleteStep()
    {
        if (YesButton != null) YesButton.interactable = false;
        if (NoButton != null) NoButton.interactable = false;

        // MicBtn: 활성화 유지하되 터치만 차단
        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;

            var micBtn = mic.GetComponent<Button>();
            if (micBtn != null) micBtn.interactable = false;

            var hover = mic.GetComponent<ButtonHover>();
            if (hover != null) hover.SetInteractable(false);
        }

        // 완료 텍스트 표시 (TTSTrigger 비활성화하여 중복 TTS 방지)
        if (HanamTextLabel != null && CompleteTextId > 0)
        {
            var tts = HanamTTSTrigger;
            if (tts != null) tts.enabled = false;
            HanamTextLabel.text = ProblemRuntime.L(CompleteTextId);
        }

        // 완료 TTS 직접 재생
        if (CompleteTextId > 0 && SoundManager.Instance != null)
            SoundManager.Instance.PlayTTS(CompleteTextId);

        SaveRebuttalAttempt();

        Debug.Log("[Problem4_Step3] 반박 질문 완료");
        _stepCompleted = true;

        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
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
