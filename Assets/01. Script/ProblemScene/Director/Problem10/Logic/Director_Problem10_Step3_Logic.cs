using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step3 로직 베이스
/// - 2라운드 말하기 (영화 제목 → 다짐 선언)
/// - 각 라운드: 가이드 → 말하기 → STT 인식 → 포스터에 텍스트 작성
/// - 라운드 사이 전환 대사 + NextBtn으로 진행
/// - 2라운드 완료 후 성공 가이드 + NextStepBtn
/// </summary>
public abstract class Director_Problem10_Step3_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    [Serializable]
    public class RoundData
    {
        public int guideTextId;              // 말하기 안내 (textId, 0이면 hardcodedGuide 사용)
        [TextArea(1, 2)]
        public string hardcodedGuide;        // guideTextId == 0일 때 사용
        public string sttKeyword;            // STT 키워드 = 포스터에 표시할 텍스트
        public int transitionGuideTextId;    // 성공 후 전환 대사 textId (0이면 바로 Complete)
    }

    [Serializable]
    private class PosterCreationDto
    {
        public string title;
        public string commitment;
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    protected abstract RoundData[] Rounds { get; }

    // 하남박스
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId_Success { get; }
    protected abstract string FailGuideText { get; }
    protected abstract Button NextDialogueBtn { get; }
    protected abstract GameObject NextStepButtonRoot { get; }

    // 마이크
    protected abstract GameObject MicRoot { get; }
    protected abstract Button MicButton { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }

    // 포스터
    protected abstract Image GenreCardImage { get; }
    protected abstract Text PosterTitleText { get; }
    protected abstract Text PosterCommitmentText { get; }

    // 공유 데이터
    protected abstract Problem10SharedData SharedData { get; }

    #endregion

    #region Virtual Config

    protected virtual float TransitionDelay => 1.0f;

    #endregion

    // 내부 상태
    private int _currentRound;
    private bool _speaking;
    private Coroutine _guideRevertRoutine;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _currentRound = 0;
        _speaking = false;

        // SharedData에서 포스터 스프라이트 로드
        var shared = SharedData;
        if (shared != null && shared.selectedSprite != null && GenreCardImage != null)
            GenreCardImage.sprite = shared.selectedSprite;

        // 포스터 텍스트 초기화
        if (PosterTitleText != null) PosterTitleText.text = "";
        if (PosterCommitmentText != null) PosterCommitmentText.text = "";

        RegisterListeners();
        ShowSpeakingPhase(0);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

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

        var nextBtn = NextDialogueBtn;
        if (nextBtn != null)
        {
            nextBtn.onClick.RemoveAllListeners();
            nextBtn.onClick.AddListener(OnNextDialogueClicked);
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

        var nextBtn = NextDialogueBtn;
        if (nextBtn != null) nextBtn.onClick.RemoveAllListeners();
    }

    // =========================
    // 말하기 페이즈
    // =========================

    private void ShowSpeakingPhase(int round)
    {
        var rounds = Rounds;
        if (rounds == null || round >= rounds.Length) return;

        var data = rounds[round];
        _speaking = false;

        // 가이드 텍스트
        if (GuideText != null)
        {
            if (data.guideTextId > 0)
                GuideText.text = ProblemRuntime.L(data.guideTextId);
            else if (!string.IsNullOrEmpty(data.hardcodedGuide))
                GuideText.text = data.hardcodedGuide;
        }

        // STT 키워드 설정
        var mic = MicIndicator;
        if (mic != null && !string.IsNullOrEmpty(data.sttKeyword))
            mic.SetKeywords(new[] { data.sttKeyword });

        // UI: MicRoot 표시, 버튼들 숨김
        if (MicRoot != null) MicRoot.SetActive(true);
        if (MicButton != null) MicButton.interactable = true;
        if (NextDialogueBtn != null) NextDialogueBtn.gameObject.SetActive(false);
        if (NextStepButtonRoot != null) NextStepButtonRoot.SetActive(false);
    }

    // =========================
    // 마이크 클릭
    // =========================

    private void OnMicClicked()
    {
        if (_speaking) return;
        // ToggleRecording은 인스펙터에서 직접 연결 — 여기서는 상태만 관리
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

        var rounds = Rounds;
        if (rounds == null || _currentRound >= rounds.Length) return;

        var data = rounds[_currentRound];

        // 포스터 텍스트 작성
        if (_currentRound == 0 && PosterTitleText != null)
            PosterTitleText.text = data.sttKeyword;
        else if (_currentRound == 1 && PosterCommitmentText != null)
            PosterCommitmentText.text = data.sttKeyword;

        // 마이크 숨김
        if (MicRoot != null) MicRoot.SetActive(false);

        // 전환 대사가 있으면 → 대사 표시 + NextBtn
        if (data.transitionGuideTextId > 0)
        {
            if (GuideText != null)
                GuideText.text = ProblemRuntime.L(data.transitionGuideTextId);

            if (NextDialogueBtn != null)
                NextDialogueBtn.gameObject.SetActive(true);
        }
        else
        {
            // 전환 대사 없으면 → 딜레이 후 Complete
            StartCoroutine(DelayedComplete());
        }
    }

    private void OnSpeakFail()
    {
        if (_speaking) return;

        string failText = FailGuideText;
        if (GuideText != null && !string.IsNullOrEmpty(failText))
        {
            if (_guideRevertRoutine != null)
                StopCoroutine(_guideRevertRoutine);
            _guideRevertRoutine = StartCoroutine(ShowFailGuideAndRevert());
        }
    }

    private IEnumerator ShowFailGuideAndRevert()
    {
        var rounds = Rounds;
        var data = (rounds != null && _currentRound < rounds.Length) ? rounds[_currentRound] : null;

        GuideText.text = FailGuideText;
        yield return new WaitForSeconds(2f);

        // 원래 가이드로 복귀
        if (GuideText != null && data != null && !_speaking)
        {
            if (data.guideTextId > 0)
                GuideText.text = ProblemRuntime.L(data.guideTextId);
            else if (!string.IsNullOrEmpty(data.hardcodedGuide))
                GuideText.text = data.hardcodedGuide;
        }

        _guideRevertRoutine = null;
    }

    // =========================
    // NextDialogue 클릭
    // =========================

    private void OnNextDialogueClicked()
    {
        _currentRound++;
        ShowSpeakingPhase(_currentRound);
    }

    // =========================
    // Complete
    // =========================

    private IEnumerator DelayedComplete()
    {
        yield return new WaitForSeconds(TransitionDelay);
        ShowComplete();
    }

    private void ShowComplete()
    {
        // 성공 가이드
        if (GuideText != null && GuideTextId_Success > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Success);

        // NextStepBtn 활성화
        if (NextStepButtonRoot != null)
            NextStepButtonRoot.SetActive(true);

        // 나머지 숨김
        if (MicRoot != null) MicRoot.SetActive(false);
        if (NextDialogueBtn != null) NextDialogueBtn.gameObject.SetActive(false);

        // SharedData에 포스터 텍스트 저장 (엔딩에서 사용)
        var shared = SharedData;
        if (shared != null)
        {
            shared.SetPosterTexts(
                PosterTitleText != null ? PosterTitleText.text : "",
                PosterCommitmentText != null ? PosterCommitmentText.text : ""
            );
        }

        // DB 저장
        SaveAttempt(new PosterCreationDto
        {
            title = PosterTitleText != null ? PosterTitleText.text : "",
            commitment = PosterCommitmentText != null ? PosterCommitmentText.text : ""
        });
    }
}
