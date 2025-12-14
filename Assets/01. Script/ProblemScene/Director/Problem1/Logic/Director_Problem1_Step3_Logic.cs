using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem1 / Step3 ���� ���� ���̽�
/// - ���� ī�� ������(RandomCardSequenceStepBase)�� �������
///   "����/���" ������ �з� + �α� ���� + ��� �г� ��ȯ����
///   ���� ���⼭ ó��.
/// - ���� Step ��ũ��Ʈ��
///   - �ʵ�(films, �����̳�, ��ư ��)�� SerializeField�� ��� �ְ�
///   - �� ���̽��� �߻� ������Ƽ���� override �ؼ� ���θ� ���ش�.
/// </summary>
public abstract class Director_Problem1_Step3_Logic : RandomCardSequenceStepBase
{
    [Serializable]
    protected class SortLogEntry
    {
        public int filmId;
        public string text;
        public string correctType;   // "����" / "���" (���� Ÿ��)
        public string chosenType;    // "����" / "���" (����� ����)
    }

    [Serializable]
    private class SortLogPayload
    {
        public SortLogEntry[] items;
    }

    // ====== �ڽ��� ä���� �߻� ������Ƽ�� ======

    /// <summary>��ü �ʸ� ����</summary>
    protected abstract int FilmCount { get; }

    /// <summary>index�� �ش��ϴ� �ʸ��� ID</summary>
    protected abstract int GetFilmId(int index);

    /// <summary>index�� �ش��ϴ� �ʸ��� �ؽ�Ʈ</summary>
    protected abstract string GetFilmText(int index);

    /// <summary>index�� �ش��ϴ� �ʸ��� '����' �������� ����</summary>
    protected abstract bool IsFilmThought(int index);

    // --- UI ���ε��� �߻� ������Ƽ�� ---

    protected abstract RectTransform CurrentFilmRoot { get; }
    protected abstract GameObject CurrentFilmPrefab { get; }

    protected abstract GameObject AnswerButtonsRoot { get; }
    protected abstract GameObject SummaryButtonRoot { get; }

    protected abstract Transform ThoughtsContainer { get; }
    protected abstract Transform FactsContainer { get; }
    protected abstract GameObject BinItemPrefab { get; }
    protected abstract float SortAdvanceDelay { get; }

    protected abstract MicRecordingIndicator MicIndicator { get; }

    protected abstract GameObject StepRoot { get; }
    protected abstract GameObject SummaryPanelRoot { get; }

    // ====== ���� ���� ======
    private GameObject _currentFilmInstance;
    private Director_Problem1_Step3_FilmCardAnimator _currentFilmAnimator;

    private bool _isAdvancing;

    // "�̹� �� �� �з��� �ʸ�����" Ȯ�ο�
    private readonly HashSet<int> _sortedFilmIds = new HashSet<int>();

    // ����/��� �뿡 �� �׸�� (UI ���ſ�)
    private readonly List<SortLogEntry> _thoughtEntries = new List<SortLogEntry>();
    private readonly List<SortLogEntry> _factEntries = new List<SortLogEntry>();

    // DB ����� ��ü �α�
    private readonly List<SortLogEntry> _logs = new List<SortLogEntry>();

    // ====== RandomCardSequenceStepBase ������ ======

    protected override int CardCount => FilmCount;

    protected override void OnSequenceReset()
    {
        _isAdvancing = false;
        _sortedFilmIds.Clear();
        _thoughtEntries.Clear();
        _factEntries.Clear();
        _logs.Clear();

        // MicIndicator 이벤트 구독
        if (MicIndicator != null)
        {
            MicIndicator.OnKeywordAMatched -= OnSTTThought;
            MicIndicator.OnKeywordBMatched -= OnSTTFact;
            MicIndicator.OnKeywordAMatched += OnSTTThought;
            MicIndicator.OnKeywordBMatched += OnSTTFact;
            MicIndicator.SetRecording(false);
        }

        ClearContainer(ThoughtsContainer);
        ClearContainer(FactsContainer);

        if (SummaryPanelRoot != null)
            SummaryPanelRoot.SetActive(false);

        if (SummaryButtonRoot != null)
            SummaryButtonRoot.SetActive(false);

        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(true);

        DestroyCurrentFilmCard();
    }

    private void OnSTTThought() => HandleSort(true);
    private void OnSTTFact() => HandleSort(false);

