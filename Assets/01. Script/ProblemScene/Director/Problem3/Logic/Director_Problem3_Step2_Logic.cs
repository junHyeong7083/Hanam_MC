using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 재작성 라운드 데이터 인터페이스.
/// 각 라운드의 원본 텍스트, 재작성 텍스트, 옵션 목록, 키워드, 스프라이트 등을 정의한다.
/// </summary>
public interface IRewriteStepData
{
    int Id { get; }                    // 라운드 ID (로그용)
    string OriginalText { get; }       // 원본 텍스트 (재작성 전)
    string RewrittenText { get; }      // 재작성된 텍스트 (애니메이션 후)
    string[] Options { get; }          // 캐러셀 옵션 텍스트 배열
    string[][] OptionKeywords { get; } // 각 옵션별 STT 매칭 키워드 2차원 배열 (null 허용)
    Sprite[] OptionSprites { get; }    // 각 옵션별 표시 스프라이트 배열 (null 허용)
    int AfterCompleteTextId { get; }   // 라운드 완료 시 가이드 텍스트 ID (0이면 공통 GuideTextId_After 사용)
}

/// <summary>
/// Director_Problem3_Step2_Logic - 문제3 스텝2의 비즈니스 로직 베이스 클래스.
///
/// 【역할】 "재작성(Rewrite)" 활동을 담당한다. 여러 라운드로 구성되며, 각 라운드에서:
///         1) 원본 텍스트를 EffectController로 표시
///         2) 캐러셀 UI로 대안 옵션을 좌우 탐색
///         3) 마이크로 현재 옵션을 읽으면(STT) 재작성 애니메이션 재생
///         4) "다음 대사" 버튼으로 다음 라운드 또는 완료로 진행
///         모든 라운드 완료 시 DB에 결과를 저장한다.
/// 【패턴】 Binder/Logic 패턴의 Logic 측.
/// 【문제/스텝】 Director 테마 / 문제3 / 스텝2 (메인 활동 - 캐러셀 + STT 재작성)
/// 【부모 클래스】 ProblemStepBase → OnStepEnter()/OnStepExit()
/// 【참조하는 곳】 Director_Problem3_Step2 (Binder 자식 클래스)
/// 【참조되는 곳】 IRewriteStepData (라운드 데이터 인터페이스),
///               Problem3_Step2_EffectController (원본→재작성 텍스트 애니메이션),
///               MicRecordingIndicator (STT), DialogueSequencer (대사),
///               StepCompletionGate (완료 판정)
/// 【흐름】 스텝 진입 → enter 대사 → 첫 라운드 시작 → 캐러셀 탐색 → STT 녹음 →
///         매칭 성공 → 재작성 애니메이션 → 다음 라운드 버튼 → ... → 마지막 라운드 완료 →
///         DB 저장 → completed 대사 → 다음 스텝
/// </summary>
public abstract class Director_Problem3_Step2_Logic : ProblemStepBase
{
    [Header("재작성 단계 데이터 (자식 구현)")]
    protected abstract IRewriteStepData[] Steps { get; }

    [Header("이펙트 컨트롤러")]
    protected abstract Problem3_Step2_EffectController EffectController { get; }

    [Header("상단 가이드 텍스트")]
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId_Before { get; }
    protected abstract int GuideTextId_After { get; }
    protected abstract int GuideTextId_BetweenRounds { get; }  // 중간 스텝 완료 시 (마지막 제외)

    [Header("캐러셀 UI")]
    protected abstract GameObject CarouselRoot { get; }
    protected abstract Button PrevButton { get; }
    protected abstract Button NextButton { get; }
    protected abstract Text CarouselText { get; }
    protected abstract Text CarouselIndexText { get; }
    protected abstract Image OptionImage { get; }

    [Header("마이크 UI")]
    protected abstract GameObject MicButtonRoot { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }
    protected abstract GameObject RecordingOverlay { get; }

