using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem9_Step3_Logic - 문제9 스텝3 대사 조각 말하기 로직 (추상 클래스)
///
/// 【역할】 3라운드에 걸쳐 키워드 카드를 보고 마이크로 전체 문장을 말하는 마무리 스텝.
///          각 라운드: 키워드(흰색) 표시 → STT 녹음 → 매칭 성공 시 전체 문장(검정) 표시
///          → 1초 후 다음 라운드. 3라운드 완료 시 CompleteRoot 표시 + DB 저장.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층.
/// 【문제/스텝】 Director 테마 > 문제9 > 스텝3 (마무리 - STT 말하기)
/// 【부모 클래스】 ProblemStepBase
/// 【참조하는 곳】 Director_Problem9_Step3 (Binder)
/// 【참조되는 곳】 ProblemStepBase, DialogueSequencer, MicRecordingIndicator
/// 【흐름】 스텝 진입 → 라운드1: 키워드 카드 표시 → 마이크 녹음 → STT 매칭
///         → 성공: 전체 문장 표시 → 1초 후 라운드2 → ... → 라운드3 완료 → DB 저장 + 완료
/// </summary>
public abstract class Director_Problem9_Step3_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    /// <summary>한 라운드의 키워드/전체문장/이미지 데이터를 묶는 구조체</summary>
    [Serializable]
    public class RoundData
    {
        public int guideTextId;        // 하남박스 가이드 텍스트의 CSV textId
        public int keywordTextId;      // 키워드 textId ("상황"/"감정"/"바람") - 흰색으로 표시
        public int fullTextId;         // 전체 문장 textId - 검정으로 표시 (STT 매칭 키워드)
        public int altFullTextId;      // 추가 STT 인식 키워드 (선택, 0이면 무시)
        public Sprite questionSprite;  // 퍼즐 이미지: 키워드 표시 시 스프라이트
        public Sprite answerSprite;    // 퍼즐 이미지: 전체 문장 표시 시 스프라이트
    }

    /// <summary>말하기 기록 DTO (DB 저장용)</summary>
    [Serializable]
    private class SpeakAttemptDto
    {
        public int roundIndex;      // 라운드 번호
        public string phase;        // 페이즈 이름 ("situation"/"feeling"/"request")
        public string recordedText; // 인식된 전체 문장 텍스트
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    /// <summary>라운드 데이터 배열 (3라운드)</summary>
    protected abstract RoundData[] Rounds { get; }

    // ----- 하남박스 -----
    /// <summary>가이드 텍스트 UI</summary>
    protected abstract Text GuideText { get; }
    /// <summary>실패 시 표시할 텍스트의 CSV textId</summary>
    protected abstract int GuideTextId_Fail { get; }
    /// <summary>전체 완료 시 표시할 텍스트의 CSV textId</summary>
    protected abstract int GuideTextId_Success { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 대사 시퀀서

    // ----- 메인 영역 -----
    /// <summary>퍼즐(키워드/전체 문장) 이미지</summary>
    protected abstract Image PuzzleImage { get; }
    /// <summary>퍼즐 텍스트 (키워드=흰색, 전체 문장=검정)</summary>
    protected abstract Text PuzzleText { get; }

    // ----- 마이크 -----
    /// <summary>마이크 녹음 버튼</summary>
    protected abstract Button MicButton { get; }
    /// <summary>마이크 녹음 인디케이터 (STT 처리)</summary>
    protected abstract MicRecordingIndicator MicIndicator { get; }

    // ----- 루트 -----
    /// <summary>메인 활동 UI 루트</summary>
    protected abstract GameObject MainRoot { get; }
    /// <summary>완료 시 표시할 UI 루트</summary>
    protected abstract GameObject CompleteRoot { get; }

    #endregion

    #region Virtual Config

    /// <summary>키워드 텍스트 색상 (기본 흰색)</summary>
    protected virtual Color KeywordColor => Color.white;
    /// <summary>전체 문장 텍스트 색상 (기본 어두운 회색)</summary>
    protected virtual Color FullTextColor => new Color(0.196f, 0.196f, 0.196f);
    /// <summary>성공 후 다음 라운드까지 전환 딜레이 (초)</summary>
    protected virtual float TransitionDelay => 1.0f;

    #endregion

    // ===== 내부 상태 =====
    private int _currentRound;                     // 현재 라운드 인덱스
    private bool _speaking;                        // STT 매칭 성공 처리 중 여부
    private Coroutine _transitionRoutine;          // 라운드 전환 코루틴 핸들
    private Coroutine _guideRevertRoutine;         // 실패 가이드 복귀 코루틴 핸들
    private List<SpeakAttemptDto> _attempts;        // 모든 라운드의 말하기 기록 (DB 저장용)
    private bool _interactionLocked = true;        // 대화 재생 중 상호작용 잠금

    // =========================
    // ProblemStepBase 구현
    // =========================

    /// <summary>스텝 진입. 상태 초기화, 리스너 등록, 첫 라운드 표시, 대화 재생 대기.</summary>
    protected override void OnStepEnter()
    {
        _currentRound = 0;
        _speaking = false;
        _attempts = new List<SpeakAttemptDto>();

        RegisterListeners();
        ShowRound(0);

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    /// <summary>대화 진입 완료 시 상호작용 잠금 해제.</summary>
    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    /// <summary>스텝 퇴장. 코루틴 정지, 리스너 정리.</summary>
    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;

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

    /// <summary>마이크 버튼 클릭 및 STT 이벤트 리스너를 등록한다.</summary>
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

    /// <summary>
    /// 지정된 라운드의 UI를 세팅한다. 가이드 텍스트, 퍼즐 이미지/키워드 텍스트,
    /// STT 키워드 설정, 마이크 활성화, MainRoot/CompleteRoot 상태를 초기화한다.
    /// </summary>
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

        // STT 키워드: fullTextId + altFullTextId (있으면 추가)
        var mic = MicIndicator;
        if (mic != null && data.fullTextId > 0)
        {
            if (data.altFullTextId > 0)
                mic.SetKeywords(new[] { ProblemRuntime.L(data.fullTextId), ProblemRuntime.L(data.altFullTextId) });
            else
                mic.SetKeywords(new[] { ProblemRuntime.L(data.fullTextId) });
        }

        // 마이크 버튼 활성화
        if (MicButton != null)
            MicButton.interactable = true;

        // MainRoot 표시, CompleteRoot 숨김
        if (MainRoot != null) MainRoot.SetActive(true);
        if (CompleteRoot != null) CompleteRoot.SetActive(false);
    }

    // =========================
    // 마이크 클릭
    // =========================

    /// <summary>마이크 버튼 클릭. 상태 관리만 하고 실제 녹음 토글은 인스펙터에서 직접 연결.</summary>
    private void OnMicClicked()
    {
        if (_speaking) return;
        // ToggleRecording은 인스펙터에서 직접 연결 — 여기서는 상태만 관리
    }

    // =========================
    // STT 콜백
    // =========================

    /// <summary>STT 키워드 매칭 성공 시 호출. 성공 처리로 전달한다.</summary>
    private void OnKeywordMatched(int index)
    {
        OnSpeakSuccess();
    }

    /// <summary>STT 매칭 실패 시 호출. 실패 처리로 전달한다.</summary>
    private void OnNoMatch(string result)
    {
        OnSpeakFail();
    }

    // =========================
    // 성공 / 실패
    // =========================

    /// <summary>
    /// 말하기 성공. 기록 추가, 퍼즐을 전체 문장(검정)으로 전환,
    /// 마이크 비활성화, TransitionDelay 후 다음 라운드로 전환한다.
    /// </summary>
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

    /// <summary>말하기 실패. 실패 가이드를 표시하고 2초 후 원래 가이드로 복귀한다.</summary>
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

    /// <summary>실패 가이드를 표시한 뒤 2초 후 원래 가이드로 복귀하는 코루틴.</summary>
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

    /// <summary>딜레이 후 다음 라운드로 전환하거나, 마지막이면 완료 처리하는 코루틴.</summary>
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

    /// <summary>
    /// 3라운드 모두 완료. MainRoot 숨기고 CompleteRoot 표시,
    /// 성공 가이드 텍스트 설정, DB 저장, 완료 텍스트 표시.
    /// </summary>
    private void ShowComplete()
    {
        // MainRoot 숨기고 CompleteRoot 표시
        if (MainRoot != null) MainRoot.SetActive(false);
        if (CompleteRoot != null) CompleteRoot.SetActive(true);

        // 가이드 텍스트: 성공
        if (GuideText != null && GuideTextId_Success > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Success);

        // DB 저장
        SaveAttempt(_attempts);

        // 완료 처리
        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
    }
}