    protected override void OnApplyCardToUI(int logicalIndex)
    {
        if (logicalIndex < 0 || logicalIndex >= FilmCount)
        {
            DestroyCurrentFilmCard();
            if (AnswerButtonsRoot != null)
                AnswerButtonsRoot.SetActive(false);
            return;
        }

        string text = GetFilmText(logicalIndex);
        SpawnOrUpdateCurrentFilmCard(text);

        if (_currentFilmAnimator != null)
            _currentFilmAnimator.PlayEnter();

        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(true);
    }

    protected override void OnClearCurrentCardUI()
    {
        DestroyCurrentFilmCard();
        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(false);
    }

    protected override void OnCardProcessed(int logicalIndex)
    {
        // ���� ī�� ó�� �� Ư���� �߰�ó�� ����.
    }

    protected override void OnAllCardsProcessed()
    {
        // ��� �ʸ� �з� �Ϸ� ��:
        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(false);

        if (SummaryButtonRoot != null)
            SummaryButtonRoot.SetActive(true);
    }

    // ====== ���� ��ƿ ======

    private void ClearContainer(Transform t)
    {
        if (t == null) return;

        for (int i = t.childCount - 1; i >= 0; i--)
            UnityEngine.Object.Destroy(t.GetChild(i).gameObject);
    }

    private void SpawnOrUpdateCurrentFilmCard(string text)
    {
        if (CurrentFilmRoot == null || CurrentFilmPrefab == null) return;

        if (_currentFilmInstance == null)
        {
            _currentFilmInstance = UnityEngine.Object.Instantiate(
                CurrentFilmPrefab,
                CurrentFilmRoot,
                false
            );
            _currentFilmAnimator =
                _currentFilmInstance.GetComponent<Director_Problem1_Step3_FilmCardAnimator>();
        }

        var uiText = _currentFilmInstance.GetComponentInChildren<Text>();
        if (uiText != null) uiText.text = text;
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

    private void RefreshBinsUI()
    {
        ClearContainer(ThoughtsContainer);
        ClearContainer(FactsContainer);

        if (BinItemPrefab == null) return;

        foreach (var e in _thoughtEntries)
            CreateBinItem(ThoughtsContainer, e.text);

        foreach (var e in _factEntries)
            CreateBinItem(FactsContainer, e.text);
    }

    private void CreateBinItem(Transform parent, string text)
    {
        if (parent == null || BinItemPrefab == null) return;

        var go = UnityEngine.Object.Instantiate(BinItemPrefab, parent);
        var uiText = go.GetComponentInChildren<Text>();
        if (uiText != null)
            uiText.text = text;
    }

    // ====== ��ư�� ���� �޼��� ======

    public void OnClickSortThought() => HandleSort(true);   // '����' ��ư
    public void OnClickSortFact() => HandleSort(false);     // '���' ��ư

    private void HandleSort(bool userChoseThought)
    {
        int logicalIndex = GetCurrentLogicalIndex();
        if (logicalIndex < 0) return;
        if (_isAdvancing) return;
        if (logicalIndex < 0 || logicalIndex >= FilmCount) return;

        int filmId = GetFilmId(logicalIndex);
        string text = GetFilmText(logicalIndex);
        bool isThought = IsFilmThought(logicalIndex);

        bool alreadySorted = _sortedFilmIds.Contains(filmId);

        if (!alreadySorted)
        {
            _sortedFilmIds.Add(filmId);

            string correctType = isThought ? "����" : "���";
            string chosenType = userChoseThought ? "����" : "���";

            var entry = new SortLogEntry
            {
                filmId = filmId,
                text = text,
                correctType = correctType,
                chosenType = chosenType
            };

            _logs.Add(entry);

            if (userChoseThought)
                _thoughtEntries.Add(entry);
            else
                _factEntries.Add(entry);

            RefreshBinsUI();
        }

        _isAdvancing = true;
        StartCoroutine(AdvanceAfterDelayWithAnimation());
    }

    private IEnumerator AdvanceAfterDelayWithAnimation()
    {
        if (SortAdvanceDelay > 0f)
            yield return new WaitForSeconds(SortAdvanceDelay);

        if (_currentFilmAnimator != null)
            yield return StartCoroutine(_currentFilmAnimator.PlayExit());

        _isAdvancing = false;

        // ���� ī�� �Ϸ� �� ���� ī�� or ��ü �Ϸ�
        CompleteCurrentCard();
    }

    public void OnClickMic()
    {
        if (MicIndicator != null)
            MicIndicator.ToggleRecording();
    }

    public void OnClickSummaryButton()
    {
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
            Debug.Log("[Problem1_ThoughtFactSortStepBase] ������ �αװ� ���� DB ���� ��ŵ");
            return;
        }

        var body = new SortLogPayload
        {
            items = _logs.ToArray()
        };

        SaveAttempt(body);
    }
}
