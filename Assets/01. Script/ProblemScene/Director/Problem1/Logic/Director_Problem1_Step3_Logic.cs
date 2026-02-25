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

    protected abstract Text HanamiDialogueText { get; }
    protected abstract int GetHanamiDialogueTextId(int index);
    protected abstract float WrongHanamiMessageDuration { get; }

    protected abstract RectTransform CurrentFilmRoot { get; }
    protected abstract GameObject CurrentFilmPrefab { get; }

    protected abstract Transform[] ThoughtSlots { get; }
    protected abstract Transform[] FactSlots { get; }

    protected abstract GameObject AnswerButtonsRoot { get; }
    protected abstract GameObject SummaryButtonRoot { get; }
    protected abstract float SortAdvanceDelay { get; }

    protected abstract MicRecordingIndicator MicIndicator { get; }

    protected abstract GameObject StepRoot { get; }
    protected abstract GameObject SummaryPanelRoot { get; }

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

        StopHanamiTempMessageCoroutineIfRunning();
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
        RenderHanamiDefaultDialogue();
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

            if (!_isShowingHanamiWrongMessage)
                RenderHanamiDefaultDialogue();

            return;
        }

        string text = GetFilmText(logicalIndex);
        Sprite sprite = GetFilmSprite(logicalIndex);

        SpawnOrUpdateCurrentFilmCard(text, sprite);

        if (!_isShowingHanamiWrongMessage)
            RenderHanamiDefaultDialogue();

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

        if (!_isShowingHanamiWrongMessage)
            RenderHanamiDefaultDialogue();
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
            RenderHanamiDefaultDialogue();
            Debug.Log("[Director_Problem1_Step3_Logic] 모든 필름 분류 완료. 다음촬영 버튼 활성화.");
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

        var uiText = _currentFilmInstance.GetComponentInChildren<Text>(true);
        if (uiText != null)
            uiText.text = text ?? string.Empty;

        var uiImage = _currentFilmInstance.GetComponentInChildren<Image>(true);
        if (uiImage != null)
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

    private void RenderHanamiDefaultDialogue()
    {
        if (HanamiDialogueText == null)
            return;

        int defaultTextId = GetHanamiDialogueTextId(0);
        if (defaultTextId < 0)
        {
            HanamiDialogueText.text = string.Empty;
            return;
        }

        HanamiDialogueText.text = ProblemRuntime.L(defaultTextId);
    }

    private void RenderHanamiWrongDialogueTemp()
    {
        if (HanamiDialogueText == null)
            return;

        int wrongTextId = GetHanamiDialogueTextId(1);
        if (wrongTextId < 0)
        {
            RenderHanamiDefaultDialogue();
            return;
        }

        StopHanamiTempMessageCoroutineIfRunning();
        _hanamiTempMessageCoroutine = StartCoroutine(CoHanamiWrongDialogueTemp());
    }

    private IEnumerator CoHanamiWrongDialogueTemp()
    {
        _isShowingHanamiWrongMessage = true;

        int wrongTextId = GetHanamiDialogueTextId(1);
        if (wrongTextId >= 0 && HanamiDialogueText != null)
            HanamiDialogueText.text = ProblemRuntime.L(wrongTextId);

        float wait = Mathf.Max(0f, WrongHanamiMessageDuration);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        _isShowingHanamiWrongMessage = false;
        RenderHanamiDefaultDialogue();
        _hanamiTempMessageCoroutine = null;
    }

    private void StopHanamiTempMessageCoroutineIfRunning()
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
        int logicalIndex = GetCurrentLogicalIndex();
        if (logicalIndex < 0) return;
        if (_isAdvancing) return;
        if (logicalIndex >= FilmCount) return;
        if (_currentFilmInstance == null) return;

        int filmId = GetFilmId(logicalIndex);
        string text = GetFilmText(logicalIndex);
        bool correctIsThought = IsFilmThought(logicalIndex); // 정답 타입

        bool userCorrect = (userChoseThought == correctIsThought);

        if (_sortedFilmIds.Contains(filmId))
        {
            Debug.LogWarning("[Director_Problem1_Step3_Logic] 이미 분류된 필름 재처리 시도: filmId=" + filmId);
            return;
        }

        _sortedFilmIds.Add(filmId);

        string correctType = correctIsThought ? "생각" : "사실";
        string chosenType = userChoseThought ? "생각" : "사실";

        var entry = new SortLogEntry
        {
            filmId = filmId,
            text = text,
            correctType = correctType,
            chosenType = chosenType
        };
        _logs.Add(entry);

        // 핵심: 박스 배치는 "사용자 선택"이 아니라 "정답 타입" 기준
        PlaceCurrentFilmIntoCorrectSlot(correctIsThought);

        if (!userCorrect)
            RenderHanamiWrongDialogueTemp();
        else if (!_isShowingHanamiWrongMessage)
            RenderHanamiDefaultDialogue();

        _isAdvancing = true;
        StartCoroutine(AdvanceAfterDelayWithAnimation(userCorrect));
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

        // 오답이면 하남이 오답 문구 2초 유지 보장
        if (!userCorrect)
            delay = Mathf.Max(delay, Mathf.Max(0f, WrongHanamiMessageDuration));

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