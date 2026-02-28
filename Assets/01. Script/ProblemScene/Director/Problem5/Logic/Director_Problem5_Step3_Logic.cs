using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 대사 선택 타입
/// </summary>
public enum DialogueOptionType
{
    Avoidant,
    Healthy,
    Confrontational
}

/// <summary>
/// 대사 선택지 데이터 인터페이스 (textId + sprite 기반)
/// </summary>
public interface IDialogueOptionData
{
    int Id { get; }
    int TextId { get; }
    DialogueOptionType Type { get; }
    bool IsCorrect { get; }
    Sprite OptionSprite { get; }
}

/// <summary>
/// Problem5 / Step3 대사 연습 로직 베이스.
/// - 좌우 네비게이션으로 대사 탐색 (텍스트 + 이미지 교체)
/// - 말하기 → STT로 선택한 대사 확인
/// - 정답: 하남이 성공 텍스트 + NPC 반응 + 게이트 완료
/// - 오답: 하남이 오답 피드백 → 재시도
/// - 모든 텍스트는 CSV textId 기반
/// </summary>
public abstract class Director_Problem5_Step3_Logic : ProblemStepBase
{
    [Serializable]
    private class ClickLogEntry
    {
        public int id;
        public string text;
        public string type;
        public string inputMode;
        public float time;
    }

    [Serializable]
    private class DialogueAttemptBody
    {
        public int selectedId;
        public string selectedText;
        public string selectedType;
        public string inputMode;
        public bool npcResponded;
        public ClickLogEntry[] clickLogs;
    }

    // ===== 자식에서 주입할 추상 프로퍼티 =====

    protected abstract IDialogueOptionData[] Options { get; }

    protected abstract GameObject NpcResponseRoot { get; }
    protected abstract Text NpcResponseText { get; }
    protected abstract int NpcResponseTextId { get; }

    protected abstract MicRecordingIndicator MicIndicator { get; }
    protected abstract StepCompletionGate CompletionGate { get; }

    // ===== Hanam Guide Text (CSV textId) =====

    [Header("Hanam Guide Text")]
    [SerializeField] private Text hanamText;
    [SerializeField] private int hanamTextIdOnEnter = 0;
    [SerializeField] private int hanamTextIdOnCompleted = 0;
    [SerializeField] private int hanamTextIdOnWrong = 0;

    // ===== Option Display (공유, 네비게이션으로 교체) =====

    [Header("Option Display")]
    [SerializeField] private Text optionDisplayText;
    [SerializeField] private Image optionDisplayImage;

    // ===== Navigation =====

