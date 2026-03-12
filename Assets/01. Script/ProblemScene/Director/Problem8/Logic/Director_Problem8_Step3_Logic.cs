using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem8_Step3_Logic - 문제8 스텝3 첫 장면 결정 로직 (추상 클래스)
///
/// 【역할】 "첫 장면 결정" 테마에서 액션 카드를 선택하고 마이크로 말하는 마무리 스텝.
///          카드 선택 → 마이크 버튼 표시 → 녹음 → STT 키워드 매칭 → 성공/실패 처리.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층. SerializeField는 Binder(Director_Problem8_Step3)에서 바인딩.
/// 【문제/스텝】 Director 테마 > 문제8 > 스텝3 (마무리 - 카드 선택 + STT 말하기)
/// 【부모 클래스】 ProblemStepBase
/// 【참조하는 곳】 Director_Problem8_Step3 (Binder)
/// 【참조되는 곳】 ProblemStepBase, DialogueSequencer, MicRecordingIndicator
/// 【흐름】 스텝 진입 → 대화 재생 → 액션 카드 선택 → 마이크 버튼 표시 → 녹음
///         → STT 매칭 성공: DB 저장 + 완료 / 실패: 2초 후 가이드 복귀
/// </summary>
public abstract class Director_Problem8_Step3_Logic : ProblemStepBase
{
    // =========================
    // 데이터 구조
    // =========================

    /// <summary>액션 카드 한 장의 데이터와 UI 참조</summary>
    [Serializable]
    public class ActionItem
    {
        public string id;               // DB 저장용 식별자
        public int textId;              // CSV textId (카드 라벨 텍스트)
        public Button button;           // 카드 선택 버튼
        public Text label;              // 카드 라벨 텍스트 컴포넌트
        public GameObject selectedIcon;  // 선택 시 표시할 아이콘 (ClickIcon)
    }

    /// <summary>선택된 액션의 ID와 텍스트 DTO</summary>
    [Serializable]
    private class SelectedActionDto
    {
        public string id;    // 선택된 액션 식별자
        public string text;  // 선택된 액션 텍스트
    }

    /// <summary>액션 선택 + 녹음 기록 DTO (DB 저장용)</summary>
    [Serializable]
    private class ActionAttemptDto
    {
        public SelectedActionDto selectedAction;  // 선택된 액션 정보
        public float recordingDuration;           // 녹음 소요 시간 (초)
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    /// <summary>액션 카드 선택지 배열</summary>
    protected abstract ActionItem[] ActionChoices { get; }

    // ----- 마이크 -----
    /// <summary>마이크 녹음 시작 버튼</summary>
    protected abstract Button MicButton { get; }
    /// <summary>마이크 녹음 인디케이터 (STT 처리 및 키워드 매칭)</summary>
    protected abstract MicRecordingIndicator MicIndicator { get; }

    // ----- 가이드 텍스트 -----
    /// <summary>가이드 텍스트 UI</summary>
    protected abstract Text GuideText { get; }
    /// <summary>메인 안내 텍스트의 CSV textId</summary>
    protected abstract int GuideTextId_Main { get; }
    /// <summary>실패 시 표시할 텍스트의 CSV textId</summary>
    protected abstract int GuideTextId_Fail { get; }
    /// <summary>성공 시 표시할 텍스트의 CSV textId</summary>
    protected abstract int GuideTextId_Success { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 대사 시퀀서

    #endregion

    // ===== 내부 상태 =====
    private ActionItem _selectedAction;          // 현재 선택된 액션 카드
    private int _selectedIndex;                  // 선택된 카드의 배열 인덱스
    private bool _isRecording;                   // 녹음 중 여부
    private float _recordingStartTime;           // 녹음 시작 시간 (소요 시간 계산용)
    private bool _isComplete;                    // 스텝 완료 여부
    private Coroutine _guideRevertRoutine;       // 실패 가이드 → 원래 가이드 복귀 코루틴 핸들
    private bool _interactionLocked = true;      // 대화 재생 중 상호작용 잠금

    // =========================
    // ProblemStepBase 구현
    // =========================

    /// <summary>스텝 진입 시 호출. 상태 초기화, UI 세팅, 리스너 등록, 대화 재생 대기.</summary>
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

    /// <summary>스텝 퇴장 시 호출. 가이드 복귀 코루틴 정지, 리스너 정리.</summary>
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
    // 초기 설정
    // =========================

    /// <summary>액션 카드 라벨 설정, selectedIcon 숨김, 마이크 버튼 숨김 초기화.</summary>
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
            MicButton.gameObject.SetActive(false);

    }

    /// <summary>액션 카드 버튼, 마이크 버튼, STT 이벤트 리스너를 등록한다.</summary>
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

    /// <summary>모든 리스너를 제거한다.</summary>
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

    /// <summary>카드 선택 시 호출. 선택된 카드의 selectedIcon을 활성화하고 마이크 버튼을 표시한다.</summary>
    private void OnCardSelected(ActionItem choice, int index)
    {
        if (_interactionLocked) return;
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

        // 마이크 버튼 표시
        if (MicButton != null)
            MicButton.gameObject.SetActive(true);
    }

    // =========================
    // 마이크 녹음
    // =========================

    /// <summary>마이크 버튼 클릭 시 녹음 시작. 녹음 시작 시간을 기록한다.</summary>
    private void OnMicClicked()
    {
        if (_selectedAction == null || _isComplete) return;

        if (!_isRecording)
        {
            _isRecording = true;
            _recordingStartTime = Time.time;
        }
    }

    /// <summary>STT 키워드 매칭 시 호출. 선택한 카드 인덱스와 일치하면 성공, 아니면 실패.</summary>
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

    /// <summary>STT에서 매칭 실패 시 호출. 실패 처리로 전달한다.</summary>
    private void OnMicNoMatch(string result)
    {
        if (!_isRecording) return;
        _isRecording = false;
        OnFail();
    }

    // =========================
    // 성공 / 실패
    // =========================

    /// <summary>STT 매칭 실패 시 실패 가이드를 표시하고 2초 후 원래 가이드로 복귀한다.</summary>
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

    /// <summary>실패 가이드 텍스트를 표시한 뒤 2초 후 메인 가이드로 복귀하는 코루틴.</summary>
    private IEnumerator ShowFailGuideAndRevert()
    {
        GuideText.text = ProblemRuntime.L(GuideTextId_Fail);
        yield return new WaitForSeconds(2f);
        if (GuideText != null && GuideTextId_Main > 0 && !_isComplete)
            GuideText.text = ProblemRuntime.L(GuideTextId_Main);
        _guideRevertRoutine = null;
    }

    /// <summary>
    /// STT 매칭 성공. 녹음 소요 시간을 계산하여 DB 저장하고,
    /// 성공 가이드 텍스트, 마이크 숨김, 완료 처리를 수행한다.
    /// </summary>
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

        // 완료 처리
        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
    }
}
