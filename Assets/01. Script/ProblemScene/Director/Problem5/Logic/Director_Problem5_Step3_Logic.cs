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
    int TextId { get; }
    int ResponseTextId { get; }
}

/// <summary>
/// Problem5 / Step3 시나리오 순차 진행 로직.
/// - 시나리오 카드를 순서대로 표시
/// - 유저 확인 → NPC 응답 표시
/// - NPC 응답 확인 → 다음 시나리오로 이동
/// - 모든 시나리오 완료 시 게이트 완료
/// </summary>
public abstract class Director_Problem5_Step3_Logic : ProblemStepBase
{
    [Serializable]
    private class ScenarioLogEntry
    {
        public int id;
        public string scenarioText;
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

    protected abstract GameObject NpcResponseRoot { get; }
    protected abstract Text NpcResponseText { get; }

    protected abstract StepCompletionGate CompletionGate { get; }

    protected abstract MicRecordingIndicator MicIndicator { get; }

    // ===== Option Display =====

    [Header("Option Display")]
    [SerializeField] private Text optionDisplayText;

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    // ===== 내부 상태 =====

    private int _currentIndex;
    private bool _interactionLocked = true;
    private bool _waitingForNpcDismiss;

    private readonly List<ScenarioLogEntry> _logEntries = new List<ScenarioLogEntry>();
    private float _stepStartTime;

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
        _waitingForNpcDismiss = false;

        _logEntries.Clear();
        _stepStartTime = Time.time;

        // NPC 응답 초기 숨김
        if (NpcResponseRoot != null) NpcResponseRoot.SetActive(false);

        // 첫 시나리오 표시
        ShowCurrentScenario();

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
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        if (MicIndicator != null)
        {
            MicIndicator.OnKeywordMatched -= OnSttMatched;
            MicIndicator.OnNoMatch -= OnSttNoMatch;
        }

        _interactionLocked = true;
    }

    // ===== 시나리오 표시 =====

    private void ShowCurrentScenario()
    {
        var scenarios = Scenarios;
        if (scenarios == null || _currentIndex >= scenarios.Length) return;

        var s = scenarios[_currentIndex];

        if (optionDisplayText != null && s.TextId > 0)
            optionDisplayText.text = ProblemRuntime.L(s.TextId);

        // 현재 시나리오 텍스트를 STT 키워드로 설정
        if (MicIndicator != null && s.TextId > 0)
            MicIndicator.SetKeywords(new[] { ProblemRuntime.L(s.TextId) });
    }

    // ===== STT 콜백 =====

    private void OnSttMatched(int index)
    {
        if (_interactionLocked || _waitingForNpcDismiss) return;
        ShowNpcResponse();
    }

    private void OnSttNoMatch(string rawText)
    {
        if (MicIndicator != null)
            MicIndicator.SetIdleText("다시 말해주세요");
    }

    // ===== Mic =====

    public void OnClickMic()
    {
        if (_interactionLocked || _waitingForNpcDismiss) return;

        var indicator = MicIndicator;
        if (indicator != null)
            indicator.ToggleRecording();
    }

    // ===== NPC 응답 닫기 (버튼에서 호출) =====

    /// <summary>
    /// NPC 응답 확인 버튼. NPC 응답을 닫고 다음 시나리오로 이동.
    /// </summary>
    public void OnConfirmNpcResponse()
    {
        if (_interactionLocked) return;
        if (!_waitingForNpcDismiss) return;

        DismissNpcAndAdvance();
    }

    private void ShowNpcResponse()
    {
        var scenarios = Scenarios;
        if (scenarios == null || _currentIndex >= scenarios.Length) return;

        var s = scenarios[_currentIndex];

        // 로그 기록
        _logEntries.Add(new ScenarioLogEntry
        {
            id = s.Id,
            scenarioText = ProblemRuntime.L(s.TextId),
            responseText = s.ResponseTextId > 0 ? ProblemRuntime.L(s.ResponseTextId) : "",
            time = Time.time - _stepStartTime
        });

        // NPC 응답 표시
        if (NpcResponseRoot != null) NpcResponseRoot.SetActive(true);
        if (NpcResponseText != null && s.ResponseTextId > 0)
            NpcResponseText.text = ProblemRuntime.L(s.ResponseTextId);

        _waitingForNpcDismiss = true;
    }

    private void DismissNpcAndAdvance()
    {
        if (NpcResponseRoot != null) NpcResponseRoot.SetActive(false);
        _waitingForNpcDismiss = false;

        _currentIndex++;

        var scenarios = Scenarios;
        if (scenarios == null || _currentIndex >= scenarios.Length)
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
