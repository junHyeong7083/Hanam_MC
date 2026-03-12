using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem2_Step3_Logic - 문제2 스텝3의 비즈니스 로직 베이스 클래스.
///
/// 【역할】 NG 문장을 OK 문장으로 "재촬영(Refilm)"하는 활동을 담당한다.
///         사용자가 관점 선택 버튼 중 하나를 고른 후 마이크로 해당 텍스트를 읽으면(STT),
///         NG 씬 카드가 OK 씬 카드로 전환되고 완료 처리된다.
///         STT 실패 시 재시도 안내 텍스트를 표시한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 측.
/// 【문제/스텝】 Director 테마 / 문제2 / 스텝3 (마무리 - 재촬영 활동)
/// 【부모 클래스】 ProblemStepBase → OnStepEnter()/OnStepExit()
/// 【참조하는 곳】 Director_Problem2_Step3 (Binder 자식 클래스)
/// 【참조되는 곳】 DialogueSequencer (대사), MicRecordingIndicator (STT),
///               StepCompletionGate (완료 판정)
/// 【흐름】 스텝 진입 → enter 대사 → 관점 선택 → 마이크 버튼 표시 → STT 녹음 →
///         키워드 매칭 성공 → NG→OK 전환 → completed 대사 → DB 저장 → 다음 스텝
/// </summary>
public abstract class Director_Problem2_Step3_Logic : ProblemStepBase
{
    /// <summary>관점 선택 슬롯 데이터. 버튼, 아웃라인, 텍스트, textId를 포함한다.</summary>
    [Serializable]
    public class SelectionSlot
    {
        public Button button;          // 선택 버튼
        public GameObject outline;     // 선택 시 표시되는 아웃라인 오브젝트
        public Text text;             // 관점 텍스트 UI
        public int textId;            // CSV textId (텍스트 내용 + STT 키워드)
    }

    /// <summary>재촬영 결과 DB 저장용 페이로드</summary>
    [Serializable]
    private class RefilmLogPayload
    {
        public string ngText;          // NG 문장 원문
        public int selectedId;         // 선택된 슬롯 인덱스
        public string selectedText;    // 선택된 관점 텍스트
        public bool recorded;          // STT 녹음 완료 여부
    }

    // ===== Data =====
    protected abstract string NgSentence { get; }

    // ===== Guide Text (Retry만 스텝 로직에서 직접 사용) =====
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId_Retry { get; }

    // ===== UI =====
    protected abstract RectTransform SceneCardRect { get; }
    protected abstract GameObject OkSceneCard { get; }

    // 버튼 선택 UI
    protected abstract SelectionSlot[] SelectionSlots { get; }

    // 마이크
    protected abstract GameObject MicButtonRoot { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }

