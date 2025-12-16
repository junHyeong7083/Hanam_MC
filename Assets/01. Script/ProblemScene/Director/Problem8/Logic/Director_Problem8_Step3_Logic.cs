using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem8 / Step3 로직 베이스
/// - "첫 장면 결정" 음성 녹음
/// - 흐름: selectAction → recording → result → 3초 후 자동 완료
/// </summary>
public abstract class Director_Problem8_Step3_Logic : ProblemStepBase
{
    // =========================
    // 선택지 데이터 구조
    // =========================

    [Serializable]
    public class ActionItem
    {
        public string id;       // DB 저장용 ID (예: "action1", "action2", "action3")
        public string text;     // 대사 텍스트 (예: "워크넷 사이트 둘러보는 장면")
        public Button button;   // 버튼 참조
        public Text label;      // 텍스트 표시용 (text 값이 동적으로 매핑됨)
        public GameObject normalIcon;   // 평소 아이콘 (클릭 전)
        public GameObject clickIcon;    // 클릭 시 아이콘
        public GameObject markerIcon;   // 클릭 시 마커 아이콘
    }

    // =========================
    // DB 저장용 DTO
    // =========================

    [Serializable]
    private class SelectedActionDto
    {
        public string id;
        public string text;
    }

    [Serializable]
    private class ActionAttemptDto
    {
        public SelectedActionDto selectedAction;
        public float recordingDuration;
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    [Header("액션 선택 화면")]
    protected abstract GameObject SelectActionRoot { get; }
    protected abstract ActionItem[] ActionChoices { get; }

    [Header("녹음 화면")]
    protected abstract GameObject RecordingRoot { get; }
    protected abstract Button RecordButton { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }

    [Header("결과 화면")]
    protected abstract GameObject ResultRoot { get; }
    protected abstract Text ResultText { get; }  // "좋아요! '{text}'는 정말 훌륭한 첫 장면이에요." 표시용

    [Header("완료 게이트")]
    protected abstract StepCompletionGate CompletionGateRef { get; }

    #endregion

    #region Virtual Config

    protected virtual float ResultDisplayDuration => 3.0f;  // 결과 화면 표시 후 자동 완료까지 시간

    #endregion

    // 내부 상태
    private ActionItem _selectedAction;
    private Coroutine _completeRoutine;
    private bool _isRecording;
    private float _recordingStartTime;

    // 파생 클래스에서 선택된 액션 접근용
    protected ActionItem SelectedAction => _selectedAction;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _selectedAction = null;
        _isRecording = false;
        _recordingStartTime = 0f;

        var gate = CompletionGateRef;
        if (gate != null)
            gate.ResetGate(1);

        SetupAllUI();
        RegisterListeners();
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_completeRoutine != null)
        {
            StopCoroutine(_completeRoutine);
            _completeRoutine = null;
        }

