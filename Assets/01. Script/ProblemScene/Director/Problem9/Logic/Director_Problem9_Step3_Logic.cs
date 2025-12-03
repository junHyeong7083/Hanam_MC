using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem9 / Step3 로직 베이스
/// - 나-전달법 3단계 음성 녹음 연습
/// - 3개 서브스텝: situation(상황) → feeling(감정) → request(바람)
/// - 각 서브스텝마다 마이크 버튼 클릭으로 녹음
/// - 모두 완료 시 complete 화면 표시
///
/// [TODO] STT 기능 추후 추가 예정
/// - 현재는 마이크 버튼 2번 클릭으로 녹음 시뮬레이션
/// - 1번째 클릭: 녹음 시작
/// - 2번째 클릭: 녹음 완료 → 다음 단계
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
        public string emoji;            // 📍, 💭, 🎯
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
        public string recordedText;         // [TODO] STT 결과 (추후)
        public float recordingDuration;     // 녹음 시간(초)
    }

    [Serializable]
    public class ProgressUI
    {
        public GameObject stepRoot;         // 단계 표시 루트
        public Image circleImage;           // 원형 이미지
        public Text numberText;             // 숫자 또는 체크 표시
        public GameObject checkIcon;        // 완료 체크 아이콘 (선택)
        public GameObject connectorLine;    // 다음 단계 연결선 (선택)
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

    /// <summary>단계 표시 (📍 상황, 💭 감정, 🎯 바람)</summary>
    protected abstract Text StepIndicatorEmoji { get; }
    protected abstract Text StepIndicatorTitle { get; }

    /// <summary>마이크 버튼</summary>
    protected abstract Button MicButton { get; }

    /// <summary>마이크 버튼 이미지 (색상 변경용)</summary>
    protected abstract Image MicButtonImage { get; }

    /// <summary>녹음 상태 텍스트 ("마이크를 클릭해서 말해주세요" / "듣고 있어요...")</summary>
    protected abstract Text RecordingStatusText { get; }

    /// <summary>사용자 입력 표시 영역 (STT 결과 표시용, 추후)</summary>
    protected abstract GameObject UserInputDisplayRoot { get; }
    protected abstract Text UserInputDisplayText { get; }

    [Header("===== 진행도 UI =====")]
    protected abstract ProgressUI[] ProgressIndicators { get; }

    [Header("===== 완료 화면 UI (Gate의 completeRoot 내부) =====")]
    /// <summary>최종 합쳐진 대사 표시 (Gate의 completeRoot 안에 있는 Text)</summary>
    protected abstract Text CombinedDialogueText { get; }

    [Header("===== 완료 게이트 =====")]
    /// <summary>completeRoot에 완료 화면 연결, 버튼은 인스펙터에서 직접 NextStep 연결</summary>
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    /// <summary>녹음 버튼 기본 색상</summary>
    protected virtual Color MicNormalColor => new Color(1f, 0.54f, 0.24f); // #FF8A3D

    /// <summary>녹음 중 버튼 색상</summary>
    protected virtual Color MicRecordingColor => new Color(0.94f, 0.27f, 0.27f); // Red

    /// <summary>녹음 완료 후 다음 단계 전환 대기 시간</summary>
    protected virtual float DelayAfterRecording => 0.5f;

    #endregion

    // 내부 상태
    private PracticePhase _currentPhase;
    private bool _isRecording;
    private float _recordingStartTime;

    // 각 단계별 녹음 데이터
    private PracticeInputDto _situationInput;
    private PracticeInputDto _feelingInput;
    private PracticeInputDto _requestInput;

    #region Step Lifecycle

    protected override void OnStepEnter()
    {
        _currentPhase = PracticePhase.Situation;
        _isRecording = false;

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
        UpdateProgressIndicators();
        RegisterListeners();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();
        RemoveListeners();
    }

    #endregion

    #region UI Control

    private void ShowPhase(PracticePhase phase)
    {
        _currentPhase = phase;
        _isRecording = false;

        bool isComplete = phase == PracticePhase.Complete;

        if (!isComplete)
        {
            if (RecordingPracticeRoot != null) RecordingPracticeRoot.SetActive(true);
            ApplyPhaseToUI(phase);
            ResetMicButton();
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

        UpdateProgressIndicators();
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

        // 단계 표시
        if (StepIndicatorEmoji != null)
            StepIndicatorEmoji.text = stepData.emoji;

        if (StepIndicatorTitle != null)
            StepIndicatorTitle.text = stepData.title;

        // 상태 텍스트
        if (RecordingStatusText != null)
            RecordingStatusText.text = "마이크를 클릭해서 말해주세요";
    }

    private void ApplyCompleteUI()
    {
        // 합쳐진 대사 생성
        // [TODO] STT 결과로 대체 예정 (현재는 placeholder 사용)
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

    private string GetPlaceholder(int index)
    {
        var steps = PracticeSteps;
        if (steps == null || index >= steps.Length) return "";
        return steps[index]?.placeholder ?? "";
    }

    private void UpdateProgressIndicators()
    {
        var indicators = ProgressIndicators;
        if (indicators == null) return;

        int currentIndex = (int)_currentPhase;
        if (_currentPhase == PracticePhase.Complete) currentIndex = 3;

        for (int i = 0; i < indicators.Length; i++)
        {
            var indicator = indicators[i];
            if (indicator == null) continue;

            bool isCompleted = i < currentIndex;
            bool isCurrent = i == currentIndex && _currentPhase != PracticePhase.Complete;

            // 색상/상태 업데이트
            if (indicator.circleImage != null)
            {
                if (isCompleted)
                    indicator.circleImage.color = new Color(0.13f, 0.77f, 0.33f); // Green
                else if (isCurrent)
                    indicator.circleImage.color = MicNormalColor; // Orange
                else
                    indicator.circleImage.color = new Color(1f, 1f, 1f, 0.2f); // White 20%
            }

            // 체크 아이콘 표시
            if (indicator.checkIcon != null)
                indicator.checkIcon.SetActive(isCompleted);

            // 숫자 표시
            if (indicator.numberText != null)
                indicator.numberText.gameObject.SetActive(!isCompleted);

            // 연결선 색상
            if (indicator.connectorLine != null)
            {
                var lineImage = indicator.connectorLine.GetComponent<Image>();
                if (lineImage != null)
                {
                    lineImage.color = isCompleted
                        ? new Color(0.13f, 0.77f, 0.33f)
                        : new Color(1f, 1f, 1f, 0.2f);
                }
            }
        }
    }

    private void ResetMicButton()
    {
        if (MicButtonImage != null)
            MicButtonImage.color = MicNormalColor;

        if (RecordingStatusText != null)
            RecordingStatusText.text = "마이크를 클릭해서 말해주세요";
    }

    #endregion

    #region Listeners

    private void RegisterListeners()
    {
        if (MicButton != null)
        {
            MicButton.onClick.RemoveAllListeners();
            MicButton.onClick.AddListener(OnMicButtonClicked);
        }
    }

    private void RemoveListeners()
    {
        if (MicButton != null)
            MicButton.onClick.RemoveAllListeners();
    }

    #endregion

    #region Event Handlers

    /// <summary>
    /// 마이크 버튼 클릭 처리
    /// - 1번째 클릭: 녹음 시작
    /// - 2번째 클릭: 녹음 완료 → 다음 단계
    /// </summary>
    private void OnMicButtonClicked()
    {
        if (_currentPhase == PracticePhase.Complete) return;

        if (!_isRecording)
        {
            // 녹음 시작
            StartRecording();
        }
        else
        {
            // 녹음 완료
            StopRecording();
        }
    }

    private void StartRecording()
    {
        _isRecording = true;
        _recordingStartTime = Time.time;

        // UI 업데이트
        if (MicButtonImage != null)
            MicButtonImage.color = MicRecordingColor;

        if (RecordingStatusText != null)
            RecordingStatusText.text = "듣고 있어요...";

        // 녹음 시작 콜백
        OnRecordingStarted();

        // [TODO] 실제 마이크 녹음 시작
        // AudioSource나 Microphone.Start() 호출
    }

    private void StopRecording()
    {
        _isRecording = false;
        float recordingDuration = Time.time - _recordingStartTime;

        // [TODO] 실제 마이크 녹음 종료 및 STT 처리
        // string sttResult = await SpeechToText(audioClip);

        // 현재는 placeholder로 시뮬레이션
        string simulatedText = GetPlaceholder((int)_currentPhase);

        // 녹음 데이터 저장
        SaveRecordingData(recordingDuration, simulatedText);

        // UI 업데이트
        if (MicButtonImage != null)
            MicButtonImage.color = MicNormalColor;

        if (RecordingStatusText != null)
            RecordingStatusText.text = "완료!";

        // 입력 표시 (STT 결과 표시용)
        if (UserInputDisplayRoot != null)
            UserInputDisplayRoot.SetActive(true);

        if (UserInputDisplayText != null)
            UserInputDisplayText.text = simulatedText;

        // 녹음 완료 콜백
        OnRecordingEnded();

        // 다음 단계로 전환
        StartCoroutine(TransitionToNextPhase());
    }

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

    #region Virtual Callbacks

    /// <summary>녹음 시작 시 호출 (파생 클래스에서 override 가능)</summary>
    protected virtual void OnRecordingStarted()
    {
        // [TODO] 마이크 애니메이션 시작
    }

    /// <summary>녹음 종료 시 호출 (파생 클래스에서 override 가능)</summary>
    protected virtual void OnRecordingEnded()
    {
        // [TODO] 마이크 애니메이션 종료
    }

    #endregion
}