    [Header("상단 진행도 점들 (옵션)")]
    protected abstract GameObject[] ProgressDots { get; }

    [Header("상단/하단 다음 버튼")]
    protected abstract GameObject NextDialogButtonRoot { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    [Header("완료 게이트 (옵션)")]
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("옵션")]
    protected abstract float RewriteDelay { get; }

    private int _stepIndex;
    private int _currentOptionIndex;
    private bool _isRecording;
    private bool _isStepCompleted;
    private bool _interactionLocked = true;

    [Serializable]
    private class AttemptStepLog
    {
        public int stepId;
        public string originalText;
        public int selectedOptionIndex;
        public string selectedOption;
        public string rewrittenText;
        public bool recorded;
    }

    [Serializable]
    private class AttemptBody
    {
        public AttemptStepLog[] steps;
    }

    private int[] _selectedOptionIndices;
    private bool[] _recordedFlags;

    private bool IsAnimating => EffectController != null && EffectController.IsAnimating;

    protected override void OnStepEnter()
    {
        Debug.Log("[Problem3_Step2] OnStepEnter (Carousel/Mic)");

        var steps = Steps;
        if (steps == null || steps.Length == 0)
        {
            Debug.LogWarning("[Problem3_Step2] Steps 데이터가 비어있음");
            if (CompletionGate != null) CompletionGate.ResetGate(1);
            return;
        }

        _stepIndex = 0;
        _currentOptionIndex = 0;
        _isRecording = false;
        _isStepCompleted = false;

        _selectedOptionIndices = new int[steps.Length];
        _recordedFlags = new bool[steps.Length];
        for (int i = 0; i < steps.Length; i++)
        {
            _selectedOptionIndices[i] = -1;
            _recordedFlags[i] = false;
        }

        if (CompletionGate != null) CompletionGate.ResetGate(1);

        BindCarouselButtons();
        BindMicEvents();

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;

        EnterInnerStep(_stepIndex);
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

        UnbindCarouselButtons();
        UnbindMicEvents();
    }

    private void EnterInnerStep(int index)
    {
        var steps = Steps;
        if (steps == null || index < 0 || index >= steps.Length) return;

        _stepIndex = index;
        _currentOptionIndex = 0;
        _isRecording = false;
        _isStepCompleted = false;

        if (GuideText != null && GuideTextId_Before != 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Before);

        if (GuideTextId_Before != 0 && SoundManager.Instance != null)
            SoundManager.Instance.PlayTTS(GuideTextId_Before);

        SetBeforeCompleteUI();

        ApplyProgressDots();
        ApplyOriginalText();
        RefreshCarouselUI();

        ConfigureMicKeywordsForCurrentStep();
    }

    private void ApplyProgressDots()
    {
        var dots = ProgressDots;
        if (dots == null || dots.Length == 0) return;

        for (int i = 0; i < dots.Length; i++)
        {
            if (dots[i] != null)
                dots[i].SetActive(i == _stepIndex);
        }
    }

    private void ApplyOriginalText()
    {
        var step = Steps[_stepIndex];
        var effect = EffectController;
        if (effect != null)
        {
            effect.ResetForNextStep();
            effect.ShowOriginalTextImmediate(step.OriginalText);
        }
    }

