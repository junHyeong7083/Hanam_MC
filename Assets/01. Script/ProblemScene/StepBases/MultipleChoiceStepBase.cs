using UnityEngine;
using UnityEngine.UI;
public abstract class MultipleChoiceStepBase<TQuestion> : ProblemStepBase
{
    [Header("공통 UI")]
    [SerializeField] protected Text questionLabel;
    [SerializeField] protected Button[] optionButtons;
    [SerializeField] protected Text[] optionLabels;

    [Header("색상 설정")]
    [SerializeField] protected Color optionNormalColor = Color.white;
    [SerializeField] protected Color optionCorrectColor = Color.green;
    [SerializeField] protected Color optionWrongColor = Color.red;

    [Header("완료 게이트")]
    [SerializeField] protected StepCompletionGate completionGate;

    protected int _currentQuestionIndex;

    protected bool _stepCompleted;

    protected abstract int QuestionCount { get; }
    protected abstract TQuestion GetQuestion(int index);
    protected abstract void ApplyQuestionUI(int index, TQuestion q);
    protected abstract int GetCorrectOptionIndex(TQuestion q);
    protected abstract void OnQuestionAttempted(TQuestion q, int optionIndex, bool isCorrect);
    protected abstract void OnAllQuestionsCompleted();

    protected override void OnStepEnter()
    {
        _stepCompleted = false;
        _currentQuestionIndex = 0;

        if (completionGate != null)
            completionGate.ResetGate(QuestionCount);

        ShowQuestion(_currentQuestionIndex);
    }

    protected virtual void ShowQuestion(int index)
    {
        var q = GetQuestion(index);
        ApplyQuestionUI(index, q);

        // 버튼 리스너 초기화
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int idx = i;
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnClickOption(idx));
            optionButtons[i].interactable = true;
        }

        // 색상 초기화
        ResetOptionVisual();
    }

    protected virtual void ResetOptionVisual()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            var colors = optionButtons[i].colors;
            colors.normalColor = optionNormalColor;
            optionButtons[i].colors = colors;
        }
    }

    public void OnClickOption(int optionIndex)
    {
        if (_stepCompleted) return;
        if (_currentQuestionIndex < 0 || _currentQuestionIndex >= QuestionCount) return;

        var q = GetQuestion(_currentQuestionIndex);
        int correctIndex = GetCorrectOptionIndex(q);
        bool isCorrect = (optionIndex == correctIndex);

        OnQuestionAttempted(q, optionIndex, isCorrect);

        if (isCorrect)
            HandleCorrect(optionIndex);
        else
            HandleWrong(optionIndex);
    }

    protected virtual void HandleCorrect(int optionIndex)
    {
        // 공통: 정답 색상 + 버튼 비활성
        var btn = optionButtons[optionIndex];
        var colors = btn.colors;
        colors.normalColor = optionCorrectColor;
        btn.colors = colors;

        for (int i = 0; i < optionButtons.Length; i++)
            optionButtons[i].interactable = false;

        if (completionGate != null)
            completionGate.MarkOneDone();

        GoNextQuestionOrFinish();
    }

    protected virtual void HandleWrong(int optionIndex)
    {
        var btn = optionButtons[optionIndex];
        var colors = btn.colors;
        colors.normalColor = optionWrongColor;
        btn.colors = colors;

        // 여기서 StepErrorPanel 사용해서 힌트 띄우기 가능
    }

    protected void GoNextQuestionOrFinish()
    {
        if (_currentQuestionIndex >= QuestionCount - 1)
        {
            _stepCompleted = true;
            OnAllQuestionsCompleted();
        }
        else
        {
            _currentQuestionIndex++;
            ShowQuestion(_currentQuestionIndex);
        }
    }
}
