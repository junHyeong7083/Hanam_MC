using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director 테마 / Problem1 / Step3
/// - RandomCardSequenceStepBase를 상속해
///   films[] 배열을 랜덤 순서로 하나씩 보여주고,
///   사용자가 '생각' / '사실' 버튼으로 분류.
/// - 분류된 문장은 각 통(thoughtsContainer, factsContainer)에 들어가고,
///   로그는 _logs에 누적 후 Summary 버튼에서 SaveAttempt로 저장.
/// </summary>
public class Director_Problem1_Step3 : RandomCardSequenceStepBase
{
    [Serializable]
    public class FilmItem
    {
        public int id;           // 1..N (인스펙터에서 부여)
        [TextArea]
        public string text;      // 문장 내용
        public bool isThought;   // 정답 타입: true=생각, false=사실
    }

    [Serializable]
    public class SortLogEntry
    {
        public int filmId;
        public string text;
        public string correctType;   // "생각" 또는 "사실" (원래 타입)
        public string chosenType;    // "생각" 또는 "사실" (사용자 선택)
    }

    [Header("문항 설정 (인스펙터에서 입력)")]
    [SerializeField] private FilmItem[] films;

    [Header("현재 필름 UI")]
    [SerializeField] private RectTransform currentFilmRoot;
    [SerializeField] private GameObject currentFilmPrefab;

    private GameObject _currentFilmInstance;
    private Director_Problem1_Step3_FilmCardAnimator _currentFilmAnimator;

    [Header("정답 버튼 / 요약 버튼 루트")]
    [SerializeField] private GameObject answerButtonsRoot;   // 생각/사실 버튼 묶음
    [SerializeField] private GameObject summaryButtonRoot;   // 요약 보기 버튼 묶음

    [Header("필름통 UI")]
    [SerializeField] private Transform thoughtsContainer;    // 생각 필름통 Content
    [SerializeField] private Transform factsContainer;       // 사실 필름통 Content
    [SerializeField] private GameObject binItemPrefab;       // 한 줄짜리 카드 프리팹
    [SerializeField] private float sortAdvanceDelay = 0.6f;  // 선택 후 잠깐 기다리는 시간