    private void RefreshCarouselUI()
    {
        var step = Steps[_stepIndex];
        var options = step.Options ?? Array.Empty<string>();

        if (CarouselRoot != null) CarouselRoot.SetActive(true);

        if (options.Length == 0)
        {
            if (CarouselText != null) CarouselText.text = "";
            if (CarouselIndexText != null) CarouselIndexText.text = "";

            if (PrevButton != null) PrevButton.interactable = false;
            if (NextButton != null) NextButton.interactable = false;

            SetMicInteractable(false);
            return;
        }

        if (_currentOptionIndex < 0) _currentOptionIndex = 0;
        if (_currentOptionIndex >= options.Length) _currentOptionIndex = options.Length - 1;

        if (CarouselText != null) CarouselText.text = options[_currentOptionIndex];
        if (CarouselIndexText != null) CarouselIndexText.text = $"{_currentOptionIndex + 1}/{options.Length}";

        // 옵션 인덱스에 맞는 스프라이트 교체
        var optSprites = step.OptionSprites;
        var optImg = OptionImage;
        if (optImg != null && optSprites != null && _currentOptionIndex < optSprites.Length && optSprites[_currentOptionIndex] != null)
            optImg.sprite = optSprites[_currentOptionIndex];

        bool canNav = !_isStepCompleted && options.Length > 1 && !IsAnimating;
        if (PrevButton != null) PrevButton.interactable = canNav;
        if (NextButton != null) NextButton.interactable = canNav;

        SetMicInteractable(!_isStepCompleted && !IsAnimating);
    }

    private void BindCarouselButtons()
    {
        if (PrevButton != null)
        {
            PrevButton.onClick.RemoveListener(OnClickPrev);
            PrevButton.onClick.AddListener(OnClickPrev);
        }

        if (NextButton != null)
        {
            NextButton.onClick.RemoveListener(OnClickNext);
            NextButton.onClick.AddListener(OnClickNext);
        }
    }

    private void UnbindCarouselButtons()
    {
        if (PrevButton != null) PrevButton.onClick.RemoveListener(OnClickPrev);
        if (NextButton != null) NextButton.onClick.RemoveListener(OnClickNext);
    }

    private void BindMicEvents()
    {
        var indicator = MicIndicator;
        if (indicator == null) return;

        indicator.OnKeywordMatched -= OnSTTKeywordMatched;
        indicator.OnKeywordMatched += OnSTTKeywordMatched;

        indicator.OnNoMatch -= OnSTTNoMatch;
        indicator.OnNoMatch += OnSTTNoMatch;

        indicator.OnRecordingChanged -= OnMicRecordingChanged;
        indicator.OnRecordingChanged += OnMicRecordingChanged;
    }

    private void UnbindMicEvents()
    {
        var indicator = MicIndicator;
        if (indicator == null) return;

        indicator.OnKeywordMatched -= OnSTTKeywordMatched;
        indicator.OnNoMatch -= OnSTTNoMatch;
        indicator.OnRecordingChanged -= OnMicRecordingChanged;
    }

    private void OnMicRecordingChanged(bool isRecording)
    {
        if (RecordingOverlay != null)
            RecordingOverlay.SetActive(isRecording);

        if (isRecording)
        {
            // 녹음 시작 시: Prev/Next 숨기고, 마이크 중복 클릭 방지
            if (PrevButton != null) PrevButton.gameObject.SetActive(false);
            if (NextButton != null) NextButton.gameObject.SetActive(false);
            SetMicInteractable(false);
        }
        // 녹음 종료 후 UI 복원은 OnSTTNoMatch 또는 SetAfterCompleteUI에서 처리
    }

    private void OnClickPrev()
    {
        if (_interactionLocked) return;
        if (_isStepCompleted || IsAnimating) return;

        var options = Steps[_stepIndex].Options ?? Array.Empty<string>();
        if (options.Length == 0) return;

        _currentOptionIndex--;
        if (_currentOptionIndex < 0) _currentOptionIndex = options.Length - 1;

        RefreshCarouselUI();
        ConfigureMicKeywordsForCurrentStep();
    }

    private void OnClickNext()
    {
        if (_interactionLocked) return;
        if (_isStepCompleted || IsAnimating) return;

        var options = Steps[_stepIndex].Options ?? Array.Empty<string>();
        if (options.Length == 0) return;

        _currentOptionIndex++;
        if (_currentOptionIndex >= options.Length) _currentOptionIndex = 0;

        RefreshCarouselUI();
        ConfigureMicKeywordsForCurrentStep();
    }

