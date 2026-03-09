using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step3 로직 베이스
/// - Step2에서 선택한 장르 인덱스에 따라 다짐 안내 대사 결정
/// - 마이크로 다짐 말하기 → STT 인식 → 포스터에 텍스트 작성
/// - 완료 후 ShowCompletedText → NextStepBtn
/// </summary>
public abstract class Director_Problem10_Step3_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    [Serializable]
    public class GenreCommitmentData
    {
        public int guideTextId;     // 다짐 안내 textId (101100007~010)
        public string sttKeyword;   // STT 키워드 = 포스터에 표시할 텍스트
        public Sprite cardSprite;   // 장르별 포스터 스프라이트
    }

    [Serializable]
    private class PosterCreationDto
    {
        public string commitment;
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    /// <summary>
    /// 장르 인덱스(0~3)별 다짐 데이터
    /// Step2에서 선택한 인덱스로 결정
    /// </summary>
    protected abstract GenreCommitmentData[] GenreCommitments { get; }

    protected abstract int FailGuideTextId { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    [Header("Completion")]
    [SerializeField] private StepCompletionGate completionGate;

    // 마이크
    protected abstract GameObject MicRoot { get; }
    protected abstract Button MicButton { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }

    // 포스터
    protected abstract Image GenreCardImage { get; }
    protected abstract Text PosterCommitmentText { get; }

    // 공유 데이터
    protected abstract Problem10SharedData SharedData { get; }

    #endregion

    #region Virtual Config

    protected virtual float CompleteDelay => 1.0f;

    #endregion

    // 내부 상태
    private bool _speaking;
    private GenreCommitmentData _activeData;
    private Coroutine _guideRevertRoutine;
    private bool _interactionLocked = true;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _speaking = false;
        _activeData = null;

        // 포스터 텍스트 초기화
        if (PosterCommitmentText != null) PosterCommitmentText.text = "";

        // 선택한 장르에 따른 다짐 데이터 결정
        var shared = SharedData;
        var commitments = GenreCommitments;
        if (shared != null && commitments != null
            && shared.selectedGenreIndex >= 0
            && shared.selectedGenreIndex < commitments.Length)
        {
            _activeData = commitments[shared.selectedGenreIndex];
        }

        // 장르별 포스터 스프라이트 설정
        if (_activeData != null && _activeData.cardSprite != null && GenreCardImage != null)
            GenreCardImage.sprite = _activeData.cardSprite;

        // 장르별 가이드 텍스트를 enterTextIds로 동적 설정
        if (dialogueSequencer != null && _activeData != null && _activeData.guideTextId > 0)
            dialogueSequencer.SetEnterTextIds(new[] { _activeData.guideTextId });

        RegisterListeners();

        // 초기에 마이크 숨김 (enterTextIds 끝난 후 표시)
        if (MicRoot != null) MicRoot.SetActive(false);
        if (completionGate != null) completionGate.ResetGate(1);

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            ShowSpeakingPhase();
    }

    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
        ShowSpeakingPhase();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;

        if (_guideRevertRoutine != null)
        {
            StopCoroutine(_guideRevertRoutine);
            _guideRevertRoutine = null;
        }

        RemoveListeners();
    }

    // =========================
    // 리스너 등록/해제
    // =========================

    private void RegisterListeners()
    {
        var micBtn = MicButton;
        if (micBtn != null)
        {
            micBtn.onClick.RemoveAllListeners();
            micBtn.onClick.AddListener(OnMicClicked);
        }

        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnKeywordMatched;
            mic.OnKeywordMatched += OnKeywordMatched;
            mic.OnNoMatch -= OnNoMatch;
            mic.OnNoMatch += OnNoMatch;
        }
    }

    private void RemoveListeners()
    {
        var micBtn = MicButton;
        if (micBtn != null) micBtn.onClick.RemoveAllListeners();

        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnKeywordMatched;
            mic.OnNoMatch -= OnNoMatch;
        }
    }

    // =========================
    // 말하기 페이즈
    // =========================

    private void ShowSpeakingPhase()
    {
        _speaking = false;

        // STT 키워드 설정
        var mic = MicIndicator;
        if (mic != null && _activeData != null && !string.IsNullOrEmpty(_activeData.sttKeyword))
            mic.SetKeywords(new[] { _activeData.sttKeyword });

        // UI: MicRoot 표시
        if (MicRoot != null) MicRoot.SetActive(true);
        if (MicButton != null) MicButton.interactable = true;
    }

    // =========================
    // 마이크 클릭
    // =========================

    private void OnMicClicked()
    {
        if (_speaking) return;
    }

    // =========================
    // STT 콜백
    // =========================

    private void OnKeywordMatched(int index)
    {
        OnSpeakSuccess();
    }

    private void OnNoMatch(string result)
    {
        OnSpeakFail();
    }

    // =========================
    // 성공 / 실패
    // =========================

    private void OnSpeakSuccess()
    {
        if (_speaking) return;
        _speaking = true;

        // 포스터에 다짐 텍스트 작성
        if (PosterCommitmentText != null && _activeData != null)
            PosterCommitmentText.text = _activeData.sttKeyword;

        // 마이크 숨김
        if (MicRoot != null) MicRoot.SetActive(false);

        // 딜레이 후 Complete
        StartCoroutine(DelayedComplete());
    }

    private void OnSpeakFail()
    {
        if (_speaking) return;

        if (FailGuideTextId > 0)
        {
            if (_guideRevertRoutine != null)
                StopCoroutine(_guideRevertRoutine);
            _guideRevertRoutine = StartCoroutine(ShowFailGuideAndRevert());
        }
    }

    private IEnumerator ShowFailGuideAndRevert()
    {
        if (dialogueSequencer != null)
            dialogueSequencer.SetText(FailGuideTextId);

        yield return new WaitForSeconds(2f);

        // 원래 가이드로 복귀
        if (dialogueSequencer != null && _activeData != null
            && !_speaking && _activeData.guideTextId > 0)
        {
            dialogueSequencer.SetText(_activeData.guideTextId);
        }

        _guideRevertRoutine = null;
    }

    // =========================
    // Complete
    // =========================

    private IEnumerator DelayedComplete()
    {
        yield return new WaitForSeconds(CompleteDelay);
        ShowComplete();
    }

    private void ShowComplete()
    {
        // 완료 처리: gate가 NextStepBtn 표시
        if (completionGate != null)
            completionGate.MarkOneDone();

        // 나머지 숨김
        if (MicRoot != null) MicRoot.SetActive(false);

        // SharedData에 포스터 텍스트 저장 (엔딩에서 사용)
        var shared = SharedData;
        if (shared != null)
        {
            shared.SetPosterTexts(
                "",
                PosterCommitmentText != null ? PosterCommitmentText.text : ""
            );
        }

        // DB 저장
        SaveAttempt(new PosterCreationDto
        {
            commitment = PosterCommitmentText != null ? PosterCommitmentText.text : ""
        });
    }
}
