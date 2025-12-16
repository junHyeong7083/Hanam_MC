using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Part5 / Step3 대사 연습 로직 베이스.
/// - 회피형 / 건강한 / 도전적 대사를 중에서 선택
/// - 정답(건강한) 선택 시: NPC 긍정 반응 + 게이트 완료
/// - 오답 선택 시: 피드백이 잠깐 떴다가 사라지고, 다시 시도 가능
/// - 사용자가 어떤 선택지를 어떤 순서로 눌렀는지 전부 DB에 저장.
/// </summary>
public enum DialogueOptionType
{
    Avoidant,
    Healthy,
    Confrontational
}

public interface IDialogueOptionData
{
    int Id { get; }
    string Text { get; }
    DialogueOptionType Type { get; }
    string Feedback { get; }

    // 정답 여부 (인스펙터에서 체크)
    bool IsCorrect { get; }

    Button Button { get; }
    Text Label { get; }
}

public abstract class Director_Problem5_Step3_Logic : ProblemStepBase
{
    [Serializable]
    private class ClickLogEntry
    {
        public int id;
        public string text;
        public string type;       // avoidant / healthy / confrontational
        public string inputMode;  // button / voice
        public float time;        // 스텝 시작 기준 경과 시간 (초)
    }

    [Serializable]
    private class DialogueAttemptBody
    {
        public int selectedId;
        public string selectedText;
        public string selectedType;
        public string inputMode;      // button / voice
        public bool npcResponded;
        public string feedbackText;
        public ClickLogEntry[] clickLogs;
    }

    // ===== 자식에서 주입할 추상 프로퍼티 =====

    [Header("선택지 (자식 주입)")]
    protected abstract IDialogueOptionData[] Options { get; }

    [Header("NPC 응답 UI")]
    protected abstract GameObject NpcResponseRoot { get; }

    [Header("선택지 피드백 UI")]
    protected abstract GameObject FeedbackRoot { get; }
    protected abstract Text FeedbackLabel { get; }

    [Header("색상 설정")]
    protected abstract Color OptionNormalColor { get; }
    protected abstract Color OptionHealthyColor { get; }
    protected abstract Color OptionWrongColor { get; }

    [Header("마이크 STT")]
    protected abstract MicRecordingIndicator MicIndicator { get; }

    [Header("타이밍 설정")]
    protected abstract float OptionSelectDelay { get; }          // 1.5f
    protected abstract float NpcResponseDelay { get; }           // 1.0f

    [Header("완료 게이트 (StepCompletionGate)")]
    protected abstract StepCompletionGate CompletionGate { get; }

    [Header("오답 피드백 연출")]
    protected abstract float WrongFeedbackShowDuration { get; }        // 예: 1.0
    protected abstract GameObject FeedbackNextButtonRoot { get; }      // FeedbackRoot 안 "다음" 버튼 루트

    [Header("정답 이미지 연출")]
    protected abstract GameObject OriginalAnswerImage { get; }         // 기존 이미지 (정답 시 숨김)
    protected abstract PopupImageDisplay CorrectAnswerPopup { get; }   // 정답 이미지 (PopupImageDisplay)

    // ===== 내부 상태 =====

    private int _selectedIndex = -1;
    private bool _hasAnswered;     // 정답 맞춘 뒤엔 true → 더 이상 입력 X
    private bool _npcResponded;
    private string _inputMode = "button"; // "button" or "voice"

    private Coroutine _optionRoutine;
    private Coroutine _npcRoutine;

    // 클릭 히스토리용
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
        //_isRecording = false;
        _npcResponded = false;
        _inputMode = "button";

        // 히스토리 초기화
        _clickLogList.Clear();
        _stepStartTime = Time.time;

        // NPC 응답 초기 상태
        if (NpcResponseRoot != null) NpcResponseRoot.SetActive(false);

        // 피드백 영역 비활성화
        if (FeedbackRoot != null)
            FeedbackRoot.SetActive(false);

        // 피드백 안의 "다음 버튼"은 처음엔 숨김
        if (FeedbackNextButtonRoot != null)
            FeedbackNextButtonRoot.SetActive(false);

