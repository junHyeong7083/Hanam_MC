using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director 테마 / Problem1 / Step3
/// - films 배열에서 랜덤 순서로 한 문장씩 보여줌
/// - 사용자가 '생각' / '사실' 버튼을 누르면
///   => 사용자가 누른 통(생각/사실)에 그대로 들어감 (정답/오답 상관 X)
/// - 분류 로그를 쌓았다가 요약 보기 시점에 Attempt로 DB에 저장
/// - Summary 버튼 클릭 시: StepRoot 비활성화, SummaryPanel 활성화
/// </summary>
public class Director_Problem1_Step3 : MonoBehaviour
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
    class SortLogEntry
    {
        public int filmId;
        public string text;

        public string correctType;   // "생각" 또는 "사실" (원래 타입)
        public string chosenType;    // "생각" 또는 "사실" (사용자 선택)
    }

    [Serializable]
    class SortLogPayload
    {
        public string stepKey;       // 예: "Director_Problem1_Step3"
        public string theme;         // "Director" / "Gardener"
        public int problemIndex;     // 1..10

        public SortLogEntry[] items; // 개별 필름 로그
    }

    [Header("문항 설정 (인스펙터에서 입력)")]
    [SerializeField] private FilmItem[] films;

    [Header("현재 필름 UI")]
    [SerializeField] private RectTransform currentFilmRoot;
    [SerializeField] private GameObject currentFilmPrefab;
    [SerializeField] private Text progressText;      // 우상단 "0/8"

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

    // ===== DB 메타 정보 =====
    // 전부 인스펙터에 안 보이게 private
    private ProblemTheme _theme = ProblemTheme.Director;
    private int _problemIndex = 1;
    private string _problemId;
    private string _sessionId;
    private string _userEmail;

    // 외부(ProblemSceneController 같은 곳)에서 한 번만 셋업해주면 됨
    public void ConfigureMeta(
        ProblemTheme theme,
        int problemIndex,
        string problemId,
        string sessionId,
        string userEmail)
    {
        _theme = theme;
        _problemIndex = problemIndex;
        _problemId = problemId;
        _sessionId = sessionId;
        _userEmail = userEmail;
    }

    // 내부 상태
    private int _currentIndex;       // 0..films.Length
    private int[] _order;            // 랜덤 인덱스 순서
    private bool _isAdvancing;

    private readonly List<FilmItem> _thoughtFilms = new();
    private readonly List<FilmItem> _factFilms = new();
    private readonly List<SortLogEntry> _logs = new();

    private void OnEnable()
    {
        if (summaryPanelRoot != null)
            summaryPanelRoot.gameObject.SetActive(false);

        ResetState();
    }

    private void ResetState()
    {
        _currentIndex = 0;
        _isAdvancing = false;

        _thoughtFilms.Clear();
        _factFilms.Clear();
        _logs.Clear();

        ClearContainer(thoughtsContainer);
        ClearContainer(factsContainer);

        BuildRandomOrder();

        if (summaryButtonRoot != null) summaryButtonRoot.SetActive(false);
        if (answerButtonsRoot != null) answerButtonsRoot.SetActive(true);

        UpdateCurrentFilmUI();  // 첫 카드 표시 + 등장 애니메이션
        UpdateProgressUI();
    }

    private void BuildRandomOrder()
    {
        int total = (films != null) ? films.Length : 0;
        _order = new int[total];

        for (int i = 0; i < total; i++)
            _order[i] = i;

        // Fisher–Yates 셔플
        for (int i = total - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (_order[i], _order[j]) = (_order[j], _order[i]);
        }
    }

    private void ClearContainer(Transform t)
    {
        if (t == null) return;

        for (int i = t.childCount - 1; i >= 0; i--)
            Destroy(t.GetChild(i).gameObject);
    }

    private int GetCurrentFilmIndex()
    {
        if (films == null || _order == null) return -1;
        if (_currentIndex < 0 || _currentIndex >= _order.Length) return -1;
        return _order[_currentIndex];
    }

    private void UpdateCurrentFilmUI()
    {
        int idx = GetCurrentFilmIndex();
        bool isComplete = (idx < 0);

        if (!isComplete)
        {
            SpawnOrUpdateCurrentFilmCard(films[idx].text);

            // 새로운 문장이 세팅될 때마다 등장 애니메이션 실행
            if (_currentFilmAnimator != null)
                _currentFilmAnimator.PlayEnter();
        }
        else
        {
            DestroyCurrentFilmCard();
        }

        if (answerButtonsRoot != null)
            answerButtonsRoot.SetActive(!isComplete);

        if (summaryButtonRoot != null)
            summaryButtonRoot.SetActive(isComplete);
    }

    private void SpawnOrUpdateCurrentFilmCard(string text)
    {
        if (currentFilmRoot == null || currentFilmPrefab == null) return;

        if (_currentFilmInstance == null)
        {
            _currentFilmInstance = Instantiate(currentFilmPrefab, currentFilmRoot, false);
            _currentFilmAnimator = _currentFilmInstance.GetComponent<Director_Problem1_Step3_FilmCardAnimator>();
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

    private void UpdateProgressUI()
    {
        if (progressText == null) return;

        int total = (films != null) ? films.Length : 0;
        int current = Mathf.Clamp(_currentIndex, 0, total);
        progressText.text = $"{current}/{total}";
    }

    // === 버튼에서 연결할 함수들 ===

    public void OnClickSortThought() => HandleSort(true);   // 사용자가 '생각' 버튼 클릭
    public void OnClickSortFact() => HandleSort(false);   // 사용자가 '사실' 버튼 클릭

    private void HandleSort(bool userChoseThought)
    {
        int idx = GetCurrentFilmIndex();
        if (idx < 0) return;
        if (_isAdvancing) return;

        var film = films[idx];

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
    /// 현재 카드 퇴장 애니메이션 → 다음 카드 표시 + 등장 애니메이션
    /// </summary>
    private IEnumerator AdvanceAfterDelayWithAnimation()
    {
        // 1) 선택 후 잠깐 정지
        if (sortAdvanceDelay > 0f)
            yield return new WaitForSeconds(sortAdvanceDelay);

        // 2) 카드 퇴장 애니메이션
        if (_currentFilmAnimator != null)
            yield return StartCoroutine(_currentFilmAnimator.PlayExit());

        // 3) 인덱스 증가 후 다음 카드 세팅 + 등장 애니메이션
        _currentIndex++;
        _isAdvancing = false;

        UpdateCurrentFilmUI();
        UpdateProgressUI();
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

    // 마이크 버튼 (지금은 이펙트만, STT는 나중에)
    public void OnClickMic()
    {
        if (micIndicator != null)
            micIndicator.ToggleRecording();
    }

    // 요약 보기 버튼에서 호출:
    // 1) 지금까지의 분류 로그를 DB에 저장
    // 2) stepRoot 비활성화, summaryPanelRoot 활성화
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

        if (DataService.Instance == null || DataService.Instance.User == null)
        {
            Debug.LogWarning("[Director_Problem1_Step3] DataService.Instance.User 없음 - DB 저장 불가");
            return;
        }

        var payload = new SortLogPayload
        {
            stepKey = "Director_Problem1_Step3",
            theme = _theme.ToString(),
            problemIndex = _problemIndex,
            items = _logs.ToArray()
        };

        string contentJson = JsonUtility.ToJson(payload);

        var attempt = new Attempt
        {
            SessionId = _sessionId,
            UserEmail = _userEmail,
            Content = contentJson,
            ProblemId = string.IsNullOrEmpty(_problemId) ? null : _problemId,
            Theme = _theme,
            ProblemIndex = _problemIndex
        };

        DataService.Instance.User.SaveAttempt(attempt);
        Debug.Log("[Director_Problem1_Step3] SaveAttempt 호출 완료");
    }
}
