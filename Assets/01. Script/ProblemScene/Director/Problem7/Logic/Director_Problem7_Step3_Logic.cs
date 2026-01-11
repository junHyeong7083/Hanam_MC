using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem7 / Step3 로직 베이스
/// - "명대사 만들기"
/// - 대사 3개 중 하나 선택 (버튼 클릭 또는 STT)
/// - 선택 시 RecordingRoot 표시 + "주인공에게, (선택한 대사)" 출력
/// </summary>
public abstract class Director_Problem7_Step3_Logic : ProblemStepBase
{
    // =========================
    // 선택지 데이터 구조
    // =========================

    [Serializable]
    public class DialogueItem
    {
        public string id;       // DB 저장용 ID (예: "d1", "d2", "d3")
        public string text;     // 대사 텍스트 (예: "지금까지 정말 잘 버텨왔어")
        public Button button;   // 버튼 참조
        public GameObject micIcon;    // 선택 시 표시할 마이크 아이콘 (왼쪽)
        public GameObject checkIcon;  // 선택 시 표시할 체크 아이콘 (오른쪽)
    }

    // =========================
    // DB 저장용 DTO
    // =========================

    [Serializable]
    private class SelectedDialogueDto
    {
        public string id;
        public string text;
    }

    [Serializable]
    private class DialogueAttemptDto
    {
        public SelectedDialogueDto selectedDialogue;
        public string inputMode;  // "button" or "voice"
    }

    protected enum Phase { Intro, SelectDialogue }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    [Header("Intro 화면")]
    protected abstract GameObject IntroRoot { get; }
    protected abstract Button IntroNextButton { get; }

    [Header("대사 선택 화면")]
    protected abstract GameObject SelectDialogueRoot { get; }
    protected abstract DialogueItem[] DialogueChoices { get; }

    [Header("마이크 STT")]
    protected abstract MicRecordingIndicator MicIndicator { get; }

    [Header("결과 화면")]
    protected abstract GameObject RecordingRoot { get; }
    protected abstract Text ResultText { get; }  // "주인공에게, (선택한 대사)" 표시할 텍스트

    [Header("완료 게이트 (CompleteRoot에 Result 화면 연결)")]
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    protected virtual string ResultTextFormat => "주인공에게,\n{0}";

    #endregion

    // 내부 상태
    private Phase _currentPhase;
    private DialogueItem _selectedDialogue;
    private bool _hasSelected;
    private string _inputMode = "button";

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _currentPhase = Phase.Intro;
        _selectedDialogue = null;
        _hasSelected = false;
        _inputMode = "button";

