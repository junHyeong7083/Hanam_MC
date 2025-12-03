using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step3 로직 베이스
/// - 영화 제목 녹음 → 다짐 선언 녹음 → 포스터 완성
/// - 2개 서브스텝: title(제목) → commitment(다짐)
/// - 각 서브스텝마다 마이크 버튼 클릭으로 녹음
/// - 모두 완료 시 complete 화면 표시
///
/// [STT 연동]
/// - StartMicrophoneRecording(): 마이크 녹음 시작 (파생 클래스에서 구현)
/// - StopMicrophoneRecording(): 마이크 녹음 종료 + STT 결과 반환 (파생 클래스에서 구현)
/// - 현재는 더미로 빈 문자열 반환
/// </summary>
public abstract class Director_Problem10_Step3_Logic : ProblemStepBase
{
    #region Data Classes

    public enum RecordingPhase
    {
        Title,      // 영화 제목 녹음
        Commitment, // 다짐 선언 녹음
        Complete    // 완료 화면
    }

    [Serializable]
    public class PhaseData
    {
        public string id;               // title, commitment
        [TextArea(2, 4)]
        public string instruction;      // 조감독 안내 멘트
    }

    // DB 저장용 DTO
    [Serializable]
    public class PosterCreationDto
    {
        public string stepKey;
        public string selectedGenreId;
        public string selectedGenreName;
        public string movieTitle;
        public float titleRecordingDuration;
        public string commitment;
        public float commitmentRecordingDuration;
        public DateTime completedAt;
    }

    #endregion

    #region Abstract Properties

    [Header("===== 단계 데이터 =====")]
    protected abstract PhaseData[] PhaseDataList { get; }

    [Header("===== 화면 루트 =====")]
    protected abstract GameObject RecordingRoot { get; }

    [Header("===== 포스터 프리뷰 UI =====")]
    protected abstract Text PosterGenreEmoji { get; }
    protected abstract Text PosterGenreName { get; }
    protected abstract Text PosterTitleText { get; }
    protected abstract Text PosterCommitmentText { get; }

    [Header("===== 녹음 화면 UI =====")]
    protected abstract Text InstructionText { get; }
    protected abstract Button MicButton { get; }
    protected abstract Image MicButtonImage { get; }
    protected abstract Text RecordingStatusText { get; }

    [Header("===== 공유 데이터 =====")]
    protected abstract Problem10SharedData SharedData { get; }

    [Header("===== 완료 게이트 =====")]
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    protected virtual Color MicNormalColor => new Color(1f, 0.54f, 0.24f); // #FF8A3D
    protected virtual Color MicRecordingColor => new Color(0.94f, 0.27f, 0.27f); // Red
    protected virtual float DelayAfterRecording => 0.8f;

    #endregion

    // 내부 상태
    private RecordingPhase _currentPhase;
    private bool _isRecording;
    private float _recordingStartTime;

    // 녹음 결과
    private string _movieTitle = "";
    private float _titleDuration;
    private string _commitment = "";
    private float _commitmentDuration;

    #region Step Lifecycle

    protected override void OnStepEnter()
    {
        _currentPhase = RecordingPhase.Title;
        _isRecording = false;
        _movieTitle = "";
        _commitment = "";

        // 공유 데이터에서 장르 정보 로드
        LoadGenreFromSharedData();

        // Gate 초기화
        var gate = CompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);

