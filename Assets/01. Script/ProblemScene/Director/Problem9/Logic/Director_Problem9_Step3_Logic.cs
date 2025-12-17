using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem9 / Step3 로직 베이스
/// - 나-전달법 3단계 음성 녹음 연습
/// - 3개 서브스텝: situation(상황) → feeling(감정) → request(바람)
/// - 마이크는 별도 프리팹에서 처리, 녹음 완료 시 OnRecordingComplete() 호출
/// - 모두 완료 시 complete 화면 표시
/// </summary>
public abstract class Director_Problem9_Step3_Logic : ProblemStepBase
{
    #region Data Classes

    public enum PracticePhase
    {
        Situation,  // 상황 설명
        Feeling,    // 감정 전달
        Request,    // 바람 표현
        Complete    // 완료 화면
    }

    [Serializable]
    public class PracticeStepData
    {
        public string id;               // situation, feeling, request
        public string title;            // 상황, 감정, 바람
        [TextArea(2, 4)]
        public string question;         // 조감독 질문
        [TextArea(2, 4)]
        public string placeholder;      // 예시 텍스트
    }

    // DB 저장용 DTO
    [Serializable]
    public class PracticeAttemptDto
    {
        public string stepKey;
        public PracticeInputDto situationInput;
        public PracticeInputDto feelingInput;
        public PracticeInputDto requestInput;
        public string combinedDialogue;     // 최종 합쳐진 대사
        public DateTime completedAt;
    }

    [Serializable]
    public class PracticeInputDto
    {
        public string phase;                // situation, feeling, request
        public string recordedText;         // STT 결과
        public float recordingDuration;     // 녹음 시간(초)
    }

    #endregion

    #region Abstract Properties

    [Header("===== 연습 단계 데이터 =====")]
    protected abstract PracticeStepData[] PracticeSteps { get; }

    [Header("===== 화면 루트 =====")]
    /// <summary>녹음 연습 화면 (situation, feeling, request 공용)</summary>
    protected abstract GameObject RecordingPracticeRoot { get; }

    [Header("===== 녹음 화면 UI =====")]
    /// <summary>조감독 질문 텍스트</summary>
    protected abstract Text QuestionText { get; }

    /// <summary>단계 제목 (상황, 감정, 바람)</summary>
    protected abstract Text StepIndicatorTitle { get; }

    /// <summary>사용자 입력 표시 영역 (STT 결과 표시용)</summary>
    protected abstract GameObject UserInputDisplayRoot { get; }
    protected abstract Text UserInputDisplayText { get; }

    [Header("===== 진행도 이미지 (3개) =====")]
    /// <summary>진행 단계별 이미지 (index 0, 1, 2)</summary>
    protected abstract Image[] ProgressImages { get; }

    [Header("===== 완료 화면 UI (Gate의 completeRoot 내부) =====")]
    /// <summary>최종 합쳐진 대사 표시 (Gate의 completeRoot 안에 있는 Text)</summary>
    protected abstract Text CombinedDialogueText { get; }

    [Header("===== 완료 게이트 =====")]
    /// <summary>completeRoot에 완료 화면 연결, 버튼은 인스펙터에서 직접 NextStep 연결</summary>
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    /// <summary>녹음 완료 후 다음 단계 전환 대기 시간</summary>
    protected virtual float DelayAfterRecording => 0.5f;

    #endregion

    // 내부 상태
    private PracticePhase _currentPhase;

    // 각 단계별 녹음 데이터
    private PracticeInputDto _situationInput;
    private PracticeInputDto _feelingInput;
    private PracticeInputDto _requestInput;

    // 현재 Phase 외부 접근용
    public PracticePhase CurrentPhase => _currentPhase;

    #region Step Lifecycle

    protected override void OnStepEnter()
    {
        _currentPhase = PracticePhase.Situation;

        // 녹음 데이터 초기화
        _situationInput = new PracticeInputDto { phase = "situation" };
        _feelingInput = new PracticeInputDto { phase = "feeling" };
        _requestInput = new PracticeInputDto { phase = "request" };

        // Gate 초기화
        var gate = CompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);

