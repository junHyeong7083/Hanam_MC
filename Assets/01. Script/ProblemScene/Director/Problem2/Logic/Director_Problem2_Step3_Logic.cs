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

    // ===== Guide Text =====
    protected abstract Text GuideText { get; }
    protected abstract int GuideTextId_Before { get; }
    protected abstract int GuideTextId_After { get; }
    protected abstract int GuideTextId_Retry { get; }

    // ===== UI =====
    protected abstract RectTransform SceneCardRect { get; }
    protected abstract GameObject OkSceneCard { get; }

    // 캐러셀
    protected abstract GameObject CarouselRoot { get; }
    protected abstract Button PrevButton { get; }
    protected abstract Button NextButton { get; }

    // 마이크
    protected abstract GameObject MicButtonRoot { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }

    // ✅ 추가: 녹음 중 표시(이미지) 오브젝트
    protected abstract GameObject RecordingOverlay { get; }

    // ✅ 추가: STT 완료 후 보여줄 다음 버튼(요약/다음 단계 버튼) 루트
    protected abstract GameObject NextStepButtonRoot { get; }

    // 패널
    protected abstract GameObject StepRoot { get; }

    protected abstract StepCompletionGate CompletionGate { get; }

    private int _currentIndex;
    private IDirectorProblem2PerspectiveOption _selected;
    private bool _isRecording;
    private bool _hasRecordedAnswer;
    private bool _isFinished;

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
        }

        var gate = CompletionGate;
        if (gate != null) gate.ResetGate(1);

        BindCarouselButtons();
        BindMicButton();
    }

    protected override void OnStepExit()
    {
        var indicator = MicIndicator;
        if (indicator != null)
        {
            indicator.OnKeywordMatched -= OnSTTKeywordMatched;
            indicator.OnNoMatch -= OnSTTNoMatch;
        }

        UnbindCarouselButtons();
        UnbindMicButton();
    }

    private void ResetState()
    {
        _currentIndex = 0;
        _selected = null;
        _isRecording = false;
        _hasRecordedAnswer = false;
        _isFinished = false;

        if (StepRoot != null) StepRoot.SetActive(true);

        if (GuideText != null && GuideTextId_Before != 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Before);

        if (SceneCardRect != null) SceneCardRect.gameObject.SetActive(true);
        if (OkSceneCard != null) OkSceneCard.SetActive(false);

        if (CarouselRoot != null) CarouselRoot.SetActive(true);

        ClampIndex();
        RefreshCarouselUI();

        // ✅ 완료 전 UI 상태로 세팅
        SetBeforeCompleteUI();

        // 마이크 아이들 텍스트 원복
        var indicator2 = MicIndicator;
        if (indicator2 != null)
            indicator2.ResetIdleText();
    }

    private void SetBeforeCompleteUI()
    {
        // STT 완료 전: 마이크 보임, Next 숨김, 녹음 표시 꺼짐
        if (MicButtonRoot != null) MicButtonRoot.SetActive(true);
        if (NextStepButtonRoot != null) NextStepButtonRoot.SetActive(false);
        if (RecordingOverlay != null) RecordingOverlay.SetActive(false);

        SetMicInteractable(true);
    }

    private void SetAfterCompleteUI()
    {
        // STT 완료 후: 마이크 숨김, Next 보임, 녹음 표시 꺼짐
        if (RecordingOverlay != null) RecordingOverlay.SetActive(false);

        if (MicButtonRoot != null) MicButtonRoot.SetActive(false);
        if (NextStepButtonRoot != null) NextStepButtonRoot.SetActive(true);

        SetMicInteractable(false);
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

    private void ClampIndex()
    {
        var p = Perspectives;
        if (p == null || p.Length == 0)
        {
            _currentIndex = 0;
            return;
        }

        if (_currentIndex < 0) _currentIndex = 0;
        if (_currentIndex >= p.Length) _currentIndex = p.Length - 1;
    }

    private void RefreshCarouselUI()
    {
        var p = Perspectives;
        if (p == null || p.Length == 0)
        {
            _selected = null;

            if (PrevButton != null) PrevButton.interactable = false;
            if (NextButton != null) NextButton.interactable = false;

            SetMicInteractable(false);
            return;
        }

        ClampIndex();
        _selected = p[_currentIndex];

        bool canNavigate = !_isFinished && p.Length > 1;
        if (PrevButton != null) PrevButton.interactable = canNavigate;
        if (NextButton != null) NextButton.interactable = canNavigate;
    }

    private void OnClickPrev()
    {
        if (_isFinished) return;

        var p = Perspectives;
        if (p == null || p.Length == 0) return;

        _currentIndex--;
        if (_currentIndex < 0) _currentIndex = p.Length - 1;

        RefreshCarouselUI();
    }

    private void OnClickNext()
    {
        if (_isFinished) return;

        var p = Perspectives;
        if (p == null || p.Length == 0) return;

        _currentIndex++;
        if (_currentIndex >= p.Length) _currentIndex = 0;

        RefreshCarouselUI();
    }

    // ===== Mic =====
    public void OnClickMic()
    {
        Debug.Log($"[Step3] OnClickMic / indicator={(MicIndicator != null)} / finished={_isFinished}");
        if (_isFinished) return;

        var p = Perspectives;
        if (p == null || p.Length == 0) return;

        ClampIndex();
        _selected = p[_currentIndex];

        _isRecording = !_isRecording;

        // ✅ 녹음 중 이미지 토글
        if (RecordingOverlay != null)
            RecordingOverlay.SetActive(_isRecording);

        var indicator = MicIndicator;
        if (indicator != null)
            indicator.ToggleRecording();
    }

    // ===== STT =====
    private void OnSTTKeywordMatched(int matchedIndex)
    {
        if (_isFinished) return;

        var p = Perspectives;
        if (p == null || p.Length == 0) return;

        ClampIndex();

        if (matchedIndex == _currentIndex)
        {
            _hasRecordedAnswer = true;
            _isRecording = false;

            // ✅ 녹음 표시 끄기
            if (RecordingOverlay != null)
                RecordingOverlay.SetActive(false);

            PlayRefilmComplete();
        }
        else
        {
            // 다른 관점의 키워드가 매칭됨 → 재시도 처리
            _isRecording = false;
            if (RecordingOverlay != null)
                RecordingOverlay.SetActive(false);

            if (GuideText != null)
            {
                var retryTextId = GuideTextId_Retry;
                GuideText.text = retryTextId != 0
                    ? ProblemRuntime.L(retryTextId)
                    : "조금 더 가까이서 힘차게 말해주세요!";
            }

            var ind = MicIndicator;
            if (ind != null)
                ind.SetIdleText("다시 말하기");
        }
    }

    private void OnSTTNoMatch(string sttResult)
    {
        _isRecording = false;
        if (RecordingOverlay != null)
            RecordingOverlay.SetActive(false);

        // 재시도 안내 텍스트 표시
        if (GuideText != null)
        {
            var retryTextId = GuideTextId_Retry;
            GuideText.text = retryTextId != 0
                ? ProblemRuntime.L(retryTextId)
                : "조금 더 가까이서 힘차게 말해주세요!";
        }

        // 마이크 버튼 텍스트를 "다시 말하기"로 변경
        var indicator = MicIndicator;
        if (indicator != null)
            indicator.SetIdleText("다시 말하기");
    }

    // ===== Complete Sequence =====
    private void PlayRefilmComplete()
    {
        if (GuideText != null && GuideTextId_After != 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_After);

        if (PrevButton != null) PrevButton.interactable = false;
        if (NextButton != null) NextButton.interactable = false;

        if (StepRoot != null) StepRoot.SetActive(false);
        if (OkSceneCard != null) OkSceneCard.SetActive(true);

        _isFinished = true;

        // 완료 UI 전환: 마이크 숨기고 Next 보이기
        SetAfterCompleteUI();

        var gate = CompletionGate;
        if (gate != null)
            gate.MarkOneDone();
    }

    public void OnClickSummaryButton()
    {
        SaveRefilmLogToDb();
    }

    private void SaveRefilmLogToDb()
    {
        var p = Perspectives;
        if (p == null || p.Length == 0) return;

        ClampIndex();
        _selected = p[_currentIndex];
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