        // 정답 이미지 초기화
        if (OriginalAnswerImage != null)
            OriginalAnswerImage.SetActive(true);
        if (CorrectAnswerPopup != null)
            CorrectAnswerPopup.ResetToInitial();

        // 버튼 리스너, 색상 초기화
        for (int i = 0; i < options.Length; i++)
        {
            int idx = i;
            var opt = options[i];

            if (opt.Button != null)
            {
                opt.Button.onClick.RemoveAllListeners();
                opt.Button.onClick.AddListener(() => OnClickOption(idx));
                opt.Button.interactable = true;
            }

            if (opt.Label != null)
                opt.Label.text = opt.Text;

            SetOptionVisual(idx, false, false);
        }

        // MicIndicator STT 설정
        var mic = MicIndicator;
        if (mic != null)
        {
            // 옵션 텍스트를 키워드로 설정
            var keywordList = new List<string>();
            foreach (var opt in options)
            {
                keywordList.Add(opt.Text);
            }
            mic.SetKeywords(keywordList.ToArray());

            // 이벤트 구독
            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnKeywordMatched += OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;
            mic.OnNoMatch += OnSTTNoMatch;
        }

        // 게이트 리셋: 이 스텝은 "1번 완료"만 채우면 끝
        if (CompletionGate != null)
            CompletionGate.ResetGate(1);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (_optionRoutine != null) StopCoroutine(_optionRoutine);
        if (_npcRoutine != null) StopCoroutine(_npcRoutine);

        _optionRoutine = null;
        _npcRoutine = null;