    public void OnClickMic()
    {
        if (_interactionLocked) return;
        if (_isStepCompleted || IsAnimating) return;

        var options = Steps[_stepIndex].Options ?? Array.Empty<string>();
        if (options.Length == 0) return;

        var indicator = MicIndicator;
        if (indicator != null)
            indicator.ToggleRecording();
        // RecordingOverlay, PrevBtn, NextBtn 토글은 OnMicRecordingChanged 이벤트에서 처리
    }

    // 핵심 수정: MicRecordingIndicator(SetKeywords)에 실제로 키워드를 넣어준다.
    private void ConfigureMicKeywordsForCurrentStep()
    {
        var indicator = MicIndicator;
        if (indicator == null) return;

        var step = Steps[_stepIndex];
        var options = step.Options ?? Array.Empty<string>();

        string[] keywordsToSet = BuildIndicatorKeywords(step, options);

        indicator.SetKeywords(keywordsToSet);

        Debug.Log($"[Problem3_Step2] SetKeywords stepIndex={_stepIndex} -> {string.Join(" | ", keywordsToSet)}");
    }

    // MicRecordingIndicator는 string[]만 받으므로,
    // 옵션별 키워드 그룹(string[][])이 있으면 각 옵션의 "대표 키워드" 1개씩만 뽑아준다.
    private string[] BuildIndicatorKeywords(IRewriteStepData step, string[] options)
    {
        int n = options.Length;
        var result = new string[n];

        var kwGroups = step.OptionKeywords;

        for (int i = 0; i < n; i++)
        {
            string v = null;

            if (kwGroups != null && i < kwGroups.Length && kwGroups[i] != null && kwGroups[i].Length > 0)
            {
                // 대표 키워드(첫 번째)
                v = kwGroups[i][0];
            }

            if (string.IsNullOrWhiteSpace(v))
            {
                // 키워드가 없으면 옵션 텍스트 자체를 사용
                v = (i < options.Length) ? options[i] : "";
            }

            result[i] = v;
        }

        return result;
    }

    private void OnSTTKeywordMatched(int matchedIndex)
    {
        Debug.Log($"[Problem3_Step2] OnKeywordMatched matchedIndex={matchedIndex}, currentOption={_currentOptionIndex}, stepIndex={_stepIndex}");

        if (_isStepCompleted || IsAnimating) return;

        _isRecording = false;
        if (RecordingOverlay != null) RecordingOverlay.SetActive(false);

        if (matchedIndex != _currentOptionIndex)
            return;

        _recordedFlags[_stepIndex] = true;
        _selectedOptionIndices[_stepIndex] = _currentOptionIndex;

        StartCoroutine(PlayRewriteCompleteSequence());
    }

    private void OnSTTNoMatch(string sttResult)
    {
        Debug.Log($"[Problem3_Step2] OnNoMatch result={sttResult}");

        // 녹음 실패: Prev/Next 다시 보이고, 마이크 버튼 다시 활성화
        if (PrevButton != null) PrevButton.gameObject.SetActive(true);
        if (NextButton != null) NextButton.gameObject.SetActive(true);
        SetMicInteractable(true);
    }