    [Header("마이크 이펙트 (선택사항)")]
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("패널 전환")]
    [SerializeField] private GameObject stepRoot;         // 현재 Step3 패널 루트
    [SerializeField] private GameObject summaryPanelRoot; // 요약 패널 루트

    // 내부 상태
    private bool _isAdvancing;

    private readonly List<FilmItem> _thoughtFilms = new();
    private readonly List<FilmItem> _factFilms = new();
    private readonly List<SortLogEntry> _logs = new();

    // === RandomCardSequenceStepBase 구현부 ===

    protected override int CardCount => (films != null) ? films.Length : 0;

    protected override void OnSequenceReset()
    {
        _isAdvancing = false;
        _thoughtFilms.Clear();
        _factFilms.Clear();
        _logs.Clear();

        ClearContainer(thoughtsContainer);
        ClearContainer(factsContainer);

        if (summaryPanelRoot != null)
            summaryPanelRoot.SetActive(false);

        if (summaryButtonRoot != null)
            summaryButtonRoot.SetActive(false);

        if (answerButtonsRoot != null)
            answerButtonsRoot.SetActive(true);
    }

    protected override void OnApplyCardToUI(int logicalIndex)
    {
        if (films == null || logicalIndex < 0 || logicalIndex >= films.Length)
        {
            DestroyCurrentFilmCard();
            if (answerButtonsRoot != null)
                answerButtonsRoot.SetActive(false);
            return;
        }

        var film = films[logicalIndex];
        SpawnOrUpdateCurrentFilmCard(film.text);

        // 새로운 문장이 세팅될 때마다 등장 애니메이션 실행
        if (_currentFilmAnimator != null)
            _currentFilmAnimator.PlayEnter();

        if (answerButtonsRoot != null)
            answerButtonsRoot.SetActive(true);
    }

    protected override void OnClearCurrentCardUI()
    {
        DestroyCurrentFilmCard();
        if (answerButtonsRoot != null)
            answerButtonsRoot.SetActive(false);
    }

    protected override void OnCardProcessed(int logicalIndex)
    {
        // 여기서는 특별히 할 일 없음.
        // 분류/로그 저장은 HandleSort 내부에서 이미 처리됨.
    }

    protected override void OnAllCardsProcessed()
    {
        // 모든 필름 분류 완료 시:
        // - answerButtonsRoot 숨기고
        // - summaryButtonRoot 활성화
        if (answerButtonsRoot != null)
            answerButtonsRoot.SetActive(false);

        if (summaryButtonRoot != null)
            summaryButtonRoot.SetActive(true);
    }

    // === 내부 유틸 ===

    private void ClearContainer(Transform t)
    {
        if (t == null) return;

        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    private void SpawnOrUpdateCurrentFilmCard(string text)
    {
        if (currentFilmRoot == null || currentFilmPrefab == null) return;

        if (_currentFilmInstance == null)
        {
            _currentFilmInstance = Instantiate(currentFilmPrefab, currentFilmRoot, false);
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
            Destroy(_currentFilmInstance);
            _currentFilmInstance = null;
            _currentFilmAnimator = null;
        }
    }

    private void RefreshBinsUI()
    {
        ClearContainer(thoughtsContainer);
        ClearContainer(factsContainer);

        if (binItemPrefab == null) return;

        foreach (var film in _thoughtFilms)
            CreateBinItem(thoughtsContainer, film.text);

        foreach (var film in _factFilms)
            CreateBinItem(factsContainer, film.text);
    }

    private void CreateBinItem(Transform parent, string text)
    {
        if (parent == null || binItemPrefab == null) return;

        var go = Instantiate(binItemPrefab, parent);
        var uiText = go.GetComponentInChildren<Text>();
        if (uiText != null)
            uiText.text = text;
    }

    // === 버튼에서 연결할 함수들 ===

    public void OnClickSortThought() => HandleSort(true);   // 사용자가 '생각' 버튼 클릭
    public void OnClickSortFact() => HandleSort(false);     // 사용자가 '사실' 버튼 클릭

    private void HandleSort(bool userChoseThought)
    {
        int logicalIndex = GetCurrentLogicalIndex();
        if (logicalIndex < 0) return;
        if (_isAdvancing) return;
        if (films == null || logicalIndex < 0 || logicalIndex >= films.Length) return;

        var film = films[logicalIndex];

        // 이미 분류된 필름이면 패스
        bool alreadySorted =
            _thoughtFilms.Exists(f => f.id == film.id) ||
            _factFilms.Exists(f => f.id == film.id);

        if (!alreadySorted)
        {
            // 1) 실제 박스에는 "사용자가 선택한 통" 기준으로 넣는다 (정답/오답 상관 없음)
            if (userChoseThought)
                _thoughtFilms.Add(film);
            else
                _factFilms.Add(film);

            // 2) 로그에는 "생각/사실" 문자열로 저장 (관리자용)
            string correctType = film.isThought ? "생각" : "사실";
            string chosenType = userChoseThought ? "생각" : "사실";

            _logs.Add(new SortLogEntry
            {
                filmId = film.id,
                text = film.text,
                correctType = correctType,
                chosenType = chosenType
            });

            RefreshBinsUI();
        }

        // 선택 후 중복 입력 방지
        _isAdvancing = true;
        StartCoroutine(AdvanceAfterDelayWithAnimation());
    }

    /// <summary>
    /// 선택 후 잠깐 기다렸다가( sortAdvanceDelay )
    /// 현재 카드 퇴장 애니메이션 → 다음 카드로 진행(CompleteCurrentCard)
    /// </summary>
    private IEnumerator AdvanceAfterDelayWithAnimation()
    {
        // 1) 선택 후 잠깐 정지
        if (sortAdvanceDelay > 0f)
            yield return new WaitForSeconds(sortAdvanceDelay);

        // 2) 카드 퇴장 애니메이션
        if (_currentFilmAnimator != null)
            yield return StartCoroutine(_currentFilmAnimator.PlayExit());

        _isAdvancing = false;

        // 3) 현재 카드 처리 완료 통보
        //   - 내부적으로:
        //     - OnCardProcessed 호출
        //     - completionGate.MarkOneDone
        //     - _currentIndex++ 후 다음 카드 UI 적용 or 전체 완료 처리
        CompleteCurrentCard();
    }

    // 마이크 버튼 (지금은 이펙트만, STT는 나중에)
    public void OnClickMic()
    {
        if (micIndicator != null)
            micIndicator.ToggleRecording();
    }

    public void OnClickSummaryButton()
    {
        SaveSortLogToDb();

        if (stepRoot != null)
            stepRoot.SetActive(false);

        if (summaryPanelRoot != null)
            summaryPanelRoot.SetActive(true);
    }

    private void SaveSortLogToDb()
    {
        if (_logs.Count == 0)
        {
            Debug.Log("[Director_Problem1_Step3] 저장할 로그가 없어 DB 저장 스킵");
            return;
        }

        // ProblemStepBase.SaveAttempt 사용
        // (context null 이면 내부에서 경고 로그 찍고 스킵)
        var body = new
        {
            items = _logs.ToArray()
        };

        SaveAttempt(body);
    }
}
