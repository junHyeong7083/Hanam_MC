using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MultipleChoiceStepBase - 4지선다(객관식) 문제 스텝의 제네릭 베이스 클래스
///
/// 【역할】 N개의 객관식 문제를 순차적으로 표시하고, 사용자의 선택이 정답인지 판별한다.
///          정답 시 다음 문제로 이동하고 completionGate에 완료를 알린다.
///          오답 시 HandleWrong()을 호출하여 시각적 피드백을 제공한다.
///          모든 문제 완료 시 OnAllQuestionsCompleted()를 호출한다.
/// 【참조하는 곳】 각 Problem Director의 Step2/Step3 등에서 상속하여 사용
///                (예: Director_Problem1_Step2 : MultipleChoiceStepBase&lt;QuestionData&gt;)
/// 【참조되는 곳】 StepCompletionGate (완료 관리)
/// 【흐름】 OnStepEnter() → ResetGate(QuestionCount) → ShowQuestion(0)
///          → 사용자 클릭 → OnClickOption() → 정답이면 HandleCorrect() → GoNextQuestionOrFinish()
///          → 모두 정답 시 OnAllQuestionsCompleted()
///
/// ※ TQuestion: 문제 데이터 타입. 자식 클래스에서 정의 (예: struct QuestionData { int textId; int correctIndex; })
/// </summary>
/// <typeparam name="TQuestion">문제 데이터 타입. GetQuestion(index)이 반환하는 타입.</typeparam>
public abstract class MultipleChoiceStepBase<TQuestion> : ProblemStepBase
{
    [Header("선택 UI")]
    [SerializeField] protected Text questionLabel;      // 문제 텍스트를 표시할 Text UI
    [SerializeField] protected Button[] optionButtons;  // 선택지 버튼 배열 (보통 4개)
    [SerializeField] protected Text[] optionLabels;     // 선택지 텍스트 배열 (optionButtons와 1:1 매핑)

    [Header("색상 설정")]
    [SerializeField] protected Color optionNormalColor = Color.white;   // 선택지 기본 색상
    [SerializeField] protected Color optionCorrectColor = Color.green;  // 정답 선택 시 색상
    [SerializeField] protected Color optionWrongColor = Color.red;      // 오답 선택 시 색상

    [Header("완료 게이트")]
    [SerializeField] protected StepCompletionGate completionGate; // 문제 완료 추적 게이트

    protected int _currentQuestionIndex; // 현재 표시 중인 문제의 인덱스 (0-based)
    protected bool _stepCompleted;       // 모든 문제를 완료했는지 여부

    // ====== 자식 클래스에서 반드시 구현해야 하는 추상 멤버들 ======

    /// <summary>전체 문제 수 (예: 4문제면 4 반환)</summary>
    protected abstract int QuestionCount { get; }

    /// <summary>index번째 문제 데이터를 반환한다.</summary>
    /// <param name="index">문제 인덱스 (0-based)</param>
    protected abstract TQuestion GetQuestion(int index);

    /// <summary>index번째 문제의 UI를 갱신한다. (문제 텍스트, 선택지 텍스트 등)</summary>
    /// <param name="index">문제 인덱스 (0-based)</param>
    /// <param name="q">문제 데이터</param>
    protected abstract void ApplyQuestionUI(int index, TQuestion q);

    /// <summary>해당 문제의 정답 선택지 인덱스를 반환한다. (0-based)</summary>
    /// <param name="q">문제 데이터</param>
    protected abstract int GetCorrectOptionIndex(TQuestion q);

    /// <summary>문제에 대한 사용자의 시도가 발생했을 때 호출된다. (정답/오답 무관)</summary>
    /// <param name="q">문제 데이터</param>
    /// <param name="optionIndex">사용자가 선택한 선택지 인덱스</param>
    /// <param name="isCorrect">정답 여부</param>
    protected abstract void OnQuestionAttempted(TQuestion q, int optionIndex, bool isCorrect);

    /// <summary>모든 문제를 정답으로 완료했을 때 호출된다.</summary>
    protected abstract void OnAllQuestionsCompleted();

