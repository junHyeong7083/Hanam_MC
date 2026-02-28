using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem8 / Step3 로직 베이스
/// - "첫 장면 결정" 한 화면에서 카드 선택 + 말하기
/// - 흐름: 카드 선택 → 마이크 클릭 → 녹음 → 키워드 매칭 → 성공/실패
/// </summary>
public abstract class Director_Problem8_Step3_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    [Serializable]
    public class ActionItem
    {
        public string id;               // DB 저장용 ID
        public int textId;              // CSV textId (카드 라벨)
        public Button button;           // 버튼 참조
        public Text label;              // 텍스트 표시용
        public GameObject selectedIcon;  // 선택 시 아이콘 (ClickIcon)
    }

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

    protected abstract ActionItem[] ActionChoices { get; }

    // 마이크 버튼
    protected abstract Button MicButton { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }

    // 가이드 텍스트
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId_Main { get; }
    protected abstract int GuideTextId_Fail { get; }
    protected abstract int GuideTextId_Success { get; }

    // 완료
    protected abstract GameObject NextStepButtonRoot { get; }

    #endregion

    // 내부 상태
    private ActionItem _selectedAction;
    private int _selectedIndex;
    private bool _isRecording;
    private float _recordingStartTime;
    private bool _isComplete;
    private Coroutine _guideRevertRoutine;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _selectedAction = null;
        _selectedIndex = -1;
        _isRecording = false;
        _recordingStartTime = 0f;
        _isComplete = false;

        InitUI();
        RegisterListeners();

        if (GuideText != null && GuideTextId_Main > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Main);
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
    // 초기 설정
    // =========================

    private void InitUI()
    {
        var actions = ActionChoices;
        if (actions != null)
        {
            foreach (var choice in actions)
            {
                if (choice == null) continue;

                if (choice.label != null && choice.textId > 0)
                    choice.label.text = ProblemRuntime.L(choice.textId);

                if (choice.selectedIcon != null)
                    choice.selectedIcon.SetActive(false);
            }
        }

        if (MicButton != null)
            MicButton.interactable = false;

        if (NextStepButtonRoot != null)
            NextStepButtonRoot.SetActive(false);
    }

    private void RegisterListeners()
    {
        var actions = ActionChoices;
        if (actions != null)
        {
            for (int i = 0; i < actions.Length; i++)
            {
                var choice = actions[i];
                if (choice?.button == null) continue;
                choice.button.onClick.RemoveAllListeners();
                int idx = i;
                var c = choice;
                choice.button.onClick.AddListener(() => OnCardSelected(c, idx));
            }
        }

        if (MicButton != null)
        {
            MicButton.onClick.RemoveAllListeners();
            MicButton.onClick.AddListener(OnMicClicked);
        }

        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched += OnMicKeywordMatched;
            mic.OnNoMatch += OnMicNoMatch;
        }
    }

    private void RemoveListeners()
    {
        var actions = ActionChoices;
        if (actions != null)
        {
            foreach (var choice in actions)
                if (choice?.button != null) choice.button.onClick.RemoveAllListeners();
        }

        if (MicButton != null)
            MicButton.onClick.RemoveAllListeners();

        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnMicKeywordMatched;
            mic.OnNoMatch -= OnMicNoMatch;
        }
    }

    // =========================
    // 카드 선택
    // =========================

    private void OnCardSelected(ActionItem choice, int index)
    {
        if (_isRecording || _isComplete) return;

        _selectedAction = choice;
        _selectedIndex = index;

        // 시각 효과: 선택된 카드만 selectedIcon 활성화
        var actions = ActionChoices;
        if (actions != null)
        {
            foreach (var c in actions)
            {
                if (c?.selectedIcon != null)
                    c.selectedIcon.SetActive(c == choice);
            }
        }

        // 마이크 버튼 활성화
        if (MicButton != null)
            MicButton.interactable = true;
    }

    // =========================
    // 마이크 녹음
    // =========================

    private void OnMicClicked()
    {
        if (_selectedAction == null || _isComplete) return;

        var mic = MicIndicator;
        if (mic != null)
        {
            if (!_isRecording)
            {
                _isRecording = true;
                _recordingStartTime = Time.time;
                mic.ToggleRecording();
            }
            else
            {
                mic.ToggleRecording();
            }
        }
        else
        {
            // MicIndicator 없으면 바로 완료
            _recordingStartTime = Time.time;
            OnSuccess();
        }
    }

    private void OnMicKeywordMatched(int keywordIndex)
    {
        if (!_isRecording) return;
        _isRecording = false;

        // 선택한 카드의 인덱스와 키워드 인덱스 매칭
        if (keywordIndex == _selectedIndex)
        {
            OnSuccess();
        }
        else
        {
            OnFail();
        }
    }

    private void OnMicNoMatch(string result)
    {
        if (!_isRecording) return;
        _isRecording = false;
        OnFail();
    }

    // =========================
    // 성공 / 실패
    // =========================

    private void OnFail()
    {
        // 가이드 텍스트: 실패 → 2초 후 복귀
        if (GuideText != null && GuideTextId_Fail > 0)
        {
            if (_guideRevertRoutine != null)
                StopCoroutine(_guideRevertRoutine);
            _guideRevertRoutine = StartCoroutine(ShowFailGuideAndRevert());
        }
    }

    private IEnumerator ShowFailGuideAndRevert()
    {
        GuideText.text = ProblemRuntime.L(GuideTextId_Fail);
        yield return new WaitForSeconds(2f);
        if (GuideText != null && GuideTextId_Main > 0 && !_isComplete)
            GuideText.text = ProblemRuntime.L(GuideTextId_Main);
        _guideRevertRoutine = null;
    }

    private void OnSuccess()
    {
        _isComplete = true;

        float recordingDuration = Time.time - _recordingStartTime;

        // DB 저장
        var body = new ActionAttemptDto
        {
            selectedAction = new SelectedActionDto
            {
                id = _selectedAction?.id,
                text = _selectedAction != null && _selectedAction.textId > 0
                    ? ProblemRuntime.L(_selectedAction.textId)
                    : ""
            },
            recordingDuration = recordingDuration
        };
        SaveAttempt(body);

        // 가이드 텍스트 → 성공
        if (GuideText != null && GuideTextId_Success > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Success);

        // 마이크 버튼 숨기기
        if (MicButton != null)
            MicButton.gameObject.SetActive(false);

        // 다음 버튼 표시
        if (NextStepButtonRoot != null)
            NextStepButtonRoot.SetActive(true);
    }
}
