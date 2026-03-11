using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 시나리오 카드 데이터 인터페이스
/// </summary>
public interface IScenarioCardData
{
    int Id { get; }
    Sprite FilmSprite { get; }
    int FilmTextId { get; }
    int HanamTextId { get; }
    int ResponseTextId { get; }
}

/// <summary>
/// Problem5 / Step3 시나리오 순차 진행 로직.
/// - 라운드별: 필름(이미지+텍스트) + 하남 대사(TTS) + 초록색 대사(responseText)
/// - 사용자가 responseText를 말하면(STT) 다음 라운드로 이동
/// - 모든 라운드 완료 시 게이트 완료
/// </summary>
public abstract class Director_Problem5_Step3_Logic : ProblemStepBase
{
    [Serializable]
    private class ScenarioLogEntry
    {
        public int id;
        public string hanamText;
        public string responseText;
        public float time;
    }

    [Serializable]
    private class ScenarioAttemptBody
    {
        public ScenarioLogEntry[] entries;
    }

    // ===== 자식에서 주입할 추상 프로퍼티 =====

    protected abstract IScenarioCardData[] Scenarios { get; }

    protected abstract StepCompletionGate CompletionGate { get; }

    protected abstract MicRecordingIndicator MicIndicator { get; }

    // ===== 필름 UI =====

    protected abstract Image FilmImage { get; }
    protected abstract Text FilmText { get; }

    // ===== 초록색 네모 (responseText 표시) =====

    [Header("초록색 네모 (Response Text)")]
    [SerializeField] private Text responseDisplayText;

    [Header("마이크 버튼 (enter 대사 중 숨김)")]
    [SerializeField] private GameObject micButton;

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    // ===== 내부 상태 =====

    private int _currentIndex;
    private bool _interactionLocked = true;

    private readonly List<ScenarioLogEntry> _logEntries = new List<ScenarioLogEntry>();
    private float _stepStartTime;

    // ===== 페이지 계산 (Problem1_Step3 패턴) =====

    private int TotalPages => dialogueSequencer != null
        ? dialogueSequencer.EnterTextCount + Scenarios.Length + dialogueSequencer.CompletedTextCount
        : Scenarios.Length;

    private void SetTextWithScenarioPage(int textId, int scenarioIndex)
    {
        int enterCount = (dialogueSequencer != null) ? dialogueSequencer.EnterTextCount : 0;
        int currentPage = enterCount + scenarioIndex + 1;
        dialogueSequencer.SetText(textId, currentPage, TotalPages);
    }

    // ===== ProblemStepBase Hooks =====