    /// <summary>
    /// 스텝 진입 시: 상태 초기화 → completionGate 리셋 → 첫 번째 문제 표시
    /// </summary>
    protected override void OnStepEnter()
    {
        _stepCompleted = false;
        _currentQuestionIndex = 0;

        if (completionGate != null)
            completionGate.ResetGate(QuestionCount);

        ShowQuestion(_currentQuestionIndex);
    }

    /// <summary>
    /// 지정 인덱스의 문제를 UI에 표시한다.
    /// 버튼 리스너를 재설정하고 선택지 색상을 초기화한다.
    /// </summary>
    /// <param name="index">표시할 문제 인덱스 (0-based)</param>
    protected virtual void ShowQuestion(int index)
    {
        var q = GetQuestion(index);
        ApplyQuestionUI(index, q);

        // 버튼 리스너 초기화: 기존 리스너 제거 후 새로 등록
        for (int i = 0; i < optionButtons.Length; i++)
        {
            int idx = i; // 클로저 캡처용 로컬 변수
            optionButtons[i].onClick.RemoveAllListeners();
            optionButtons[i].onClick.AddListener(() => OnClickOption(idx));
            optionButtons[i].interactable = true;
        }

        // 선택지 색상 초기화
        ResetOptionVisual();
    }

    /// <summary>모든 선택지 버튼의 색상을 기본(optionNormalColor)으로 리셋한다.</summary>
    protected virtual void ResetOptionVisual()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            var colors = optionButtons[i].colors;
            colors.normalColor = optionNormalColor;
            optionButtons[i].colors = colors;
        }
    }

    /// <summary>
    /// 선택지 버튼 클릭 시 호출되는 콜백.
    /// 정답 여부를 판별하고 HandleCorrect() 또는 HandleWrong()을 호출한다.
    /// </summary>
    /// <param name="optionIndex">클릭된 선택지 인덱스 (0-based)</param>
    public void OnClickOption(int optionIndex)
    {
        if (_stepCompleted) return;
        if (_currentQuestionIndex < 0 || _currentQuestionIndex >= QuestionCount) return;

        var q = GetQuestion(_currentQuestionIndex);
        int correctIndex = GetCorrectOptionIndex(q);
        bool isCorrect = (optionIndex == correctIndex);

        // 자식 클래스에 시도 결과 알림 (DB 저장 등)
        OnQuestionAttempted(q, optionIndex, isCorrect);

        if (isCorrect)
            HandleCorrect(optionIndex);
        else
            HandleWrong(optionIndex);
    }

    /// <summary>
    /// 정답 처리: 정답 색상 표시 → 모든 버튼 비활성화 → completionGate 진행 → 다음 문제로
    /// </summary>
    /// <param name="optionIndex">정답 선택지 인덱스</param>
    protected virtual void HandleCorrect(int optionIndex)
    {
        // 정답 선택지에 정답 색상 적용
        var btn = optionButtons[optionIndex];
        var colors = btn.colors;
        colors.normalColor = optionCorrectColor;
        btn.colors = colors;

        // 모든 버튼 비활성화 (추가 클릭 방지)
        for (int i = 0; i < optionButtons.Length; i++)
            optionButtons[i].interactable = false;

        // completionGate에 완료 1건 알림
        if (completionGate != null)
            completionGate.MarkOneDone();

        GoNextQuestionOrFinish();
    }

    /// <summary>
    /// 오답 처리: 오답 색상만 표시. StepErrorPanel 연동은 자식 클래스에서 추가 구현 가능.
    /// </summary>
    /// <param name="optionIndex">오답 선택지 인덱스</param>
    protected virtual void HandleWrong(int optionIndex)
    {
        var btn = optionButtons[optionIndex];
        var colors = btn.colors;
        colors.normalColor = optionWrongColor;
        btn.colors = colors;

        // 필요 시 StepErrorPanel을 사용하여 피드백 표시 가능
    }

    /// <summary>
    /// 다음 문제로 이동하거나, 마지막 문제였으면 전체 완료 처리를 한다.
    /// </summary>
    protected void GoNextQuestionOrFinish()
    {
        if (_currentQuestionIndex >= QuestionCount - 1)
        {
            // 마지막 문제까지 완료
            _stepCompleted = true;
            OnAllQuestionsCompleted();
        }
        else
        {
            // 다음 문제로 이동
            _currentQuestionIndex++;
            ShowQuestion(_currentQuestionIndex);
        }
    }
}
