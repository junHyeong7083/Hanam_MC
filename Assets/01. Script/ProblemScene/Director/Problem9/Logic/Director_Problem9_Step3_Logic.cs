using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem9 / Step3 로직 베이스
/// - "3라운드 대사 조각 말하기" 한 화면에서 진행
/// - 흐름: 키워드 카드(흰색) + 말하기 → STT 인식 → 전체 문장(검정) → 다음 라운드 (총 3회)
/// - 3회 완료 후 CompleteRoot 표시 + NextStepBtn 활성화
/// </summary>
public abstract class Director_Problem9_Step3_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    [Serializable]
    public class RoundData
    {
        public int guideTextId;        // HanamBox 가이드
        public int keywordTextId;      // 키워드 textId ("상황"/"감정"/"바람") - 흰색
        public int fullTextId;         // 전체 문장 textId - 검정
        public Sprite questionSprite;  // puzzleImg 질문 스프라이트
        public Sprite answerSprite;    // puzzleImg 답변 스프라이트
    }

    [Serializable]
    private class SpeakAttemptDto
    {
        public int roundIndex;
        public string phase;
        public string recordedText;
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    protected abstract RoundData[] Rounds { get; }

    // 하남박스
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId_Fail { get; }
    protected abstract int GuideTextId_Success { get; }
    protected abstract GameObject NextStepButtonRoot { get; }

    // 메인 영역
    protected abstract Image PuzzleImage { get; }
    protected abstract Text PuzzleText { get; }

    // 마이크
    protected abstract Button MicButton { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }

    // 루트
    protected abstract GameObject MainRoot { get; }
    protected abstract GameObject CompleteRoot { get; }

    #endregion

    #region Virtual Config

    protected virtual Color KeywordColor => Color.white;
    protected virtual Color FullTextColor => new Color(0.196f, 0.196f, 0.196f);
    protected virtual float TransitionDelay => 1.0f;

    #endregion

    // 내부 상태
    private int _currentRound;
    private bool _speaking;
    private Coroutine _transitionRoutine;
    private Coroutine _guideRevertRoutine;
    private List<SpeakAttemptDto> _attempts;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _currentRound = 0;
        _speaking = false;
        _attempts = new List<SpeakAttemptDto>();

        RegisterListeners();
        ShowRound(0);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_transitionRoutine != null)
        {
            StopCoroutine(_transitionRoutine);
            _transitionRoutine = null;
        }
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
        if (micBtn != null)
            micBtn.onClick.RemoveAllListeners();

        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnKeywordMatched;
            mic.OnNoMatch -= OnNoMatch;
        }
    }

    // =========================
    // 라운드 표시
    // =========================

    private void ShowRound(int round)
    {
        var rounds = Rounds;
        if (rounds == null || round >= rounds.Length) return;

        var data = rounds[round];
        _speaking = false;

        // 가이드 텍스트
        if (GuideText != null && data.guideTextId > 0)
            GuideText.text = ProblemRuntime.L(data.guideTextId);

        // puzzleImg: 질문 스프라이트 + 키워드(흰색)
        if (PuzzleImage != null && data.questionSprite != null)
            PuzzleImage.sprite = data.questionSprite;

        if (PuzzleText != null && data.keywordTextId > 0)
        {
            PuzzleText.text = ProblemRuntime.L(data.keywordTextId);
            PuzzleText.color = KeywordColor;
        }

        // STT 키워드: 현재 라운드의 fullText 1개만 설정
        var mic = MicIndicator;
        if (mic != null && data.fullTextId > 0)
            mic.SetKeywords(new[] { ProblemRuntime.L(data.fullTextId) });

        // 마이크 버튼 활성화
        if (MicButton != null)
            MicButton.interactable = true;

        // MainRoot 표시, CompleteRoot 숨김
        if (MainRoot != null) MainRoot.SetActive(true);
        if (CompleteRoot != null) CompleteRoot.SetActive(false);
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

        // 기록
        _attempts.Add(new SpeakAttemptDto
        {
            roundIndex = _currentRound,
            phase = _currentRound == 0 ? "situation" : _currentRound == 1 ? "feeling" : "request",
            recordedText = data.fullTextId > 0 ? ProblemRuntime.L(data.fullTextId) : ""
        });

        // puzzleImg: 답변 스프라이트 + 전체 문장(검정)
        if (PuzzleImage != null && data.answerSprite != null)
            PuzzleImage.sprite = data.answerSprite;

        if (PuzzleText != null && data.fullTextId > 0)
        {
            PuzzleText.text = ProblemRuntime.L(data.fullTextId);
            PuzzleText.color = FullTextColor;
        }

        // 마이크 비활성화
        if (MicButton != null)
            MicButton.interactable = false;

        // 1초 후 다음으로
        if (_transitionRoutine != null)
            StopCoroutine(_transitionRoutine);
        _transitionRoutine = StartCoroutine(TransitionAfterDelay());
    }

    private void OnSpeakFail()
    {
        if (_speaking) return;

        if (GuideText != null && GuideTextId_Fail > 0)
        {
            if (_guideRevertRoutine != null)
                StopCoroutine(_guideRevertRoutine);
            _guideRevertRoutine = StartCoroutine(ShowFailGuideAndRevert());
        }
    }

    private IEnumerator ShowFailGuideAndRevert()
    {
        var rounds = Rounds;
        int guideTextId = (rounds != null && _currentRound < rounds.Length)
            ? rounds[_currentRound].guideTextId
            : 0;

        GuideText.text = ProblemRuntime.L(GuideTextId_Fail);
        yield return new WaitForSeconds(2f);

        if (GuideText != null && guideTextId > 0 && !_speaking)
            GuideText.text = ProblemRuntime.L(guideTextId);

        _guideRevertRoutine = null;
    }

    // =========================
    // 라운드 전환 / 완료
    // =========================

    private IEnumerator TransitionAfterDelay()
    {
        yield return new WaitForSeconds(TransitionDelay);

        if (_currentRound < Rounds.Length - 1)
        {
            _currentRound++;
            ShowRound(_currentRound);
        }
        else
        {
            ShowComplete();
        }

        _transitionRoutine = null;
    }

    private void ShowComplete()
    {
        // MainRoot 숨기고 CompleteRoot 표시
        if (MainRoot != null) MainRoot.SetActive(false);
        if (CompleteRoot != null) CompleteRoot.SetActive(true);

        // 가이드 텍스트: 성공
        if (GuideText != null && GuideTextId_Success > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Success);

        // NextStepBtn 활성화
        if (NextStepButtonRoot != null)
            NextStepButtonRoot.SetActive(true);

        // DB 저장
        SaveAttempt(_attempts);
    }
}