        var gate = CompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);

        SetupAllPhases();
        ShowPhase(Phase.Intro);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();
        RemoveAllListeners();
    }

    // =========================
    // 초기 설정
    // =========================

    private void SetupAllPhases()
    {
        // 모든 화면 숨기기
        if (IntroRoot != null) IntroRoot.SetActive(false);
        if (SelectDialogueRoot != null) SelectDialogueRoot.SetActive(false);
        if (RecordingRoot != null) RecordingRoot.SetActive(false);

        // 모든 선택지 아이콘 비활성화
        var dialogues = DialogueChoices;
        if (dialogues != null)
        {
            foreach (var choice in dialogues)
            {
                if (choice == null) continue;
                if (choice.micIcon != null) choice.micIcon.SetActive(false);
                if (choice.checkIcon != null) choice.checkIcon.SetActive(false);
            }
        }

        // 버튼 리스너 등록
        RegisterListeners();
    }

    private void RegisterListeners()
    {
        // Intro 버튼
        if (IntroNextButton != null)
        {
            IntroNextButton.onClick.RemoveAllListeners();
            IntroNextButton.onClick.AddListener(OnIntroNextClicked);
        }

        // 대사 버튼들
        var dialogues = DialogueChoices;
        if (dialogues != null)
        {
            for (int i = 0; i < dialogues.Length; i++)
            {
                int index = i;
                var choice = dialogues[i];
                if (choice?.button != null)
                {
                    choice.button.onClick.RemoveAllListeners();
                    choice.button.onClick.AddListener(() => OnDialogueSelected(index, "button"));
                }
            }
        }

        // MicIndicator STT 이벤트 구독 + 키워드 설정
        var mic = MicIndicator;
        if (mic != null && dialogues != null)
        {
            // 3개 대사 모두 키워드로 설정
            var keywords = new string[dialogues.Length];
            for (int i = 0; i < dialogues.Length; i++)
            {
                keywords[i] = dialogues[i]?.text ?? "";
            }
            mic.SetKeywords(keywords);

            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnKeywordMatched += OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;
            mic.OnNoMatch += OnSTTNoMatch;
        }
    }

    private void RemoveAllListeners()
    {
        if (IntroNextButton != null)
            IntroNextButton.onClick.RemoveAllListeners();

        var dialogues = DialogueChoices;
        if (dialogues != null)
        {
            foreach (var choice in dialogues)
                if (choice?.button != null) choice.button.onClick.RemoveAllListeners();
        }

        // MicIndicator 이벤트 해제
        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;
        }
    }

    // =========================
    // Phase 전환
    // =========================

    private void ShowPhase(Phase phase)
    {
        _currentPhase = phase;

        if (IntroRoot != null) IntroRoot.SetActive(phase == Phase.Intro);
        if (SelectDialogueRoot != null) SelectDialogueRoot.SetActive(phase == Phase.SelectDialogue);
    }

    // =========================
    // 버튼 핸들러
    // =========================

    private void OnIntroNextClicked()
    {
        ShowPhase(Phase.SelectDialogue);
    }

    /// <summary>
    /// 대사 선택 (버튼 또는 STT)
    /// </summary>
    private void OnDialogueSelected(int index, string inputMode)
    {
        if (_currentPhase != Phase.SelectDialogue) return;
        if (_hasSelected) return;

        var dialogues = DialogueChoices;
        if (dialogues == null || index < 0 || index >= dialogues.Length) return;

        _hasSelected = true;
        _inputMode = inputMode;
        _selectedDialogue = dialogues[index];

        Debug.Log($"[Problem7_Step3] 대사 선택: [{index}] {_selectedDialogue.text} (입력: {inputMode})");

        // 선택 시각 효과
        OnDialogueSelectedVisual(_selectedDialogue);

        // SelectDialogueRoot 숨기고 RecordingRoot 표시
        if (SelectDialogueRoot != null)
            SelectDialogueRoot.SetActive(false);
        if (RecordingRoot != null)
            RecordingRoot.SetActive(true);

        // 결과 텍스트 표시: "주인공에게, (선택한 대사)"
        if (ResultText != null)
            ResultText.text = string.Format(ResultTextFormat, _selectedDialogue.text);

        // Attempt 저장
        var body = new DialogueAttemptDto
        {
            selectedDialogue = new SelectedDialogueDto
            {
                id = _selectedDialogue.id,
                text = _selectedDialogue.text
            },
            inputMode = _inputMode
        };
        SaveAttempt(body);

        // Gate 완료 → CompleteRoot 표시
        var gate = CompletionGateRef;
        if (gate != null)
            gate.MarkOneDone();
    }

    // =========================
    // STT 이벤트 핸들러
    // =========================

    /// <summary>
    /// STT 키워드 매칭 성공 시 호출
    /// </summary>
    private void OnSTTKeywordMatched(int matchedIndex)
    {
        Debug.Log($"[Problem7_Step3] STT 매칭 성공: index={matchedIndex}");
        OnDialogueSelected(matchedIndex, "voice");
    }

    /// <summary>
    /// STT 매칭 실패 시 호출
    /// </summary>
    private void OnSTTNoMatch(string sttResult)
    {
        Debug.Log($"[Problem7_Step3] STT 매칭 실패: {sttResult}");
        // 매칭 실패 시에는 다시 녹음 시도 가능
    }

    // =========================
    // 시각 효과 (파생 클래스에서 override 가능)
    // =========================

    protected virtual void OnDialogueSelectedVisual(DialogueItem selected)
    {
        var dialogues = DialogueChoices;
        if (dialogues == null) return;

        foreach (var choice in dialogues)
        {
            if (choice == null) continue;

            bool isSelected = choice == selected;

            // 선택된 항목의 아이콘만 활성화, 나머지는 비활성화
            if (choice.micIcon != null)
                choice.micIcon.SetActive(isSelected);
            if (choice.checkIcon != null)
                choice.checkIcon.SetActive(isSelected);
        }
    }
}