    [Header("Navigation")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    // ===== Select Outline (STT 인식 중 활성화) =====

    [Header("Select Outline")]
    [SerializeField] private GameObject selectOutline;

    // ===== Complete UI Toggle =====

    [Header("Complete UI Toggle")]
    [SerializeField] private GameObject micButtonRoot;
    [SerializeField] private GameObject nextStepButtonRoot;

    // ===== Timing =====

    [Header("Timing")]
    [SerializeField] private float wrongFeedbackShowDuration = 1.5f;

    // ===== 내부 상태 =====

    private int _currentDisplayIndex;
    private int _selectedIndex = -1;
    private bool _hasAnswered;
    private bool _npcResponded;
    private string _inputMode = "button";

    private Coroutine _optionRoutine;

    private readonly List<ClickLogEntry> _clickLogList = new List<ClickLogEntry>();
    private float _stepStartTime;

    // ===== ProblemStepBase Hooks =====

    protected override void OnStepEnter()
    {
        var options = Options;
        if (options == null || options.Length == 0)
        {
            Debug.LogWarning("[Problem5_Step3] Options 가 비어 있음");
            return;
        }

        _selectedIndex = -1;
        _hasAnswered = false;
        _npcResponded = false;
        _inputMode = "button";
        _currentDisplayIndex = 0;

        _clickLogList.Clear();
        _stepStartTime = Time.time;

        // 하남이 가이드 텍스트
        ApplyHanamText(hanamTextIdOnEnter);

        // NPC 응답 초기 숨김
        if (NpcResponseRoot != null) NpcResponseRoot.SetActive(false);

        // 아웃라인 초기 숨김
        if (selectOutline != null)
            selectOutline.SetActive(false);

        // 마이크 보이고, 다음 버튼 숨기기
        if (micButtonRoot != null)
            micButtonRoot.SetActive(true);
        if (nextStepButtonRoot != null)
            nextStepButtonRoot.SetActive(false);

        // 첫 번째 옵션 표시
        ShowCurrentOption();

        // 네비게이션 버튼 바인딩
        BindNavButtons();

        // MicIndicator STT 설정
        SetupSTT();

        // 게이트 리셋
        if (CompletionGate != null)
            CompletionGate.ResetGate(1);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_optionRoutine != null) StopCoroutine(_optionRoutine);
        _optionRoutine = null;

        UnbindNavButtons();

        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;
        }
    }

    // ===== 네비게이션 =====

    private void BindNavButtons()
    {
        if (prevButton != null)
        {
            prevButton.onClick.RemoveAllListeners();
            prevButton.onClick.AddListener(OnPrevOption);
        }
        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextOption);
        }
    }

    private void UnbindNavButtons()
    {
        if (prevButton != null)
            prevButton.onClick.RemoveAllListeners();
        if (nextButton != null)
            nextButton.onClick.RemoveAllListeners();
    }

    private void OnPrevOption()
    {
        if (_hasAnswered) return;
        var options = Options;
        if (options == null || options.Length == 0) return;
        _currentDisplayIndex = (_currentDisplayIndex - 1 + options.Length) % options.Length;
        ShowCurrentOption();
    }

    private void OnNextOption()
    {
        if (_hasAnswered) return;
        var options = Options;
        if (options == null || options.Length == 0) return;
        _currentDisplayIndex = (_currentDisplayIndex + 1) % options.Length;
        ShowCurrentOption();
    }

    private void ShowCurrentOption()
    {
        var options = Options;
        if (options == null || options.Length == 0) return;

        var opt = options[_currentDisplayIndex];

        if (optionDisplayText != null && opt.TextId > 0)
            optionDisplayText.text = ProblemRuntime.L(opt.TextId);

        if (optionDisplayImage != null && opt.OptionSprite != null)
            optionDisplayImage.sprite = opt.OptionSprite;
    }

    // ===== Hanam Text =====

    private void ApplyHanamText(int textId)
    {
        if (hanamText == null || textId <= 0) return;
        hanamText.text = ProblemRuntime.L(textId);
    }

    // ===== STT 설정 =====

    private void SetupSTT()
    {
        var mic = MicIndicator;
        if (mic == null) return;

        var options = Options;
        if (options == null) return;

        var keywordList = new List<string>();
        foreach (var opt in options)
        {
            keywordList.Add(ProblemRuntime.L(opt.TextId));
        }
        mic.SetKeywords(keywordList.ToArray());

        mic.OnKeywordMatched -= OnSTTKeywordMatched;
        mic.OnKeywordMatched += OnSTTKeywordMatched;
        mic.OnNoMatch -= OnSTTNoMatch;
        mic.OnNoMatch += OnSTTNoMatch;
    }

    // ===== STT 이벤트 =====

    protected void OnSTTKeywordMatched(int matchedIndex)
    {
        Debug.Log($"[Problem5_Step3] STT 매칭: index={matchedIndex}");

        // 아웃라인 끄기
        if (selectOutline != null)
            selectOutline.SetActive(false);

        if (_hasAnswered) return;

        _inputMode = "voice";
        OnSelectOption(matchedIndex);
    }

    protected void OnSTTNoMatch(string sttResult)
    {
        Debug.Log($"[Problem5_Step3] STT 매칭 실패: {sttResult}");

        if (selectOutline != null)
            selectOutline.SetActive(false);
    }

    /// <summary>
    /// 말하기 버튼에서 호출 → 아웃라인 활성화
    /// </summary>
    public void OnStartRecording()
    {
        if (selectOutline != null)
            selectOutline.SetActive(true);
    }

    // ===== 선택 흐름 =====

    public void OnSelectOption(int index)
    {
        if (_hasAnswered) return;

        var options = Options;
        if (options == null || index < 0 || index >= options.Length) return;

        _selectedIndex = index;
        LogClick(index);

        if (_optionRoutine != null)
        {
            StopCoroutine(_optionRoutine);
            _optionRoutine = null;
        }
        _optionRoutine = StartCoroutine(OptionSelectFlow(index));
    }

    private IEnumerator OptionSelectFlow(int index)
    {
        var options = Options;
        if (options == null || index < 0 || index >= options.Length) yield break;

        var opt = options[index];
        bool isCorrect = opt.IsCorrect;

        if (isCorrect)
        {
            // 하남이 성공 텍스트
            ApplyHanamText(hanamTextIdOnCompleted);

            _hasAnswered = true;
            _npcResponded = true;

            // NPC 응답
            if (NpcResponseRoot != null) NpcResponseRoot.SetActive(true);
            if (NpcResponseText != null && NpcResponseTextId > 0)
                NpcResponseText.text = ProblemRuntime.L(NpcResponseTextId);

            // 마이크 숨기고 다음 버튼 보이기
            if (micButtonRoot != null)
                micButtonRoot.SetActive(false);
            if (nextStepButtonRoot != null)
                nextStepButtonRoot.SetActive(true);

            // 게이트 완료
            if (CompletionGate != null)
                CompletionGate.MarkOneDone();
        }
        else
        {
            // 하남이 오답 텍스트
            ApplyHanamText(hanamTextIdOnWrong);

            // 잠시 대기 후 복원
            float dur = Mathf.Max(0f, wrongFeedbackShowDuration);
            if (dur > 0f)
                yield return new WaitForSeconds(dur);

            // 가이드 텍스트 복원
            ApplyHanamText(hanamTextIdOnEnter);
        }

        _optionRoutine = null;
    }

    // ===== 클릭 로그 =====

    private void LogClick(int index)
    {
        var options = Options;
        if (options == null || index < 0 || index >= options.Length) return;

        var opt = options[index];
        _clickLogList.Add(new ClickLogEntry
        {
            id = opt.Id,
            text = ProblemRuntime.L(opt.TextId),
            type = ToTypeString(opt.Type),
            inputMode = _inputMode,
            time = Time.time - _stepStartTime
        });
    }

    // ===== DB 저장 =====

    public void OnClickContinue()
    {
        var options = Options;
        if (options == null || _selectedIndex < 0 || _selectedIndex >= options.Length) return;

        var opt = options[_selectedIndex];
        SaveAttempt(new DialogueAttemptBody
        {
            selectedId = opt.Id,
            selectedText = ProblemRuntime.L(opt.TextId),
            selectedType = ToTypeString(opt.Type),
            inputMode = _inputMode,
            npcResponded = _npcResponded,
            clickLogs = _clickLogList.ToArray()
        });
    }

    private string ToTypeString(DialogueOptionType type)
    {
        switch (type)
        {
            case DialogueOptionType.Avoidant: return "avoidant";
            case DialogueOptionType.Healthy: return "healthy";
            case DialogueOptionType.Confrontational: return "confrontational";
        }
        return type.ToString();
    }
}
