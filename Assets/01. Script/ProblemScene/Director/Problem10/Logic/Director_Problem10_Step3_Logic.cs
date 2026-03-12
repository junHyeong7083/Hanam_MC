using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem10_Step3_Logic - 문제10 스텝3 다짐 말하기 로직 (추상 클래스)
///
/// 【역할】 Step2에서 선택한 장르에 따라 다짐 안내를 결정하고,
///          마이크로 다짐을 말하면 STT로 인식하여 포스터에 텍스트를 작성한다.
///          완료 후 CompletionGate를 열어 NextStepBtn을 표시한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층.
/// 【문제/스텝】 Director 테마 > 문제10 > 스텝3 (마무리 - STT 다짐 말하기 + 포스터 작성)
/// 【부모 클래스】 ProblemStepBase
/// 【참조하는 곳】 Director_Problem10_Step3 (Binder)
/// 【참조되는 곳】 ProblemStepBase, DialogueSequencer, MicRecordingIndicator, Problem10SharedData
/// 【흐름】 스텝 진입 → 장르별 가이드 텍스트 동적 설정 → 대화 재생 → 마이크 표시
///         → STT 녹음 → 성공: 포스터에 텍스트 작성 + 게이트 열림 / 실패: 재시도
/// </summary>
public abstract class Director_Problem10_Step3_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    /// <summary>장르별 다짐 데이터 (가이드 textId, STT 키워드, 포스터 스프라이트)</summary>
    [Serializable]
    public class GenreCommitmentData
    {
        public int guideTextId;     // 다짐 안내 텍스트의 CSV textId (101100007~010)
        public string sttKeyword;   // STT 매칭 키워드 = 포스터에 표시할 다짐 텍스트
        public Sprite cardSprite;   // 장르별 포스터 스프라이트
    }

    /// <summary>포스터 작성 결과 DTO (DB 저장용)</summary>
    [Serializable]
    private class PosterCreationDto
    {
        public string commitment;   // 포스터에 작성된 다짐 텍스트
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    /// <summary>
    /// 장르 인덱스(0~3)별 다짐 데이터 배열.
    /// Step2에서 선택한 인덱스에 해당하는 데이터가 사용된다.
    /// </summary>
    protected abstract GenreCommitmentData[] GenreCommitments { get; }

    /// <summary>STT 매칭 실패 시 표시할 안내 텍스트의 CSV textId</summary>
    protected abstract int FailGuideTextId { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 대사 시퀀서 (장르별 동적 설정)

    [Header("Completion")]
    [SerializeField] private StepCompletionGate completionGate;   // 스텝 완료 게이트

    // ----- 마이크 -----
    /// <summary>마이크 UI 루트 (대화 재생 완료 후 표시)</summary>
    protected abstract GameObject MicRoot { get; }
    /// <summary>마이크 녹음 버튼</summary>
    protected abstract Button MicButton { get; }
    /// <summary>마이크 녹음 인디케이터 (STT 처리)</summary>
    protected abstract MicRecordingIndicator MicIndicator { get; }

    // ----- 포스터 -----
    /// <summary>장르별 포스터 카드 이미지</summary>
    protected abstract Image GenreCardImage { get; }
    /// <summary>포스터에 작성되는 다짐 텍스트</summary>
    protected abstract Text PosterCommitmentText { get; }

    // ----- 공유 데이터 -----
    /// <summary>Step2에서 선택한 장르 정보 + 포스터 텍스트를 공유하는 ScriptableObject</summary>
    protected abstract Problem10SharedData SharedData { get; }

    #endregion

    #region Virtual Config

    /// <summary>STT 성공 후 완료 처리까지의 딜레이 (초)</summary>
    protected virtual float CompleteDelay => 1.0f;

    #endregion

    // ===== 내부 상태 =====
    private bool _speaking;                        // STT 성공 처리 중 여부
    private GenreCommitmentData _activeData;       // 현재 선택된 장르의 다짐 데이터
    private Coroutine _guideRevertRoutine;         // 실패 가이드 복귀 코루틴 핸들
    private bool _interactionLocked = true;        // 대화 재생 중 상호작용 잠금

    // =========================
    // ProblemStepBase 구현
    // =========================

    /// <summary>
    /// 스텝 진입. Step2에서 선택한 장르에 따라 다짐 데이터를 결정하고,
    /// 포스터 스프라이트 설정, 동적 enterTextIds 설정, 리스너 등록, 대화 재생 대기.
    /// </summary>
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

    /// <summary>대화 진입 완료 시 상호작용 잠금 해제 및 마이크 표시 페이즈로 전환.</summary>
    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
        ShowSpeakingPhase();
    }

    /// <summary>스텝 퇴장. 가이드 복귀 코루틴 정지, 리스너 정리.</summary>
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

    /// <summary>마이크 버튼 및 STT 이벤트 리스너를 등록한다.</summary>
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

    /// <summary>모든 리스너를 제거한다.</summary>
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

    /// <summary>말하기 페이즈 진입. STT 키워드 설정, MicRoot 표시, 마이크 활성화.</summary>
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

    /// <summary>마이크 버튼 클릭. 녹음 토글을 실행한다.</summary>
    private void OnMicClicked()
    {
        if (_speaking) return;

        var mic = MicIndicator;
        if (mic != null)
            mic.ToggleRecording();
    }

    // =========================
    // STT 콜백
    // =========================

    /// <summary>STT 키워드 매칭 성공 시 호출.</summary>
    private void OnKeywordMatched(int index)
    {
        OnSpeakSuccess();
    }

    /// <summary>STT 매칭 실패 시 호출.</summary>
    private void OnNoMatch(string result)
    {
        OnSpeakFail();
    }

    // =========================
    // 성공 / 실패
    // =========================

    /// <summary>말하기 성공. 포스터에 다짐 텍스트를 작성하고 마이크를 숨긴 뒤 딜레이 후 완료 처리.</summary>
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

    /// <summary>말하기 실패. 실패 가이드를 표시하고 2초 후 원래 가이드로 복귀.</summary>
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

    /// <summary>실패 가이드를 DialogueSequencer에 설정한 뒤 2초 후 원래 가이드로 복귀하는 코루틴.</summary>
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

    /// <summary>CompleteDelay 후 ShowComplete를 호출하는 코루틴.</summary>
    private IEnumerator DelayedComplete()
    {
        yield return new WaitForSeconds(CompleteDelay);
        ShowComplete();
    }

    /// <summary>
    /// 완료 처리. CompletionGate 열기, 마이크 숨김, SharedData에 포스터 텍스트 저장, DB 저장.
    /// </summary>
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