    protected abstract GameObject RecordingOverlay { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    // 패널
    protected abstract GameObject StepRoot { get; }

    protected abstract Text CompletionText { get; }
    protected abstract StepCompletionGate CompletionGate { get; }

    private int _selectedIndex = -1;
    private bool _isRecording;
    private bool _hasRecordedAnswer;
    private bool _isFinished;
    private bool _interactionLocked = true;

    protected override void OnStepEnter()
    {
        ResetState();

        var indicator = MicIndicator;
        if (indicator != null)
        {
            indicator.OnKeywordMatched -= OnSTTKeywordMatched;
            indicator.OnKeywordMatched += OnSTTKeywordMatched;

            indicator.OnNoMatch -= OnSTTNoMatch;
            indicator.OnNoMatch += OnSTTNoMatch;

            indicator.OnRecordingChanged -= OnMicRecordingChanged;
            indicator.OnRecordingChanged += OnMicRecordingChanged;
        }

        var gate = CompletionGate;
        if (gate != null) gate.ResetGate(1);

        BindSelectionButtons();
        BindMicButton();

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
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;

        _interactionLocked = true;

        var indicator = MicIndicator;
        if (indicator != null)
        {
            indicator.OnKeywordMatched -= OnSTTKeywordMatched;
            indicator.OnNoMatch -= OnSTTNoMatch;
            indicator.OnRecordingChanged -= OnMicRecordingChanged;
        }

        UnbindSelectionButtons();
        UnbindMicButton();
    }

    private void OnMicRecordingChanged(bool isRecording)
    {
        _isRecording = isRecording;

        if (RecordingOverlay != null)
            RecordingOverlay.SetActive(isRecording);

        SetSelectionButtonsInteractable(!isRecording);
        SetMicInteractable(!isRecording);
    }

    private void ResetState()
    {
        _selectedIndex = -1;
        _isRecording = false;
        _hasRecordedAnswer = false;
        _isFinished = false;

        if (StepRoot != null) StepRoot.SetActive(true);

        if (SceneCardRect != null) SceneCardRect.gameObject.SetActive(true);
        if (OkSceneCard != null) OkSceneCard.SetActive(false);

        InitSlots();
        SetBeforeCompleteUI();
        SetMicInteractable(false);

        var indicator2 = MicIndicator;
        if (indicator2 != null)
            indicator2.ResetIdleText();
    }

    private void InitSlots()
    {
        var slots = SelectionSlots;
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s == null) continue;

            if (s.outline != null) s.outline.SetActive(false);

            if (s.text != null && s.textId > 0)
                s.text.text = ProblemRuntime.L(s.textId);
        }
    }

    private SelectionSlot GetSelectedSlot()
    {
        var slots = SelectionSlots;
        if (slots == null || _selectedIndex < 0 || _selectedIndex >= slots.Length) return null;
        return slots[_selectedIndex];
    }

    private void SetBeforeCompleteUI()
    {
        if (MicButtonRoot != null) MicButtonRoot.SetActive(false);
        if (RecordingOverlay != null) RecordingOverlay.SetActive(false);
    }

    private void SetAfterCompleteUI()
    {
        if (RecordingOverlay != null) RecordingOverlay.SetActive(false);
        if (MicButtonRoot != null) MicButtonRoot.SetActive(false);
        SetMicInteractable(false);
    }

    // ===== Selection Buttons =====
    private void BindSelectionButtons()
    {
        var slots = SelectionSlots;
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null || slots[i].button == null) continue;
            int idx = i;
            slots[i].button.onClick.RemoveAllListeners();
            slots[i].button.onClick.AddListener(() => OnSlotClicked(idx));
        }
    }

    private void UnbindSelectionButtons()
    {
        var slots = SelectionSlots;
        if (slots == null) return;

        foreach (var s in slots)
        {
            if (s?.button != null)
                s.button.onClick.RemoveAllListeners();
        }
    }

    private void SetSelectionButtonsInteractable(bool interactable)
    {
        var slots = SelectionSlots;
        if (slots == null) return;

        foreach (var s in slots)
        {
            if (s?.button != null)
                s.button.interactable = interactable;
        }
    }

    private void OnSlotClicked(int index)
    {
        if (_interactionLocked) return;
        if (_isFinished) return;

        var slots = SelectionSlots;
        if (slots == null || index < 0 || index >= slots.Length) return;

        _selectedIndex = index;

        // 아웃라인 갱신: 선택된 것만 켜기
        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i]?.outline != null)
                slots[i].outline.SetActive(i == index);
        }

        if (MicButtonRoot != null) MicButtonRoot.SetActive(true);
        SetMicInteractable(true);

        // 선택한 슬롯의 텍스트를 키워드로 세팅
        var slot = slots[index];
        var indicator = MicIndicator;
        if (indicator != null && slot.textId > 0)
            indicator.SetKeywords(new[] { ProblemRuntime.L(slot.textId) });
    }

    // ===== Mic =====
    public void OnClickMic()
    {
        if (_isFinished) return;
        if (_selectedIndex < 0) return;

        var indicator = MicIndicator;
        if (indicator != null)
            indicator.ToggleRecording();
    }

    // ===== STT =====
    private void OnSTTKeywordMatched(int matchedIndex)
    {
        if (_isFinished) return;
        if (_selectedIndex < 0) return;

        _hasRecordedAnswer = true;
        _isRecording = false;

        if (RecordingOverlay != null)
            RecordingOverlay.SetActive(false);

        PlayRefilmComplete();
    }

    private void OnSTTNoMatch(string sttResult)
    {
        _isRecording = false;
        if (RecordingOverlay != null)
            RecordingOverlay.SetActive(false);

        SetSelectionButtonsInteractable(true);

        if (GuideText != null)
        {
            var retryTextId = GuideTextId_Retry;
            GuideText.text = retryTextId != 0
                ? ProblemRuntime.L(retryTextId)
                : "조금 더 가까이서 힘차게 말해주세요!";
        }

        var indicator = MicIndicator;
        if (indicator != null)
            indicator.SetIdleText("다시 말하기");
    }

    // ===== Complete =====
    private void PlayRefilmComplete()
    {
        SetSelectionButtonsInteractable(false);

        if (StepRoot != null) StepRoot.SetActive(false);
        if (OkSceneCard != null) OkSceneCard.SetActive(true);

        var slot = GetSelectedSlot();
        if (CompletionText != null && slot != null && slot.textId > 0)
            CompletionText.text = ProblemRuntime.L(slot.textId);

        _isFinished = true;

        SetAfterCompleteUI();

        var gate = CompletionGate;
        if (gate != null)
            gate.MarkOneDone();

        if (dialogueSequencer != null)
            dialogueSequencer.ShowCompletedText();
    }

    public void OnClickSummaryButton()
    {
        SaveRefilmLogToDb();
    }

    private void SaveRefilmLogToDb()
    {
        var slot = GetSelectedSlot();
        if (slot == null) return;

        var body = new RefilmLogPayload
        {
            ngText = NgSentence,
            selectedId = _selectedIndex,
            selectedText = slot.textId > 0 ? ProblemRuntime.L(slot.textId) : "",
            recorded = _hasRecordedAnswer
        };

        SaveAttempt(body);
    }

    private Button _micButton;

    private void BindMicButton()
    {
        if (MicButtonRoot == null) return;
        _micButton = MicButtonRoot.GetComponentInChildren<Button>(true);
    }

    private void UnbindMicButton()
    {
        _micButton = null;
    }

    private void SetMicInteractable(bool interactable)
    {
        if (_micButton != null)
            _micButton.interactable = interactable;
    }
}