    protected override void OnStepEnter()
    {
        var scenarios = Scenarios;
        if (scenarios == null || scenarios.Length == 0)
        {
            Debug.LogWarning("[Problem5_Step3] Scenarios 가 비어 있음");
            return;
        }

        _currentIndex = 0;

        _logEntries.Clear();
        _stepStartTime = Time.time;

        // DialogueSequencer에 시나리오 수를 extraPageCount로 알림
        if (dialogueSequencer != null)
            dialogueSequencer.SetExtraPageCount(scenarios.Length);

        // 마이크 버튼 숨김 (enter 대사 중에는 보이지 않아야 함)
        if (micButton != null)
            micButton.SetActive(false);

        // 필름/초록네모 등 시나리오 비주얼 먼저 표시 (hanamTextId 제외)
        ShowCurrentVisuals();

        // MicIndicator 이벤트 구독
        if (MicIndicator != null)
        {
            MicIndicator.OnKeywordMatched += OnSttMatched;
            MicIndicator.OnNoMatch += OnSttNoMatch;
        }

        // 게이트 리셋
        if (CompletionGate != null)
            CompletionGate.ResetGate(1);

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterSequenceDone += OnDialogueEnterComplete;
        else
        {
            _interactionLocked = false;
            OnScenarioReady();
        }
    }

    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
        OnScenarioReady();
    }

    /// <summary>
    /// enter 대사 완료 후: 마이크 버튼 표시 + hanamTextId 설정
    /// </summary>
    private void OnScenarioReady()
    {
        if (micButton != null)
            micButton.SetActive(true);

        ShowCurrentScenario();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterSequenceDone -= OnDialogueEnterComplete;

        if (MicIndicator != null)
        {
            MicIndicator.OnKeywordMatched -= OnSttMatched;
            MicIndicator.OnNoMatch -= OnSttNoMatch;
        }

        _interactionLocked = true;
    }

    // ===== 시나리오 표시 =====

    /// <summary>
    /// 필름 이미지/텍스트 + 초록 네모만 표시 (hanamTextId 제외)
    /// </summary>
    private void ShowCurrentVisuals()
    {
        var scenarios = Scenarios;
        if (scenarios == null || _currentIndex >= scenarios.Length) return;

        var s = scenarios[_currentIndex];

        if (FilmImage != null && s.FilmSprite != null)
            FilmImage.sprite = s.FilmSprite;

        if (FilmText != null && s.FilmTextId > 0)
            FilmText.text = ProblemRuntime.L(s.FilmTextId);

        if (responseDisplayText != null && s.ResponseTextId > 0)
            responseDisplayText.text = ProblemRuntime.L(s.ResponseTextId);

        // STT 키워드도 미리 설정
        if (MicIndicator != null && s.ResponseTextId > 0)
            MicIndicator.SetKeywords(new[] { ProblemRuntime.L(s.ResponseTextId) });
    }

    private void ShowCurrentScenario()
    {
        var scenarios = Scenarios;
        if (scenarios == null || _currentIndex >= scenarios.Length) return;

        var s = scenarios[_currentIndex];

        // 필름 이미지
        if (FilmImage != null && s.FilmSprite != null)
            FilmImage.sprite = s.FilmSprite;

        // 필름 내 텍스트
        if (FilmText != null && s.FilmTextId > 0)
            FilmText.text = ProblemRuntime.L(s.FilmTextId);

        // 초록색 네모 = responseText (사용자가 말해야 하는 대사)
        if (responseDisplayText != null && s.ResponseTextId > 0)
            responseDisplayText.text = ProblemRuntime.L(s.ResponseTextId);

        // 하남 텍스트 (하단 대사 + TTS) + 페이지 표시
        if (dialogueSequencer != null && s.HanamTextId > 0)
            SetTextWithScenarioPage(s.HanamTextId, _currentIndex);

        // STT 키워드 = responseText (사용자가 이 대사를 말해야 매칭)
        if (MicIndicator != null && s.ResponseTextId > 0)
            MicIndicator.SetKeywords(new[] { ProblemRuntime.L(s.ResponseTextId) });
    }

    // ===== STT 콜백 =====

    private void OnSttMatched(int index)
    {
        if (_interactionLocked) return;
        AdvanceToNext();
    }

    private void OnSttNoMatch(string rawText)
    {
        // idle 텍스트는 인스펙터 값 유지
    }

    // ===== Mic =====

    public void OnClickMic()
    {
        if (_interactionLocked) return;

        var indicator = MicIndicator;
        if (indicator != null)
            indicator.ToggleRecording();
    }

    // ===== 라운드 진행 =====

    private void AdvanceToNext()
    {
        var scenarios = Scenarios;
        if (scenarios == null || _currentIndex >= scenarios.Length) return;

        var s = scenarios[_currentIndex];

        // 로그 기록
        _logEntries.Add(new ScenarioLogEntry
        {
            id = s.Id,
            hanamText = s.HanamTextId > 0 ? ProblemRuntime.L(s.HanamTextId) : "",
            responseText = s.ResponseTextId > 0 ? ProblemRuntime.L(s.ResponseTextId) : "",
            time = Time.time - _stepStartTime
        });

        _currentIndex++;

        if (_currentIndex >= scenarios.Length)
        {
            CompleteAllScenarios();
        }
        else
        {
            ShowCurrentScenario();
        }
    }

    // ===== 완료 =====

    private void CompleteAllScenarios()
    {
        SaveScenarioAttempt();

        if (CompletionGate != null)
            CompletionGate.MarkOneDone();

        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
    }

    // ===== DB 저장 =====

    private void SaveScenarioAttempt()
    {
        var body = new ScenarioAttemptBody
        {
            entries = _logEntries.ToArray()
        };

        SaveAttempt(body);
    }
}
