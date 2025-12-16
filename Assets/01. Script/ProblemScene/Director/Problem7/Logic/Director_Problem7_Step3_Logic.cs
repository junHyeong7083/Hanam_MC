using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem7 / Step3 로직 베이스
/// - "명대사 만들기" 음성 녹음
/// - 4단계: intro → selectDialogue → recording → result(CompleteRoot)
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
        public float recordingDuration;
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
    protected abstract Button RecordButton { get; }

    [Header("녹음 화면")]
    protected abstract GameObject RecordingRoot { get; }

    [Header("완료 게이트 (CompleteRoot에 Result 화면 연결)")]
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    protected virtual float DialogueSelectDelay => 0.3f;
    protected virtual float RecordingDuration => 4.0f;

    #endregion

    // 내부 상태
    private Phase _currentPhase;
    private DialogueItem _selectedDialogue;
    private Coroutine _recordingRoutine;
    private bool _isRecording;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _currentPhase = Phase.Intro;
        _selectedDialogue = null;
        _isRecording = false;

        var gate = CompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);

        SetupAllPhases();
        ShowPhase(Phase.Intro);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_recordingRoutine != null)
        {
            StopCoroutine(_recordingRoutine);
            _recordingRoutine = null;
        }

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

        // 녹음 버튼 초기 비활성화
        if (RecordButton != null)
            RecordButton.interactable = false;

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
                var choice = dialogues[i];
                if (choice?.button != null)
                {
                    choice.button.onClick.RemoveAllListeners();
                    choice.button.onClick.AddListener(() => OnDialogueSelected(choice));
                }
            }
        }

        // 녹음 버튼
        if (RecordButton != null)
        {
            RecordButton.onClick.RemoveAllListeners();
            RecordButton.onClick.AddListener(OnRecordButtonClicked);
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

        if (RecordButton != null)
            RecordButton.onClick.RemoveAllListeners();
    }

    // =========================
    // Phase 전환
    // =========================

    private void ShowPhase(Phase phase)
    {
        _currentPhase = phase;

        if (IntroRoot != null) IntroRoot.SetActive(phase == Phase.Intro);
        if (SelectDialogueRoot != null) SelectDialogueRoot.SetActive(phase == Phase.SelectDialogue);
        // RecordingRoot는 RecordButton 클릭 시 수동으로 표시/숨김
        // Result는 CompletionGate의 CompleteRoot로 자동 표시됨
    }

    // =========================
    // 버튼 핸들러
    // =========================

    private void OnIntroNextClicked()
    {
        ShowPhase(Phase.SelectDialogue);
    }

    private void OnDialogueSelected(DialogueItem choice)
    {
        if (_currentPhase != Phase.SelectDialogue) return;

        _selectedDialogue = choice;

        // 선택 시각 효과
        OnDialogueSelectedVisual(choice);

        // 녹음 버튼 활성화
        if (RecordButton != null)
            RecordButton.interactable = true;
    }

    private void OnRecordButtonClicked()
    {
        if (_currentPhase != Phase.SelectDialogue) return;
        if (_selectedDialogue == null) return;
        if (_isRecording) return;

        // 녹음 시작
        _isRecording = true;

        // SelectDialogueRoot 숨기고, RecordingRoot 표시
        if (SelectDialogueRoot != null)
            SelectDialogueRoot.SetActive(false);
        if (RecordingRoot != null)
            RecordingRoot.SetActive(true);

        if (_recordingRoutine != null)
            StopCoroutine(_recordingRoutine);
        _recordingRoutine = StartCoroutine(RecordingRoutine());
    }

    // =========================
    // 코루틴
    // =========================

    private IEnumerator RecordingRoutine()
    {
        // 녹음 시작 콜백
        OnRecordingStarted();

        // 녹음 시간 대기
        yield return new WaitForSeconds(RecordingDuration);

        // 녹음 종료
        _isRecording = false;
        OnRecordingEnded();

        // Attempt 저장
        var body = new DialogueAttemptDto
        {
            selectedDialogue = new SelectedDialogueDto
            {
                id = _selectedDialogue?.id,
                text = _selectedDialogue?.text
            },
            recordingDuration = RecordingDuration
        };
        SaveAttempt(body);

        // RecordingRoot 숨기기
        if (RecordingRoot != null)
            RecordingRoot.SetActive(false);

        // Gate 완료 → CompleteRoot(Result 패널) 표시
        var gate = CompletionGateRef;
        if (gate != null)
            gate.MarkOneDone();
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

    protected virtual void OnRecordingStarted()
    {
        // 녹음 시작 시 효과 (파생 클래스에서 override)
    }

    protected virtual void OnRecordingEnded()
    {
        // 녹음 종료 시 효과 (파생 클래스에서 override)
    }
}
