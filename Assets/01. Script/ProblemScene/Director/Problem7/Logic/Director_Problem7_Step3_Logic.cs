using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem7_Step3_Logic - 문제7 스텝3 명대사 만들기 로직 (추상 클래스)
///
/// 【역할】 "명대사 만들기" 테마에서 대사 3개 중 하나를 선택하고,
///          마이크로 직접 말하면 STT가 선택한 문장과 매칭하여 검증한다.
///          매칭 성공 시 DB 저장 및 완료 처리, 실패 시 재시도 안내.
/// 【패턴】 Binder/Logic 패턴의 Logic 계층. SerializeField는 Binder(Director_Problem7_Step3)에서 바인딩.
/// 【문제/스텝】 Director 테마 > 문제7 > 스텝3 (마무리 - STT 명대사 말하기)
/// 【부모 클래스】 ProblemStepBase
/// 【참조하는 곳】 Director_Problem7_Step3 (Binder)
/// 【참조되는 곳】 ProblemStepBase, DialogueSequencer, MicRecordingIndicator, SoundManager
/// 【흐름】 스텝 진입 → 대화 재생 → 대사 3개 중 1개 선택 → 마이크 버튼 표시
///         → 녹음 시작/중지 → STT 키워드 매칭 → 성공: DB 저장 + 완료 / 실패: 재시도 안내
/// </summary>
public abstract class Director_Problem7_Step3_Logic : ProblemStepBase
{
    // =========================
    // 선택지 데이터 구조
    // =========================

    /// <summary>
    /// DialogueItem - 명대사 선택지 한 개의 데이터와 UI 참조.
    /// 3개의 대사 중 하나를 선택하면 해당 selectImg가 활성화된다.
    /// </summary>
    [Serializable]
    public class DialogueItem
    {
        public string id;          // DB 저장용 식별자
        public int textId;         // CSV textId (대사 텍스트 표시용)
        public Button button;      // 대사 선택 버튼
        public GameObject selectImg;   // 선택 시 활성화할 시각적 표시 이미지
    }

    // =========================
    // DB 저장용 DTO
    // =========================

    /// <summary>선택된 대사의 ID와 텍스트를 담는 DTO</summary>
    [Serializable]
    private class DialogueAttemptDto
    {
        public string id;    // 선택된 대사 식별자
        public string text;  // 선택된 대사 텍스트
    }

    // =========================
    // 파생 클래스(Binder)에서 넘겨줄 UI 참조
    // =========================

    #region Abstract Properties

    /// <summary>STT 매칭 실패 시 표시할 재시도 안내 텍스트의 CSV textId</summary>
    [Header("재시도 텍스트")]
    protected abstract int RetryTextId { get; }

    [Header("대사 선택 화면")]
    /// <summary>대사 선택 UI 루트 오브젝트</summary>
    protected abstract GameObject SelectDialogueRoot { get; }
    /// <summary>대사 선택지 배열 (3개)</summary>
    protected abstract DialogueItem[] DialogueChoices { get; }

    [Header("마이크 STT")]
    /// <summary>마이크 녹음 인디케이터 (STT 처리 및 키워드 매칭)</summary>
    protected abstract MicRecordingIndicator MicIndicator { get; }
    /// <summary>마이크 버튼 루트 (대사 선택 전에는 숨김)</summary>
    protected abstract GameObject MicButtonRoot { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer; // 대사 시퀀서 (진입/완료/재시도 대사)

    #endregion

    // ===== 내부 상태 =====
    private int _selectedIndex = -1;                // 선택된 대사의 배열 인덱스 (-1이면 미선택)
    private DialogueItem _selectedDialogue;         // 선택된 대사 항목 참조
    private bool _isRecording;                      // 현재 녹음 중인지 여부
    private bool _isFinished;                       // STT 매칭 성공하여 완료된 상태인지
    private bool _interactionLocked = true;         // 대화 재생 중 상호작용 잠금 플래그

    // =========================
    // ProblemStepBase 생명주기 구현
    // =========================

    /// <summary>
    /// 스텝 진입 시 호출. 상태 초기화, 선택 이미지 리셋, 라벨 적용, 리스너 등록.
    /// 대사 선택 화면 표시, 마이크 버튼 숨김. 대화 재생 완료 대기.
    /// </summary>
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

        _interactionLocked = true;
        if (dialogueSequencer != null)
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        else
            _interactionLocked = false;
    }

    /// <summary>대화 진입 완료 시 상호작용 잠금을 해제한다.</summary>
    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;
    }

    /// <summary>스텝 퇴장 시 호출. 이벤트 해제, 녹음 상태 초기화, 리스너 정리.</summary>
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

    /// <summary>모든 대사의 selectImg를 비활성화한다.</summary>
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

    /// <summary>각 대사 버튼의 하위 Text 컴포넌트에 CSV 텍스트를 적용한다.</summary>
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

    /// <summary>
    /// 대사 선택 버튼 리스너 및 MicRecordingIndicator의 STT 이벤트 리스너를 등록한다.
    /// STT 키워드로 모든 대사 텍스트를 설정한다.
    /// </summary>
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

    /// <summary>대사 버튼 리스너와 STT 이벤트 리스너를 모두 제거한다.</summary>
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

    /// <summary>
    /// 대사 버튼 클릭 시 호출. 선택된 대사의 selectImg를 활성화하고 마이크 버튼을 표시한다.
    /// </summary>
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

    /// <summary>
    /// 마이크 버튼 클릭 시 호출 (인스펙터 OnClick에서 연결).
    /// 녹음 시작/중지를 토글하고, 녹음 중에는 대사 호버 효과를 비활성화한다.
    /// </summary>
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

    /// <summary>대사 선택 버튼들의 ButtonHover 컴포넌트를 활성화/비활성화한다.</summary>
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

    /// <summary>
    /// STT 키워드 매칭 성공 시 호출. 매칭된 인덱스가 선택한 대사와 같으면 성공,
    /// 다르면 재시도 안내를 표시한다.
    /// </summary>
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

            if (MicButtonRoot != null) MicButtonRoot.SetActive(false);

            if (dialogueSequencer != null)
                dialogueSequencer.ShowCompletedText();
        }
        else
        {
            ShowRetryGuide();
        }
    }

    /// <summary>STT에서 키워드와 매칭되지 않았을 때 호출. 재시도 안내를 표시한다.</summary>
    private void OnSTTNoMatch(string sttResult)
    {
        if (_isFinished) return;

        _isRecording = false;
        ShowRetryGuide();
    }

    /// <summary>
    /// STT 매칭 실패 시 재시도 안내를 표시한다.
    /// 선택된 selectImg를 비활성화하고, 재시도 텍스트+TTS를 재생하며, 마이크 상태 텍스트를 변경한다.
    /// </summary>
private void ShowRetryGuide()
    {
        // 녹음 종료 → hover 재활성화
        SetChoicesHoverEnabled(true);

        // 선택된 selectImg 비활성화
        if (_selectedDialogue?.selectImg != null)
            _selectedDialogue.selectImg.SetActive(false);

        if (RetryTextId > 0)
        {
            if (dialogueSequencer != null)
                dialogueSequencer.SetText(RetryTextId);

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayTTS(RetryTextId);
        }

        var mic = MicIndicator;
        if (mic != null)
            mic.SetIdleText("다시 말하기");
    }

    // =========================
    // DB 저장
    // =========================

    /// <summary>선택된 대사의 ID와 텍스트를 SaveAttempt를 통해 DB에 저장한다.</summary>
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