    private IEnumerator PlayRewriteCompleteSequence()
    {
        _isStepCompleted = true;

        var stepForText = Steps[_stepIndex];
        bool isLastStep = (_stepIndex == Steps.Length - 1);

        int afterTextId;
        if (!isLastStep && GuideTextId_BetweenRounds != 0)
            afterTextId = GuideTextId_BetweenRounds;
        else if (stepForText.AfterCompleteTextId != 0)
            afterTextId = stepForText.AfterCompleteTextId;
        else
            afterTextId = GuideTextId_After;

        Debug.Log($"[Problem3_Step2] PlayRewriteCompleteSequence stepIndex={_stepIndex} isLast={isLastStep} " +
                  $"BetweenRounds={GuideTextId_BetweenRounds} AfterComplete={stepForText.AfterCompleteTextId} " +
                  $"GuideTextId_After={GuideTextId_After} -> afterTextId={afterTextId} GuideText={(GuideText != null ? "OK" : "NULL")}");

        if (GuideText != null && afterTextId != 0)
            GuideText.text = ProblemRuntime.L(afterTextId);

        // BetweenRounds 텍스트(중간 라운드 완료)는 TTS 재생 안 함
        if (!isLastStep && GuideTextId_BetweenRounds != 0)
        {
            // TTS 없음
        }
        else if (afterTextId != 0 && SoundManager.Instance != null)
            SoundManager.Instance.PlayTTS(afterTextId);

        SetAfterCompleteUI();

        float delay = RewriteDelay;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        var step = Steps[_stepIndex];
        var effect = EffectController;
        if (effect != null)
            effect.PlayRewriteSequence(step.RewrittenText, null);
    }

    public void OnClickNextDialog()
    {
        if (IsAnimating) return;
        if (!_isStepCompleted) return;

        var steps = Steps;
        bool isLast = (_stepIndex == steps.Length - 1);

        if (!isLast)
        {
            EnterInnerStep(_stepIndex + 1);
        }
        else
        {
            SaveRewriteLogToDb();

            if (CompletionGate != null)
                CompletionGate.MarkOneDone();

            if (dialogueSequencer != null)
                dialogueSequencer.ShowCompletedText();
        }
    }

    private void SetBeforeCompleteUI()
    {
        if (MicButtonRoot != null) MicButtonRoot.SetActive(true);
        if (NextDialogButtonRoot != null) NextDialogButtonRoot.SetActive(false);
        if (RecordingOverlay != null) RecordingOverlay.SetActive(false);

        if (PrevButton != null) PrevButton.gameObject.SetActive(true);
        if (NextButton != null) NextButton.gameObject.SetActive(true);
        SetMicInteractable(true);
    }

    private void SetAfterCompleteUI()
    {
        if (RecordingOverlay != null) RecordingOverlay.SetActive(false);

        if (MicButtonRoot != null) MicButtonRoot.SetActive(false);
        if (NextDialogButtonRoot != null) NextDialogButtonRoot.SetActive(true);

        // gameObject 비활성화로 완전히 숨김 (interactable=false 시 dim 방지)
        if (PrevButton != null) PrevButton.gameObject.SetActive(false);
        if (NextButton != null) NextButton.gameObject.SetActive(false);
    }

    private void SetMicInteractable(bool interactable)
    {
        if (MicButtonRoot == null) return;

        var btn = MicButtonRoot.GetComponentInChildren<Button>(true);
        if (btn != null) btn.interactable = interactable;
    }

    private void SaveRewriteLogToDb()
    {
        var steps = Steps;
        if (steps == null || steps.Length == 0) return;

        int len = steps.Length;
        var logs = new AttemptStepLog[len];

        for (int i = 0; i < len; i++)
        {
            var s = steps[i];
            int selIndex = (i < _selectedOptionIndices.Length) ? _selectedOptionIndices[i] : -1;

            string selectedOptionText = null;
            var options = s.Options ?? Array.Empty<string>();
            if (selIndex >= 0 && selIndex < options.Length)
                selectedOptionText = options[selIndex];

            logs[i] = new AttemptStepLog
            {
                stepId = s.Id,
                originalText = s.OriginalText,
                selectedOptionIndex = selIndex,
                selectedOption = selectedOptionText,
                rewrittenText = s.RewrittenText,
                recorded = (i < _recordedFlags.Length) && _recordedFlags[i]
            };
        }

        var body = new AttemptBody { steps = logs };
        SaveAttempt(body);

        Debug.Log("[Problem3_Step2] SaveRewriteLogToDb 완료");
    }
}