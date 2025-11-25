using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Problem1 / Step3 전용 공통 베이스
/// - 랜덤 카드 시퀀스(RandomCardSequenceStepBase)를 기반으로
///   "생각/사실" 통으로 분류 + 로그 저장 + 요약 패널 전환까지
///   전부 여기서 처리.
/// - 실제 Step 스크립트는
///   - 필드(films, 컨테이너, 버튼 등)만 SerializeField로 들고 있고
///   - 이 베이스의 추상 프로퍼티들을 override 해서 매핑만 해준다.
/// </summary>
public abstract class Director_Problem1_Step3_Logic : RandomCardSequenceStepBase
{
    [Serializable]
    protected class SortLogEntry
    {
        public int filmId;
        public string text;
        public string correctType;   // "생각" / "사실" (원래 타입)
        public string chosenType;    // "생각" / "사실" (사용자 선택)
    }

    // ====== 자식이 채워줄 추상 프로퍼티들 ======

    /// <summary>전체 필름 개수</summary>
    protected abstract int FilmCount { get; }

    /// <summary>index에 해당하는 필름의 ID</summary>
    protected abstract int GetFilmId(int index);

    /// <summary>index에 해당하는 필름의 텍스트</summary>
    protected abstract string GetFilmText(int index);

    /// <summary>index에 해당하는 필름이 '생각' 문장인지 여부</summary>
    protected abstract bool IsFilmThought(int index);

    // --- UI 바인딩용 추상 프로퍼티들 ---

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

    // ====== 내부 상태 ======
    private GameObject _currentFilmInstance;
    private Director_Problem1_Step3_FilmCardAnimator _currentFilmAnimator;

    private bool _isAdvancing;

    // "이미 한 번 분류된 필름인지" 확인용
    private readonly HashSet<int> _sortedFilmIds = new HashSet<int>();

    // 생각/사실 통에 들어간 항목들 (UI 갱신용)
    private readonly List<SortLogEntry> _thoughtEntries = new List<SortLogEntry>();
    private readonly List<SortLogEntry> _factEntries = new List<SortLogEntry>();

    // DB 저장용 전체 로그
    private readonly List<SortLogEntry> _logs = new List<SortLogEntry>();

    // ====== RandomCardSequenceStepBase 구현부 ======

    protected override int CardCount => FilmCount;

    protected override void OnSequenceReset()
    {
        _isAdvancing = false;
        _sortedFilmIds.Clear();
        _thoughtEntries.Clear();
        _factEntries.Clear();
        _logs.Clear();

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
        // 개별 카드 처리 후 특별한 추가처리 없음.
    }

    protected override void OnAllCardsProcessed()
    {
        // 모든 필름 분류 완료 시:
        if (AnswerButtonsRoot != null)
            AnswerButtonsRoot.SetActive(false);

        if (SummaryButtonRoot != null)
            SummaryButtonRoot.SetActive(true);
    }

    // ====== 내부 유틸 ======

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

    // ====== 버튼용 공개 메서드 ======

    public void OnClickSortThought() => HandleSort(true);   // '생각' 버튼
    public void OnClickSortFact() => HandleSort(false);     // '사실' 버튼

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

            string correctType = isThought ? "생각" : "사실";
            string chosenType = userChoseThought ? "생각" : "사실";

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

        // 현재 카드 완료 → 다음 카드 or 전체 완료
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
            Debug.Log("[Problem1_ThoughtFactSortStepBase] 저장할 로그가 없어 DB 저장 스킵");
            return;
        }

        var body = new
        {
            items = _logs.ToArray()
        };

        SaveAttempt(body);
    }
}