        RemoveAllListeners();
    }

    // =========================
    // 초기 설정
    // =========================

    private void SetupAllUI()
    {
        // UI 초기 상태
        if (SelectActionRoot != null) SelectActionRoot.SetActive(true);
        if (RecordingRoot != null) RecordingRoot.SetActive(false);
        if (ResultRoot != null) ResultRoot.SetActive(false);

        // 액션 텍스트 동적 매핑 + 아이콘 초기화
        var actions = ActionChoices;
        if (actions != null)
        {
            foreach (var choice in actions)
            {
                if (choice == null) continue;

                if (choice.label != null && !string.IsNullOrEmpty(choice.text))
                    choice.label.text = choice.text;

                // 아이콘 초기 상태: normalIcon만 보임
                if (choice.normalIcon != null) choice.normalIcon.SetActive(true);
                if (choice.clickIcon != null) choice.clickIcon.SetActive(false);
                if (choice.markerIcon != null) choice.markerIcon.SetActive(false);
            }
        }
    }

    private void RegisterListeners()
    {
        // 액션 버튼들
        var actions = ActionChoices;
        if (actions != null)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                var choice = actions[i];
                if (choice?.button != null)
                {
                    choice.button.onClick.RemoveAllListeners();
                    choice.button.onClick.AddListener(() => OnActionSelected(choice));
                }
            }
        }

        // 녹음 버튼 → MicRecordingIndicator 토글
        if (RecordButton != null)
        {
            RecordButton.onClick.RemoveAllListeners();
            RecordButton.onClick.AddListener(OnRecordButtonClicked);
        }

        // MicRecordingIndicator 이벤트 구독
        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched += OnMicRecordingComplete;
            mic.OnNoMatch += OnMicRecordingNoMatch;
        }
    }

    private void RemoveAllListeners()
    {
        var actions = ActionChoices;
        if (actions != null)
        {
            foreach (var choice in actions)
                if (choice?.button != null) choice.button.onClick.RemoveAllListeners();
        }

        if (RecordButton != null)
            RecordButton.onClick.RemoveAllListeners();

        // MicRecordingIndicator 이벤트 해제
        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnMicRecordingComplete;
            mic.OnNoMatch -= OnMicRecordingNoMatch;
        }
    }

    // =========================
    // 버튼 핸들러
    // =========================

    private void OnActionSelected(ActionItem choice)
    {
        if (_isRecording) return;  // 녹음 중이면 무시

        _selectedAction = choice;

        // 선택 시각 효과
        OnActionSelectedVisual(choice);

        // RecordingRoot 표시 (SelectActionRoot는 유지)
        if (RecordingRoot != null)
            RecordingRoot.SetActive(true);
    }

    private void OnRecordButtonClicked()
    {
        if (_selectedAction == null) return;

        // 콜백에서 _isRecording이 변경될 수 있으므로 미리 저장
        bool wasRecording = _isRecording;

        var mic = MicIndicator;
        if (mic != null)
        {
            if (!wasRecording)
            {
                // 녹음 시작
                _isRecording = true;
                _recordingStartTime = Time.time;
                mic.ToggleRecording();
                OnRecordingStarted();
            }
            else
            {
                // 녹음 중지 (콜백에서 CompleteRecording 호출됨)
                mic.ToggleRecording();
            }
        }
        else
        {
            // MicIndicator 없으면 기존 로직
            if (!wasRecording)
            {
                _isRecording = true;
                _recordingStartTime = Time.time;
                OnRecordingStarted();
            }
            else
            {
                CompleteRecording();
            }
        }
    }

    private void OnMicRecordingComplete(int keywordIndex)
    {
        if (!_isRecording) return;
        CompleteRecording();
    }

    private void OnMicRecordingNoMatch(string result)
    {
        if (!_isRecording) return;
        // 매칭 실패해도 녹음은 완료 처리
        CompleteRecording();
    }

    private void CompleteRecording()
    {
        _isRecording = false;
        float recordingDuration = Time.time - _recordingStartTime;
        OnRecordingEnded();

        // DB 저장
        var body = new ActionAttemptDto
        {
            selectedAction = new SelectedActionDto
            {
                id = _selectedAction?.id,
                text = _selectedAction?.text
            },
            recordingDuration = recordingDuration
        };
        SaveAttempt(body);

        // SelectActionRoot, RecordingRoot 숨기고 ResultRoot 표시
        if (SelectActionRoot != null)
            SelectActionRoot.SetActive(false);
        if (RecordingRoot != null)
            RecordingRoot.SetActive(false);
        if (ResultRoot != null)
            ResultRoot.SetActive(true);

        // 결과 텍스트 매핑
        if (ResultText != null && _selectedAction != null)
            ResultText.text = $"좋아요! '{_selectedAction.text}'는 정말 훌륭한 첫 장면이에요.";

        // 3초 후 자동 완료
        if (_completeRoutine != null)
            StopCoroutine(_completeRoutine);
        _completeRoutine = StartCoroutine(CompleteAfterDelay());
    }

    // =========================
    // 코루틴
    // =========================

    private IEnumerator CompleteAfterDelay()
    {
        yield return new WaitForSeconds(ResultDisplayDuration);

        // Gate 완료
        var gate = CompletionGateRef;
        if (gate != null)
            gate.MarkOneDone();

        _completeRoutine = null;
    }

    // =========================
    // 시각 효과 (파생 클래스에서 override 가능)
    // =========================

    protected virtual void OnActionSelectedVisual(ActionItem selected)
    {
        var actions = ActionChoices;
        if (actions == null) return;

        foreach (var choice in actions)
        {
            if (choice == null) continue;

            bool isSelected = choice == selected;

            // 선택된 항목: normalIcon 숨기고, clickIcon + markerIcon 표시
            // 비선택 항목: normalIcon만 표시
            if (choice.normalIcon != null)
                choice.normalIcon.SetActive(!isSelected);
            if (choice.clickIcon != null)
                choice.clickIcon.SetActive(isSelected);
            if (choice.markerIcon != null)
                choice.markerIcon.SetActive(isSelected);
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
