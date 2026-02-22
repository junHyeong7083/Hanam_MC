using System;
using System.Collections;
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

    // ===== UI =====
    protected abstract bool OverwriteSceneTextOnReset { get; }

    protected abstract Text SceneText { get; }
    protected abstract RectTransform SceneCardRect { get; }
    protected abstract GameObject OkSceneCard { get; }
    protected abstract Text OkSceneText { get; }

    protected abstract CardFlip CardFlip { get; }

    // 캐러셀
    protected abstract GameObject CarouselRoot { get; }
    protected abstract Button PrevButton { get; }
    protected abstract Button NextButton { get; }
    protected abstract Text CarouselText { get; }
    protected abstract Text CarouselIndexText { get; } // optional

    // 마이크
    protected abstract GameObject MicButtonRoot { get; }
    protected abstract MicRecordingIndicator MicIndicator { get; }

    // ✅ 추가: 녹음 중 표시(이미지) 오브젝트
    protected abstract GameObject RecordingOverlay { get; }

    // ✅ 추가: STT 완료 후 보여줄 다음 버튼(요약/다음 단계 버튼) 루트
    protected abstract GameObject NextStepButtonRoot { get; }

    // 패널
    protected abstract GameObject StepRoot { get; }
    protected abstract GameObject SummaryPanelRoot { get; }

    protected abstract StepCompletionGate CompletionGate { get; }
    protected abstract float FlipDelay { get; }

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
    }

    private void ResetState()
    {
        _currentIndex = 0;
        _selected = null;
        _isRecording = false;
        _hasRecordedAnswer = false;
        _isFinished = false;

        if (StepRoot != null) StepRoot.SetActive(true);
        if (SummaryPanelRoot != null) SummaryPanelRoot.SetActive(false);

        if (GuideText != null && GuideTextId_Before != 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_Before);

        if (SceneText != null && OverwriteSceneTextOnReset)
            SceneText.text = NgSentence;

        if (SceneCardRect != null) SceneCardRect.gameObject.SetActive(true);
        if (OkSceneCard != null) OkSceneCard.SetActive(false);

        if (CarouselRoot != null) CarouselRoot.SetActive(true);

        ClampIndex();
        RefreshCarouselUI();

        // ✅ 완료 전 UI 상태로 세팅
        SetBeforeCompleteUI();
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

            if (CarouselText != null) CarouselText.text = "";
            if (CarouselIndexText != null) CarouselIndexText.text = "";

            if (PrevButton != null) PrevButton.interactable = false;
            if (NextButton != null) NextButton.interactable = false;

            SetMicInteractable(false);
            return;
        }

        ClampIndex();
        _selected = p[_currentIndex];

        if (CarouselText != null && _selected != null)
            CarouselText.text = _selected.Text;

        if (CarouselIndexText != null)
            CarouselIndexText.text = $"{_currentIndex + 1}/{p.Length}";

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

            StartCoroutine(PlayRefilmCompleteSequence());
        }
        else
        {
            _isRecording = false;
            if (RecordingOverlay != null)
                RecordingOverlay.SetActive(false);
        }
    }

    private void OnSTTNoMatch(string sttResult)
    {
        _isRecording = false;
        if (RecordingOverlay != null)
            RecordingOverlay.SetActive(false);
    }

    // ===== Complete Sequence =====
    private IEnumerator PlayRefilmCompleteSequence()
    {
        if (GuideText != null && GuideTextId_After != 0)
            GuideText.text = ProblemRuntime.L(GuideTextId_After);

        if (PrevButton != null) PrevButton.interactable = false;
        if (NextButton != null) NextButton.interactable = false;

        float delay = FlipDelay;
        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        var cardFlip = CardFlip;
        if (cardFlip != null)
            yield return StartCoroutine(cardFlip.PlayFlipRoutine());

        if (OkSceneText != null && _selected != null)
            OkSceneText.text = _selected.Text;

        if (SceneCardRect != null) SceneCardRect.gameObject.SetActive(false);
        if (OkSceneCard != null) OkSceneCard.SetActive(true);

        _isFinished = true;

        // ✅ 완료 UI 전환: 마이크 숨기고 Next 보이기
        SetAfterCompleteUI();

        var gate = CompletionGate;
        if (gate != null)
            gate.MarkOneDone();
    }

    public void OnClickSummaryButton()
    {
        SaveRefilmLogToDb();

        if (StepRoot != null) StepRoot.SetActive(false);
        if (SummaryPanelRoot != null) SummaryPanelRoot.SetActive(true);
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

    private void SetMicInteractable(bool interactable)
    {
        if (MicButtonRoot == null) return;

        var btn = MicButtonRoot.GetComponentInChildren<Button>(true);
        if (btn != null) btn.interactable = interactable;
    }
}