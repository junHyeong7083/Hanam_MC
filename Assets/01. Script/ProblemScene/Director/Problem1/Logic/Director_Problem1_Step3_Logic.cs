using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public abstract class Director_Problem1_Step3_Logic : RandomCardSequenceStepBase
{
    [Serializable]
    protected class SortLogEntry
    {
        public int filmId;
        public string text;
        public string correctType;
        public string chosenType;
    }

    [Serializable]
    private class SortLogPayload
    {
        public SortLogEntry[] items;
    }

    protected abstract int FilmCount { get; }

    protected abstract int GetFilmId(int index);
    protected abstract string GetFilmText(int index);
    protected abstract Sprite GetFilmSprite(int index);
    protected abstract bool IsFilmThought(int index);
    protected abstract int GetFilmHanamiTextId(int index);

    protected abstract RectTransform CurrentFilmRoot { get; }
    protected abstract GameObject CurrentFilmPrefab { get; }

    protected abstract Transform[] ThoughtSlots { get; }
    protected abstract Transform[] FactSlots { get; }

    protected abstract GameObject AnswerButtonsRoot { get; }
    protected abstract GameObject SummaryButtonRoot { get; }
    protected abstract float SortAdvanceDelay { get; }

    protected abstract MicRecordingIndicator MicIndicator { get; }

    [Header("Dialogue")]
    [SerializeField] private DialogueSequencer dialogueSequencer;

    [Header("피드백 TextId")]
    [SerializeField] private int correctThoughtTextId = 101010019;
    [SerializeField] private int correctFactTextId = 101010018;
    [SerializeField] private int wrongFeedbackTextId;
    [SerializeField] private float wrongFeedbackDuration = 2f;

    protected abstract GameObject StepRoot { get; }
    protected abstract GameObject SummaryPanelRoot { get; }

    private bool _interactionLocked = true;
    private bool _deferFirstCard;
    private GameObject _currentFilmInstance;
    private Director_Problem1_Step3_FilmCardAnimator _currentFilmAnimator;

    private bool _isAdvancing;

    private readonly HashSet<int> _sortedFilmIds = new HashSet<int>();
    private readonly List<SortLogEntry> _logs = new List<SortLogEntry>();

    private readonly List<GameObject> _placedThoughtFilmInstances = new List<GameObject>();
    private readonly List<GameObject> _placedFactFilmInstances = new List<GameObject>();

    private Coroutine _hanamiTempMessageCoroutine;
    private bool _isShowingHanamiWrongMessage;

    // 이동 연출 중인 트윈 정리용
    private readonly List<Tween> _activeTweens = new List<Tween>();

    // 연출 파라미터 (필요하면 자식 클래스에서 abstract로 뺄 수도 있음)
    private const float MoveAnimDuration = 0.28f;
    private const float MovePunchScale = 0.92f;

    protected override int CardCount => FilmCount;

    protected override void OnSequenceReset()
    {
        _isAdvancing = false;
        _sortedFilmIds.Clear();
        _logs.Clear();

        StopWrongFeedbackCoroutineIfRunning();
        _isShowingHanamiWrongMessage = false;

        KillAllTweens();

        if (MicIndicator != null)
        {
            MicIndicator.OnKeywordMatched -= OnSTTKeywordMatched;
            MicIndicator.OnKeywordMatched += OnSTTKeywordMatched;
            MicIndicator.SetRecording(false);
        }

        ClearPlacedFilmInstances();

        if (SummaryPanelRoot != null)
            SummaryPanelRoot.SetActive(false);

        if (SummaryButtonRoot != null)
            SummaryButtonRoot.SetActive(false);

        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(true);

        DestroyCurrentFilmCard();

        _interactionLocked = true;
        if (dialogueSequencer != null)
        {
            // enter 시퀀스의 쪽수에 film 수 반영
            dialogueSequencer.SetExtraPageCount(FilmCount);

            // 첫 대사가 표시될 때까지 카드 생성을 지연
            _deferFirstCard = true;
            dialogueSequencer.OnFirstTextShown += OnDialogueFirstTextShown;
            dialogueSequencer.OnEnterComplete += OnDialogueEnterComplete;
        }
        else
        {
            _deferFirstCard = false;
            _interactionLocked = false;
        }
    }

    private void OnDialogueFirstTextShown()
    {
        _deferFirstCard = false;
        UpdateCurrentCardUI();
    }

    private void OnDialogueEnterComplete()
    {
        _interactionLocked = false;

        // enter 시퀀스 완료 후 현재 카드의 하남이 대사 표시
        if (dialogueSequencer != null)
        {
            int logicalIndex = GetCurrentLogicalIndex();
            if (logicalIndex >= 0)
            {
                int hanamiTextId = GetFilmHanamiTextId(logicalIndex);
                if (hanamiTextId > 0)
                    SetTextWithFilmPage(hanamiTextId, logicalIndex);
            }
        }
    }

    private int TotalPages => dialogueSequencer != null
        ? dialogueSequencer.EnterTextCount + FilmCount
        : FilmCount;

    private void SetTextWithFilmPage(int textId, int filmIndex)
    {
        int enterCount = (dialogueSequencer != null) ? dialogueSequencer.EnterTextCount : 0;
        int currentPage = enterCount + filmIndex + 1;
        dialogueSequencer.SetText(textId, currentPage, TotalPages);
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (dialogueSequencer != null)
        {
            dialogueSequencer.OnFirstTextShown -= OnDialogueFirstTextShown;
            dialogueSequencer.OnEnterComplete -= OnDialogueEnterComplete;
        }

        _interactionLocked = true;

        if (MicIndicator != null)
            MicIndicator.OnKeywordMatched -= OnSTTKeywordMatched;
    }

    private void OnSTTKeywordMatched(int index)
    {
        // 0=생각, 1=사실
        bool userChoseThought = (index == 0);
        HandleSort(userChoseThought);
    }

    protected override void OnApplyCardToUI(int logicalIndex)
    {
        if (logicalIndex < 0 || logicalIndex >= FilmCount)
        {
            DestroyCurrentFilmCard();

            if (AnswerButtonsRoot != null)
                AnswerButtonsRoot.SetActive(false);

            return;
        }

        // 첫 대사 표시 전이면 카드 생성 스킵 (OnFirstTextShown에서 호출됨)
        if (_deferFirstCard)
            return;

        string text = GetFilmText(logicalIndex);
        Sprite sprite = GetFilmSprite(logicalIndex);

        SpawnOrUpdateCurrentFilmCard(text, sprite);

        // 카드별 하남이 대사 (enter 시퀀스 완료 후에만)
        if (!_interactionLocked && !_isShowingHanamiWrongMessage && dialogueSequencer != null)
        {
            int hanamiTextId = GetFilmHanamiTextId(logicalIndex);
            if (hanamiTextId > 0)
                SetTextWithFilmPage(hanamiTextId, logicalIndex);
        }

        if (_currentFilmAnimator != null)
            _currentFilmAnimator.PlayEnter();

        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(true);
    }

    protected override void OnClearCurrentCardUI()
    {
        // 현재 카드는 분류 시 슬롯으로 이동시키므로 여기서 삭제하지 않음
        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(false);
    }

    protected override void OnCardProcessed(int logicalIndex)
    {
    }

    protected override void OnAllCardsProcessed()
    {
        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(false);

        bool allSorted = (_logs.Count >= FilmCount);

        if (SummaryButtonRoot != null)
            SummaryButtonRoot.SetActive(allSorted);

        if (allSorted)
        {
            if (dialogueSequencer != null)
                dialogueSequencer.ShowCompletedText();

            Debug.Log("[Director_Problem1_Step3_Logic] 모든 필름 분류 완료.");
        }
        else
        {
            Debug.LogWarning("[Director_Problem1_Step3_Logic] OnAllCardsProcessed 호출됨, 하지만 분류 로그가 부족함. logs="
                             + _logs.Count + ", films=" + FilmCount);
        }
    }

    private void SpawnOrUpdateCurrentFilmCard(string text, Sprite sprite)
    {
        if (CurrentFilmRoot == null || CurrentFilmPrefab == null)
            return;

        if (_currentFilmInstance == null)
        {
            // text 자식 찾아서 idx 기반 넣어주면됨
            _currentFilmInstance = UnityEngine.Object.Instantiate(CurrentFilmPrefab, CurrentFilmRoot, false);
            _currentFilmAnimator = _currentFilmInstance.GetComponent<Director_Problem1_Step3_FilmCardAnimator>();
        }
        else
        {
            if (_currentFilmInstance.transform.parent != CurrentFilmRoot)
            {
                _currentFilmInstance.transform.SetParent(CurrentFilmRoot, false);
                ResetToLocalIdentity(_currentFilmInstance.transform);
            }
        }

        // GetComponentInChildren은 root GO도 포함하므로, 자식 전용으로 탐색
        Text uiText = null;
        foreach (var t in _currentFilmInstance.GetComponentsInChildren<Text>(true))
        {
            if (t.gameObject != _currentFilmInstance) { uiText = t; break; }
        }
        if (uiText != null)
            uiText.text = text ?? string.Empty;

        // Image는 root에 있으므로 root에서 직접 가져옴
        Image uiImage = _currentFilmInstance.GetComponent<Image>();
        if (uiImage != null && sprite != null)
            uiImage.sprite = sprite;
    }

    private void DestroyCurrentFilmCard()
    {
        if (_currentFilmInstance != null)
        {
            UnityEngine.Object.Destroy(_currentFilmInstance);
            _currentFilmInstance = null;
            _currentFilmAnimator = null;
        }
    }

    private void ClearPlacedFilmInstances()
    {
        for (int i = 0; i < _placedThoughtFilmInstances.Count; i++)
        {
            if (_placedThoughtFilmInstances[i] != null)
                UnityEngine.Object.Destroy(_placedThoughtFilmInstances[i]);
        }
        _placedThoughtFilmInstances.Clear();

        for (int i = 0; i < _placedFactFilmInstances.Count; i++)
        {
            if (_placedFactFilmInstances[i] != null)
                UnityEngine.Object.Destroy(_placedFactFilmInstances[i]);
        }
        _placedFactFilmInstances.Clear();
    }

    private void ShowWrongFeedbackTemp(int logicalIndex)
    {
        if (dialogueSequencer == null || wrongFeedbackTextId <= 0) return;

        StopWrongFeedbackCoroutineIfRunning();
        _hanamiTempMessageCoroutine = StartCoroutine(CoWrongFeedbackTemp(logicalIndex));
    }

    private IEnumerator CoWrongFeedbackTemp(int logicalIndex)
    {
        _isShowingHanamiWrongMessage = true;

        SetTextWithFilmPage(wrongFeedbackTextId, logicalIndex);

        float wait = Mathf.Max(0f, wrongFeedbackDuration);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        _isShowingHanamiWrongMessage = false;

        // 오답 후 다시 현재 카드의 하남이 대사로 복귀
        int hanamiTextId = GetFilmHanamiTextId(logicalIndex);
        if (hanamiTextId > 0)
            SetTextWithFilmPage(hanamiTextId, logicalIndex);

        _hanamiTempMessageCoroutine = null;
    }

    private void StopWrongFeedbackCoroutineIfRunning()
    {
        if (_hanamiTempMessageCoroutine != null)
        {
            StopCoroutine(_hanamiTempMessageCoroutine);
            _hanamiTempMessageCoroutine = null;
        }
    }

    private void KillAllTweens()
    {
        for (int i = 0; i < _activeTweens.Count; i++)
        {
            if (_activeTweens[i] != null && _activeTweens[i].active)
                _activeTweens[i].Kill();
        }
        _activeTweens.Clear();
    }

    public void OnClickSortThought()
    {
        HandleSort(true);
    }

    public void OnClickSortFact()
    {
        HandleSort(false);
    }

    private void HandleSort(bool userChoseThought)
    {
        if (_interactionLocked) return;
        int logicalIndex = GetCurrentLogicalIndex();
        if (logicalIndex < 0) return;
        if (_isAdvancing) return;
        if (_isShowingHanamiWrongMessage) return; // 오답 메시지 표시 중 입력 차단
        if (logicalIndex >= FilmCount) return;
        if (_currentFilmInstance == null) return;

        bool correctIsThought = IsFilmThought(logicalIndex);
        bool userCorrect = (userChoseThought == correctIsThought);

        // 오답: 메시지만 표시하고 다시 시도하게 함
        if (!userCorrect)
        {
            ShowWrongFeedbackTemp(logicalIndex);
            return;
        }

        // 정답 처리
        int filmId = GetFilmId(logicalIndex);
        string text = GetFilmText(logicalIndex);

        if (_sortedFilmIds.Contains(filmId))
        {
            Debug.LogWarning("[Director_Problem1_Step3_Logic] 이미 분류된 필름 재처리 시도: filmId=" + filmId);
            return;
        }

        _sortedFilmIds.Add(filmId);

        string correctType = correctIsThought ? "생각" : "사실";

        var entry = new SortLogEntry
        {
            filmId = filmId,
            text = text,
            correctType = correctType,
            chosenType = correctType
        };
        _logs.Add(entry);

        PlaceCurrentFilmIntoCorrectSlot(correctIsThought);

        // 정답 피드백 표시
        if (dialogueSequencer != null)
        {
            int feedbackId = correctIsThought ? correctThoughtTextId : correctFactTextId;
            if (feedbackId > 0)
                SetTextWithFilmPage(feedbackId, logicalIndex);
        }

        _isAdvancing = true;
        StartCoroutine(AdvanceAfterDelayWithAnimation(true));
    }

    private void PlaceCurrentFilmIntoCorrectSlot(bool correctIsThought)
    {
        Transform[] slots = correctIsThought ? ThoughtSlots : FactSlots;
        List<GameObject> placedList = correctIsThought ? _placedThoughtFilmInstances : _placedFactFilmInstances;

        if (_currentFilmInstance == null)
            return;

        int nextSlotIndex = placedList.Count;

        if (slots == null || nextSlotIndex < 0 || nextSlotIndex >= slots.Length || slots[nextSlotIndex] == null)
        {
            Debug.LogWarning("[Director_Problem1_Step3_Logic] 배치 슬롯 부족/누락. "
                             + (correctIsThought ? "Thought" : "Fact")
                             + " slotIndex=" + nextSlotIndex + " / slots=" + (slots == null ? 0 : slots.Length));

            _currentFilmInstance.SetActive(false);
            placedList.Add(_currentFilmInstance);
            _currentFilmInstance = null;
            _currentFilmAnimator = null;
            return;
        }

        Transform slot = slots[nextSlotIndex];
        GameObject placedGo = _currentFilmInstance;
        Transform placedTr = placedGo.transform;

        // 현재 카드용 참조 끊기 (다음 카드 생성될 수 있도록)
        _currentFilmInstance = null;
        _currentFilmAnimator = null;

        // 월드 좌표 유지한 상태로 슬롯으로 이동 후, DOTween으로 슬롯 로컬 원점까지 이동
        placedTr.SetParent(slot, true);

        // 시작값 약간 강조 (선택 직후 연출)
        placedTr.DOComplete();
        Sequence seq = DOTween.Sequence();

        // scale punch 느낌
        seq.Join(placedTr.DOScale(MovePunchScale, 0.08f).SetEase(Ease.OutQuad));
        seq.Append(placedTr.DOLocalMove(Vector3.zero, MoveAnimDuration).SetEase(Ease.OutCubic));
        seq.Join(placedTr.DOLocalRotate(Vector3.zero, MoveAnimDuration).SetEase(Ease.OutCubic));
        seq.Join(placedTr.DOScale(Vector3.one, MoveAnimDuration).SetEase(Ease.OutBack));

        _activeTweens.Add(seq);

        placedList.Add(placedGo);
    }

    private void ResetToLocalIdentity(Transform t)
    {
        t.localPosition = Vector3.zero;
        t.localRotation = Quaternion.identity;
        t.localScale = Vector3.one;
    }

    private IEnumerator AdvanceAfterDelayWithAnimation(bool userCorrect)
    {
        float delay = Mathf.Max(0f, SortAdvanceDelay);

        // 오답이면 오답 문구 유지 보장
        if (!userCorrect)
            delay = Mathf.Max(delay, Mathf.Max(0f, wrongFeedbackDuration));

        if (delay > 0f)
            yield return new WaitForSeconds(delay);

        // 현재 카드는 이미 슬롯으로 이동되어 _currentFilmAnimator == null 일 수 있음
        if (_currentFilmAnimator != null)
            yield return StartCoroutine(_currentFilmAnimator.PlayExit());

        _isAdvancing = false;
        CompleteCurrentCard();
    }

    public void OnClickMic()
    {
        if (MicIndicator != null)
            MicIndicator.ToggleRecording();
    }

    public void OnClickSummaryButton()
    {
        if (_logs.Count < FilmCount)
        {
            Debug.LogWarning("[Director_Problem1_Step3_Logic] 아직 분류 미완료. logs=" + _logs.Count + ", films=" + FilmCount);
            return;
        }

        SaveSortLogToDb();

        if (StepRoot != null)
            StepRoot.SetActive(false);

        if (SummaryPanelRoot != null)
            SummaryPanelRoot.SetActive(true);
    }

    private void SaveSortLogToDb()
    {
        if (_logs.Count == 0)
        {
            Debug.Log("[Director_Problem1_Step3_Logic] 저장할 로그가 없어 DB 저장 스킵");
            return;
        }

        var body = new SortLogPayload { items = _logs.ToArray() };
        SaveAttempt(body);
    }
}