        // 초기 화면 설정
        ShowPhase(RecordingPhase.Title);
        RegisterListeners();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();
        RemoveListeners();
    }

    #endregion

    #region Genre Loading

    private void LoadGenreFromSharedData()
    {
        var data = SharedData;

        // 포스터에 장르 정보 표시
        if (data != null)
        {
            if (PosterGenreEmoji != null)
                PosterGenreEmoji.text = data.selectedGenreEmoji;

            if (PosterGenreName != null)
                PosterGenreName.text = data.selectedGenreName;
        }

        // 제목/다짐 초기화
        if (PosterTitleText != null)
            PosterTitleText.text = "";

        if (PosterCommitmentText != null)
            PosterCommitmentText.text = "";
    }

    #endregion

    #region UI Control

    private void ShowPhase(RecordingPhase phase)
    {
        _currentPhase = phase;
        _isRecording = false;

        bool isComplete = phase == RecordingPhase.Complete;

        if (!isComplete)
        {
            if (RecordingRoot != null) RecordingRoot.SetActive(true);
            ApplyPhaseToUI(phase);
            ResetMicButton();
        }
        else
        {
            // 완료 화면으로 전환
            if (RecordingRoot != null) RecordingRoot.SetActive(false);

            // 공유 데이터에 결과 저장
            SaveToSharedData();

            // DB 저장
            SavePosterData();

            // Gate 완료 → completeRoot 자동 표시
            var gate = CompletionGateRef;
            if (gate != null)
                gate.MarkOneDone();
        }
    }

    private void ApplyPhaseToUI(RecordingPhase phase)
    {
        int phaseIndex = (int)phase;
        var phases = PhaseDataList;
        if (phases == null || phaseIndex >= phases.Length) return;

        var data = phases[phaseIndex];
        if (data == null) return;

        // 안내 텍스트
        if (InstructionText != null)
            InstructionText.text = data.instruction;

        // 상태 텍스트
        if (RecordingStatusText != null)
            RecordingStatusText.text = "마이크를 클릭해서 말해주세요";
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

    private void OnMicButtonClicked()
    {
        if (_currentPhase == RecordingPhase.Complete) return;

        if (!_isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
    }

    private void StartRecording()
    {
        _isRecording = true;
        _recordingStartTime = Time.time;

        if (MicButtonImage != null)
            MicButtonImage.color = MicRecordingColor;

        if (RecordingStatusText != null)
            RecordingStatusText.text = "듣고 있어요...";

        // [STT] 마이크 녹음 시작
        StartMicrophoneRecording();
    }

    private void StopRecording()
    {
        _isRecording = false;
        float duration = Time.time - _recordingStartTime;

        // [STT] 마이크 녹음 종료 + 결과 받기
        string sttResult = StopMicrophoneRecording();

        // 결과 저장
        SaveRecordingResult(duration, sttResult);

        // UI 업데이트
        if (MicButtonImage != null)
            MicButtonImage.color = MicNormalColor;

        if (RecordingStatusText != null)
            RecordingStatusText.text = string.IsNullOrEmpty(sttResult) ? "녹음 완료!" : "완료!";

        // 다음 단계로 전환
        StartCoroutine(TransitionToNextPhase());
    }

    private void SaveRecordingResult(float duration, string text)
    {
        switch (_currentPhase)
        {
            case RecordingPhase.Title:
                _movieTitle = text;
                _titleDuration = duration;
                // 포스터에 제목 표시
                if (PosterTitleText != null)
                    PosterTitleText.text = text;
                break;

            case RecordingPhase.Commitment:
                _commitment = text;
                _commitmentDuration = duration;
                // 포스터에 다짐 표시
                if (PosterCommitmentText != null)
                    PosterCommitmentText.text = text;
                break;
        }
    }

    private IEnumerator TransitionToNextPhase()
    {
        yield return new WaitForSeconds(DelayAfterRecording);

        switch (_currentPhase)
        {
            case RecordingPhase.Title:
                ShowPhase(RecordingPhase.Commitment);
                break;
            case RecordingPhase.Commitment:
                ShowPhase(RecordingPhase.Complete);
                break;
        }
    }

    private void SaveToSharedData()
    {
        var data = SharedData;
        if (data != null)
        {
            data.SetMovieTitle(_movieTitle);
            data.SetCommitment(_commitment);
        }
    }

    private void SavePosterData()
    {
        var data = SharedData;

        SaveAttempt(new PosterCreationDto
        {
            stepKey = context != null ? context.CurrentStepKey : null,
            selectedGenreId = data?.selectedGenreId ?? "",
            selectedGenreName = data?.selectedGenreName ?? "",
            movieTitle = _movieTitle,
            titleRecordingDuration = _titleDuration,
            commitment = _commitment,
            commitmentRecordingDuration = _commitmentDuration,
            completedAt = DateTime.UtcNow
        });
    }

    #endregion

    #region STT Virtual Methods (파생 클래스에서 구현)

    /// <summary>
    /// 마이크 녹음 시작
    /// - 파생 클래스에서 실제 마이크 녹음 로직 구현
    /// - 현재는 더미 (아무 동작 안 함)
    /// </summary>
    protected virtual void StartMicrophoneRecording()
    {
        // [TODO] 실제 마이크 녹음 시작
        // Microphone.Start(...) 등
        Debug.Log("[STT] 마이크 녹음 시작 (더미)");
    }

    /// <summary>
    /// 마이크 녹음 종료 + STT 결과 반환
    /// - 파생 클래스에서 실제 STT 로직 구현
    /// - 현재는 더미 (빈 문자열 반환)
    /// </summary>
    /// <returns>STT 결과 텍스트 (현재 더미로 빈 문자열)</returns>
    protected virtual string StopMicrophoneRecording()
    {
        // [TODO] 실제 마이크 녹음 종료 + STT 처리
        // Microphone.End(...) + STT API 호출
        Debug.Log("[STT] 마이크 녹음 종료 (더미)");

        // 더미: 빈 문자열 반환 (실제 STT 연동 시 결과 반환)
        return "";
    }

    #endregion
}
