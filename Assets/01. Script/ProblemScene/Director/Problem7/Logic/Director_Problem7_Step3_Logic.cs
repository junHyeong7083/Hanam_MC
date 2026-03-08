using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem7 / Step3 로직 베이스
/// - "명대사 만들기"
/// - 대사 3개 중 하나 선택 → 마이크로 말하기 → STT로 선택한 문장 검증
/// </summary>
public abstract class Director_Problem7_Step3_Logic : ProblemStepBase
{
    // =========================
    // 선택지 데이터 구조
    // =========================

    [Serializable]
    public class DialogueItem
    {
        public string id;          // DB 저장용 ID
        public int textId;         // CSV textId (라벨 표시용)
        public Button button;      // 버튼 참조
        public GameObject selectImg;   // 선택 시 SetActive(true)할 이미지
    }

    // =========================
    // DB 저장용 DTO
    // =========================

    [Serializable]
    private class DialogueAttemptDto
    {
        public string id;
        public string text;
    }

    // =========================
    // 파생 클래스에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    [Header("HanamBox 가이드 텍스트")]
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId_Select { get; }
    protected abstract int GuideTextId_Complete { get; }
    protected abstract int GuideTextId_Retry { get; }

    [Header("대사 선택 화면")]
    protected abstract GameObject SelectDialogueRoot { get; }
    protected abstract DialogueItem[] DialogueChoices { get; }

    [Header("마이크 STT")]
    protected abstract MicRecordingIndicator MicIndicator { get; }
    protected abstract GameObject MicButtonRoot { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    #endregion

    // 내부 상태
    private int _selectedIndex = -1;
    private DialogueItem _selectedDialogue;
    private bool _isRecording;
    private bool _isFinished;
    private bool _interactionLocked = true;

    // =========================
    // ProblemStepBase 구현
    // =========================

    protected override void OnStepEnter()
    {
        _selectedIndex = -1;
        _selectedDialogue = null;
        _isRecording = false;
        _isFinished = false;

        ResetSelectImages();
        ApplyLabelsFromTextId();
        RegisterListeners();

        if (SelectDialogueRoot != null) SelectDialogueRoot.SetActive(true);
        if (MicButtonRoot != null) MicButtonRoot.SetActive(false);

        if (GuideText != null && GuideTextId_Select > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Select);

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

        _interactionLocked = true;
        _isRecording = false;
        RemoveAllListeners();
    }

    // =========================
    // 초기 설정
    // =========================

    private void ResetSelectImages()
    {
        var dialogues = DialogueChoices;
        if (dialogues == null) return;

        foreach (var choice in dialogues)
        {
            if (choice?.selectImg != null)
                choice.selectImg.SetActive(false);
        }
    }

    private void ApplyLabelsFromTextId()
    {
        var dialogues = DialogueChoices;
        if (dialogues == null) return;

        foreach (var choice in dialogues)
        {
            if (choice == null || choice.button == null || choice.textId <= 0) continue;
            var text = choice.button.GetComponentInChildren<Text>(true);
            if (text != null)
                text.text = ProblemRuntime.L(choice.textId);
        }
    }

    private void RegisterListeners()
    {
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
                    choice.button.onClick.AddListener(() => OnDialogueClicked(index));
                }
            }
        }

        var mic = MicIndicator;
        if (mic != null && dialogues != null)
        {
            var keywords = new string[dialogues.Length];
            for (int i = 0; i < dialogues.Length; i++)
            {
                keywords[i] = dialogues[i]?.textId > 0
                    ? ProblemRuntime.L(dialogues[i].textId)
                    : "";
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
        var dialogues = DialogueChoices;
        if (dialogues != null)
        {
            foreach (var choice in dialogues)
                if (choice?.button != null) choice.button.onClick.RemoveAllListeners();
        }

        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;
        }
    }

    // =========================
    // 대사 선택 (버튼 클릭)
    // =========================

    private void OnDialogueClicked(int index)
    {
        if (_interactionLocked) return;
        if (_isFinished) return;

        var dialogues = DialogueChoices;
        if (dialogues == null || index < 0 || index >= dialogues.Length) return;

        _selectedIndex = index;
        _selectedDialogue = dialogues[index];

        // 선택된 항목의 selectImg만 활성화, 나머지 비활성화
        for (int i = 0; i < dialogues.Length; i++)
        {
            var choice = dialogues[i];
            if (choice?.selectImg != null)
                choice.selectImg.SetActive(i == index);
        }

        // 마이크 버튼 표시
        if (MicButtonRoot != null) MicButtonRoot.SetActive(true);
    }

    // =========================
    // 마이크 버튼 핸들러
    // =========================

    public void OnClickMic()
    {
        if (_isFinished) return;
        if (_selectedIndex < 0) return;

        _isRecording = !_isRecording;
        SetChoicesHoverEnabled(!_isRecording);

        var mic = MicIndicator;
        if (mic != null)
            mic.ToggleRecording();
    }

    private void SetChoicesHoverEnabled(bool enabled)
    {
        if (SelectDialogueRoot == null) return;

        var hovers = SelectDialogueRoot.GetComponentsInChildren<ButtonHover>(true);
        foreach (var hover in hovers)
            hover.enabled = enabled;
    }

    // =========================
    // STT 이벤트 핸들러
    // =========================

    private void OnSTTKeywordMatched(int matchedIndex)
    {
        if (_isFinished) return;

        _isRecording = false;

        if (matchedIndex == _selectedIndex)
        {
            _isFinished = true;

            // 모든 버튼 리스너 제거 (interactable 유지로 알파 변화 방지)
            RemoveAllListeners();

            // 대사 버튼들 interactable = false
            var dialoguesRef = DialogueChoices;
            if (dialoguesRef != null)
            {
                foreach (var choice in dialoguesRef)
                    if (choice?.button != null)
                        choice.button.interactable = false;
            }

            SaveDialogueAttempt();

            if (GuideText != null && GuideTextId_Complete > 0)
                GuideText.text = ProblemRuntime.L(GuideTextId_Complete);

            if (MicButtonRoot != null) MicButtonRoot.SetActive(false);

            if (dialogueSequencer != null)
                dialogueSequencer.ShowCompletedText();
        }
        else
        {
            ShowRetryGuide();
        }
    }

    private void OnSTTNoMatch(string sttResult)
    {
        if (_isFinished) return;

        _isRecording = false;
        ShowRetryGuide();
    }

private void ShowRetryGuide()
    {
        // 녹음 종료 → hover 재활성화
        SetChoicesHoverEnabled(true);

        // 선택된 selectImg 비활성화
        if (_selectedDialogue?.selectImg != null)
            _selectedDialogue.selectImg.SetActive(false);

        if (GuideText != null && GuideTextId_Retry > 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Retry);

        // 재시도 TTS 재생
        if (GuideTextId_Retry > 0 && SoundManager.Instance != null)
            SoundManager.Instance.PlayTTS(GuideTextId_Retry);

        var mic = MicIndicator;
        if (mic != null)
            mic.SetIdleText("다시 말하기");
    }

    // =========================
    // DB 저장
    // =========================

    private void SaveDialogueAttempt()
    {
        if (_selectedDialogue == null) return;

        var body = new DialogueAttemptDto
        {
            id = _selectedDialogue.id,
            text = _selectedDialogue.textId > 0 ? ProblemRuntime.L(_selectedDialogue.textId) : ""
        };
        SaveAttempt(body);
    }
}