        // MicIndicator 이벤트 구독 해제
        var mic = MicIndicator;
        if (mic != null)
        {
            mic.OnKeywordMatched -= OnSTTKeywordMatched;
            mic.OnNoMatch -= OnSTTNoMatch;
        }
    }

    // ===== 선택지 시각 업데이트 =====

    private void SetOptionVisual(int index, bool isSelected, bool isCorrect)
    {
        var options = Options;
        if (options == null || index < 0 || index >= options.Length) return;

        var opt = options[index];
        var button = opt.Button;
        if (button == null) return;

        var colors = button.colors;
        if (!isSelected)
        {
            colors.normalColor = OptionNormalColor;
            colors.highlightedColor = OptionNormalColor;
            colors.pressedColor = OptionNormalColor;
        }
        else
        {
            var color = isCorrect ? OptionHealthyColor : OptionWrongColor;
            colors.normalColor = color;
            colors.highlightedColor = color;
            colors.pressedColor = color;
        }

        button.colors = colors;
    }

    private void ResetOptionVisual(int index)
    {
        var options = Options;
        if (options == null || index < 0 || index >= options.Length) return;
        SetOptionVisual(index, false, false);
    }

    private void SetAllOptionsInteractable(bool interactable)
    {
        var options = Options;
        if (options == null) return;

        foreach (var opt in options)
        {
            if (opt.Button != null)
                opt.Button.interactable = interactable;
        }
    }

    // ===== 클릭 로그 =====

    private void LogClick(int index)
    {
        var options = Options;
        if (options == null || index < 0 || index >= options.Length) return;

        var opt = options[index];

        var entry = new ClickLogEntry
        {
            id = opt.Id,
            text = opt.Text,
            type = ToTypeString(opt.Type),
            inputMode = _inputMode,
            time = Time.time - _stepStartTime
        };

        _clickLogList.Add(entry);
    }

    // ===== 버튼 선택 흐름 =====

    public void OnClickOption(int index)
    {
        if (_hasAnswered) return;   // 이미 정답 맞췄으면 끝

        var options = Options;
        if (options == null || index < 0 || index >= options.Length) return;

        _selectedIndex = index;

        // 누를 때마다 히스토리 기록 (오답 포함 전체)
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

        SetAllOptionsInteractable(false);


        var opt = options[index];
        bool isCorrect = opt.IsCorrect;

        // 선택된 옵션 색상만 먼저 반영
        SetOptionVisual(index, true, isCorrect);

        // ✅ 1) 버튼 누르자마자 피드백 바로 켜기
        if (FeedbackRoot != null) FeedbackRoot.SetActive(true);
        if (FeedbackLabel != null) FeedbackLabel.text = opt.Feedback;

        if (isCorrect)
        {
            // ✅ 정답: 피드백 + NPC 반응 동시에
            if (FeedbackNextButtonRoot != null)
                FeedbackNextButtonRoot.SetActive(true);   // 요약 버튼 보이게

            _hasAnswered = true;
            _npcResponded = true;

            // 정답 이미지 연출: 기존 이미지 숨기고 정답 이미지 팝업
            if (OriginalAnswerImage != null)
                OriginalAnswerImage.SetActive(false);
            if (CorrectAnswerPopup != null)
                CorrectAnswerPopup.Show();

            // NPC 응답 표시
            if (NpcResponseRoot != null) NpcResponseRoot.SetActive(true);

            // 게이트 완료 (요약 버튼 있는 쪽 StepCompletionGate completeRoot가 켜짐)
            if (CompletionGate != null)
                CompletionGate.MarkOneDone();

            _optionRoutine = null;
            yield break;
        }
        else
        {
            // ❌ 오답: 요약 버튼은 숨김
            if (FeedbackNextButtonRoot != null)
                FeedbackNextButtonRoot.SetActive(false);

            // 지정한 시간 동안 피드백 유지
            float wrongDur = Mathf.Max(0f, WrongFeedbackShowDuration);
            if (wrongDur > 0f)
                yield return new WaitForSeconds(wrongDur);

            // 피드백 닫기
            if (FeedbackRoot != null)
                FeedbackRoot.SetActive(false);

            // 색상 원복 + 다시 선택 가능하게
            ResetOptionVisual(index);
            SetAllOptionsInteractable(true);

            _optionRoutine = null;
            yield break;
        }
    }


    // ===== STT 이벤트 핸들러 =====

    /// <summary>
    /// STT 키워드 매칭 성공 시 호출
    /// matchedIndex = 옵션 배열의 인덱스
    /// </summary>
    protected void OnSTTKeywordMatched(int matchedIndex)
    {
        Debug.Log($"[Problem5_Step3] STT 매칭: index={matchedIndex}");

        if (_hasAnswered) return;

        // voice 모드로 설정하고 해당 옵션 선택
        _inputMode = "voice";
        OnClickOption(matchedIndex);
    }

    /// <summary>
    /// STT 매칭 실패 시 호출
    /// </summary>
    protected void OnSTTNoMatch(string sttResult)
    {
        Debug.Log($"[Problem5_Step3] STT 매칭 실패: {sttResult}");
        // 매칭 실패 시에는 아무것도 하지 않음 - 사용자가 다시 녹음하거나 버튼 클릭 가능
    }

    // ===== NPC 반응 흐름 =====

    private IEnumerator NpcResponseFlow()
    {
        float delay = Mathf.Max(0f, NpcResponseDelay);
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        _npcResponded = true;

        if (NpcResponseRoot != null) NpcResponseRoot.SetActive(true);

        OnNpcResponseShown();

        _npcRoutine = null;
    }

    private void OnNpcResponseShown()
    {
        if (CompletionGate != null)
        {
            CompletionGate.MarkOneDone();
        }
        else
        {
            Debug.LogWarning("[Problem5_Step3] CompletionGate가 설정되어 있지 않습니다. 요약 버튼이 안 켜질 수 있습니다.");
        }
    }

    // ===== 완료 처리 / DB 저장 =====

    public void OnClickContinue()
    {
        var options = Options;
        if (options == null || _selectedIndex < 0 || _selectedIndex >= options.Length)
        {
            Debug.LogWarning("[Problem5_Step3] 선택된 옵션이 없어도 Continue가 눌렸습니다.");
            return;
        }

        var opt = options[_selectedIndex];

        var body = new DialogueAttemptBody
        {
            selectedId = opt.Id,
            selectedText = opt.Text,
            selectedType = ToTypeString(opt.Type),
            inputMode = _inputMode,
            npcResponded = _npcResponded,
            feedbackText = opt.Feedback,
            clickLogs = _clickLogList.ToArray()
        };

        SaveAttempt(body);
        // StepCompletionGate.completeRoot 안 "요약 보기" 버튼에서
        // 이 메서드 + StepFlowController.NextStep()을 같이 호출하면 됨.
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
