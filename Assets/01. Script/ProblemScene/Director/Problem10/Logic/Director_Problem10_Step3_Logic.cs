using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step3 로직 베이스
/// - 영화 제목 녹음 → 다짐 선언 녹음 → 포스터 완성
/// - 2개 서브스텝: title(제목) → commitment(다짐)
/// - 마이크 프리팹에서 녹음 완료 시 OnRecordingComplete() 호출
/// - 모두 완료 시 complete 화면 표시
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

    [Header("===== 녹음 화면 - 장르별 포스터 UI =====")]
    protected abstract GameObject[] GenreImages { get; }
    protected abstract Text[] PosterTitleTexts { get; }
    protected abstract Text[] PosterCommitmentTexts { get; }

    [Header("===== 녹음 화면 UI =====")]
    protected abstract Text InstructionText { get; }

    [Header("===== 공유 데이터 =====")]
    protected abstract Problem10SharedData SharedData { get; }

    [Header("===== 완료 게이트 =====")]
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    protected virtual float DelayAfterRecording => 0.8f;

    #endregion

    // 내부 상태
    private RecordingPhase _currentPhase;
    private int _selectedGenreIndex;

    // 녹음 결과
    private string _movieTitle = "";
    private float _titleDuration;
    private string _commitment = "";
    private float _commitmentDuration;

    #region Step Lifecycle

    protected override void OnStepEnter()
    {
        _currentPhase = RecordingPhase.Title;
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
    }

    #endregion

    #region Genre Loading

    private void LoadGenreFromSharedData()
    {
        var data = SharedData;

        // Step2에서 선택한 인덱스 저장 및 해당 이미지만 표시
        _selectedGenreIndex = data != null ? data.selectedGenreIndex : 0;
        UpdateGenreImage(_selectedGenreIndex);

        // 모든 포스터 제목/다짐 초기화
        ClearAllPosterTexts();
    }

    private void ClearAllPosterTexts()
    {
        var titles = PosterTitleTexts;
        var commitments = PosterCommitmentTexts;

        if (titles != null)
        {
            foreach (var text in titles)
            {
                if (text != null) text.text = "";
            }
        }

        if (commitments != null)
        {
            foreach (var text in commitments)
            {
                if (text != null) text.text = "";
            }
        }
    }

    private void UpdateGenreImage(int selectedIndex)
    {
        var images = GenreImages;
        if (images == null) return;

        for (int i = 0; i < images.Length; i++)
        {
            if (images[i] != null)
                images[i].SetActive(i == selectedIndex);
        }
    }

    #endregion

    #region UI Control

    private void ShowPhase(RecordingPhase phase)
    {
        _currentPhase = phase;

        bool isComplete = phase == RecordingPhase.Complete;

        if (!isComplete)
        {
            if (RecordingRoot != null) RecordingRoot.SetActive(true);
            ApplyPhaseToUI(phase);
        }
        else
        {
            // 완료 화면으로 전환
            if (RecordingRoot != null) RecordingRoot.SetActive(false);

            // 공유 데이터에 결과 저장 (FinalPosterDisplay가 OnEnable에서 읽음)
            SaveToSharedData();

            // DB 저장
            SavePosterData();

            // Gate 완료 → completeRoot 자동 표시 → FinalPosterDisplay.OnEnable
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
    }

    #endregion

    #region Recording Complete (마이크 프리팹에서 호출)

    /// <summary>
    /// 녹음 완료 시 Binder에서 호출
    /// - MicRecordingIndicator 이벤트와 연결
    /// </summary>
    public void OnRecordingComplete(string text, float duration)
    {
        if (_currentPhase == RecordingPhase.Complete) return;

        // 결과 저장
        SaveRecordingResult(text, duration);

        // 다음 단계로 전환
        StartCoroutine(TransitionToNextPhase());
    }

    private void SaveRecordingResult(string text, float duration)
    {
        switch (_currentPhase)
        {
            case RecordingPhase.Title:
                _movieTitle = text;
                _titleDuration = duration;
                // 선택된 장르 포스터에 제목 표시
                SetPosterTitleText(text);
                break;

            case RecordingPhase.Commitment:
                _commitment = text;
                _commitmentDuration = duration;
                // 선택된 장르 포스터에 다짐 표시
                SetPosterCommitmentText(text);
                break;
        }
    }

    private void SetPosterTitleText(string text)
    {
        var titles = PosterTitleTexts;
        if (titles != null && _selectedGenreIndex >= 0 && _selectedGenreIndex < titles.Length)
        {
            if (titles[_selectedGenreIndex] != null)
                titles[_selectedGenreIndex].text = text;
        }
    }

    private void SetPosterCommitmentText(string text)
    {
        var commitments = PosterCommitmentTexts;
        if (commitments != null && _selectedGenreIndex >= 0 && _selectedGenreIndex < commitments.Length)
        {
            if (commitments[_selectedGenreIndex] != null)
                commitments[_selectedGenreIndex].text = text;
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
}
