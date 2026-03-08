using System;
using UnityEngine;
using UnityEngine.UI;

public interface IDirectorProblem2PerspectiveOption
{
    int Id { get; }
    string Text { get; }
    string[] Keywords { get; }
}

public abstract class Director_Problem2_Step3_Logic : ProblemStepBase
{
    [Serializable]
    public class SelectionSlot
    {
        public Button button;
        public GameObject outline;
        public Image image;
    }

    [Serializable]
    private class RefilmLogPayload
    {
        public string ngText;
        public int selectedId;
        public string selectedText;
        public bool recorded;
    }

    // ===== Data =====
    protected abstract string NgSentence { get; }
    protected abstract IDirectorProblem2PerspectiveOption[] Perspectives { get; }

    // ===== Guide Text (Retry만 스텝 로직에서 직접 사용) =====
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId_Retry { get; }

    // ===== UI =====
    protected abstract RectTransform SceneCardRect { get; }
    protected abstract GameObject OkSceneCard { get; }

    // 버튼 선택 UI
    protected abstract SelectionSlot[] SelectionSlots { get; }
    protected abstract Sprite[] PerspectiveSprites { get; }

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
    private IDirectorProblem2PerspectiveOption _selected;
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

        // 녹음 중에는 선택 버튼 비활성화
        SetSelectionButtonsInteractable(!isRecording);

        SetMicInteractable(!isRecording);
    }

    private void ResetState()
    {
        _selectedIndex = -1;
        _selected = null;
        _isRecording = false;
        _hasRecordedAnswer = false;
        _isFinished = false;

        if (StepRoot != null) StepRoot.SetActive(true);

        if (SceneCardRect != null) SceneCardRect.gameObject.SetActive(true);
        if (OkSceneCard != null) OkSceneCard.SetActive(false);

        // 슬롯 초기화: 스프라이트 세팅 + 아웃라인 끄기
        InitSlots();

        SetBeforeCompleteUI();

        // 선택 전이므로 마이크 비활성
        SetMicInteractable(false);

        var indicator2 = MicIndicator;
        if (indicator2 != null)
            indicator2.ResetIdleText();
    }

    private void InitSlots()
    {
        var slots = SelectionSlots;
        var sprites = PerspectiveSprites;
        if (slots == null) return;

        for (int i = 0; i < slots.Length; i++)
        {
            var s = slots[i];
            if (s == null) continue;

            if (s.outline != null) s.outline.SetActive(false);

            if (s.image != null && sprites != null && i < sprites.Length && sprites[i] != null)
                s.image.sprite = sprites[i];
        }
    }

    private void SetBeforeCompleteUI()
    {
        if (MicButtonRoot != null) MicButtonRoot.SetActive(true);
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
            int idx = i; // 클로저 캡처
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

        var p = Perspectives;
        if (p == null || index < 0 || index >= p.Length) return;

        _selectedIndex = index;
        _selected = p[index];

        // 아웃라인 갱신: 선택된 것만 켜기
        var slots = SelectionSlots;
        if (slots != null)
        {
            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i]?.outline != null)
                    slots[i].outline.SetActive(i == index);
            }
        }

        // 선택했으니 마이크 활성화
        SetMicInteractable(true);

        // MicIndicator에 현재 선택의 키워드 세팅
        var indicator = MicIndicator;
        if (indicator != null && _selected.Keywords != null)
            indicator.SetKeywords(_selected.Keywords);
    }

    // ===== Mic =====
    public void OnClickMic()
    {
        if (_isFinished) return;
        if (_selectedIndex < 0) return; // 선택 안 됨
    }

    // ===== STT =====
    private void OnSTTKeywordMatched(int matchedIndex)
    {
        if (_isFinished) return;
        if (_selectedIndex < 0) return;

        // 선택된 관점의 키워드만 세팅했으므로, 매칭 = 성공
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
        // 선택 버튼 비활성화
        SetSelectionButtonsInteractable(false);

        if (StepRoot != null) StepRoot.SetActive(false);
        if (OkSceneCard != null) OkSceneCard.SetActive(true);

        if (CompletionText != null && _selected != null)
            CompletionText.text = _selected.Text;

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
        var p = Perspectives;
        if (p == null || p.Length == 0 || _selectedIndex < 0) return;

        _selected = p[_selectedIndex];
        if (_selected == null) return;

        var body = new RefilmLogPayload
        {
            ngText = NgSentence,
            selectedId = _selected.Id,
            selectedText = _selected.Text,
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