        // 초기 화면 설정
        ShowPhase(PracticePhase.Situation);
    }

    #endregion

    #region UI Control

    private void ShowPhase(PracticePhase phase)
    {
        _currentPhase = phase;

        bool isComplete = phase == PracticePhase.Complete;

        if (!isComplete)
        {
            if (RecordingPracticeRoot != null) RecordingPracticeRoot.SetActive(true);
            ApplyPhaseToUI(phase);
            if (UserInputDisplayRoot != null) UserInputDisplayRoot.SetActive(false);
        }
        else
        {
            // 완료 화면으로 전환
            if (RecordingPracticeRoot != null) RecordingPracticeRoot.SetActive(false);
            ApplyCompleteUI();

            // Gate 완료 → completeRoot 자동 표시
            var gate = CompletionGateRef;
            if (gate != null)
                gate.MarkOneDone();
        }

        UpdateProgressImages();
    }

    private void ApplyPhaseToUI(PracticePhase phase)
    {
        int stepIndex = (int)phase;
        var steps = PracticeSteps;
        if (steps == null || stepIndex >= steps.Length) return;

        var stepData = steps[stepIndex];
        if (stepData == null) return;

        // 질문 텍스트
        if (QuestionText != null)
            QuestionText.text = stepData.question;

        // 단계 제목
        if (StepIndicatorTitle != null)
            StepIndicatorTitle.text = stepData.title;
    }

    private void ApplyCompleteUI()
    {
        // 합쳐진 대사 생성
        string combined = $"{_situationInput?.recordedText} {_feelingInput?.recordedText} {_requestInput?.recordedText}";

        if (CombinedDialogueText != null)
            CombinedDialogueText.text = combined;

        // DB 저장
        SaveAttempt(new PracticeAttemptDto
        {
            stepKey = context != null ? context.CurrentStepKey : null,
            situationInput = _situationInput,
            feelingInput = _feelingInput,
            requestInput = _requestInput,
            combinedDialogue = combined,
            completedAt = DateTime.UtcNow
        });
    }

    private void UpdateProgressImages()
    {
        var images = ProgressImages;
        if (images == null || images.Length < 3) return;

        int currentIndex = (int)_currentPhase;
        if (_currentPhase == PracticePhase.Complete) currentIndex = 2; // 마지막 이미지 유지

        // 현재 단계 이미지만 표시
        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
                images[i].gameObject.SetActive(i == currentIndex);
        }
    }

    #endregion

    #region Public API (마이크 프리팹에서 호출)

    /// <summary>
    /// 녹음 완료 시 외부(마이크 프리팹)에서 호출
    /// </summary>
    /// <param name="recordedText">STT 결과 텍스트</param>
    /// <param name="duration">녹음 시간(초)</param>
    public void OnRecordingComplete(string recordedText, float duration)
    {
        if (_currentPhase == PracticePhase.Complete) return;

        // 녹음 데이터 저장
        SaveRecordingData(duration, recordedText);

        // 입력 표시
        if (UserInputDisplayRoot != null)
            UserInputDisplayRoot.SetActive(true);

        if (UserInputDisplayText != null)
            UserInputDisplayText.text = recordedText;

        // 다음 단계로 전환
        StartCoroutine(TransitionToNextPhase());
    }

    /// <summary>
    /// 현재 단계의 placeholder 텍스트 반환
    /// </summary>
    public string GetCurrentPlaceholder()
    {
        var steps = PracticeSteps;
        int index = (int)_currentPhase;
        if (steps == null || index >= steps.Length) return "";
        return steps[index]?.placeholder ?? "";
    }

    #endregion

    #region Internal

    private void SaveRecordingData(float duration, string text)
    {
        var input = new PracticeInputDto
        {
            phase = _currentPhase.ToString().ToLower(),
            recordedText = text,
            recordingDuration = duration
        };

        switch (_currentPhase)
        {
            case PracticePhase.Situation:
                _situationInput = input;
                break;
            case PracticePhase.Feeling:
                _feelingInput = input;
                break;
            case PracticePhase.Request:
                _requestInput = input;
                break;
        }
    }

    private IEnumerator TransitionToNextPhase()
    {
        yield return new WaitForSeconds(DelayAfterRecording);

        switch (_currentPhase)
        {
            case PracticePhase.Situation:
                ShowPhase(PracticePhase.Feeling);
                break;
            case PracticePhase.Feeling:
                ShowPhase(PracticePhase.Request);
                break;
            case PracticePhase.Request:
                ShowPhase(PracticePhase.Complete);
                break;
        }
    }

    #endregion
}
