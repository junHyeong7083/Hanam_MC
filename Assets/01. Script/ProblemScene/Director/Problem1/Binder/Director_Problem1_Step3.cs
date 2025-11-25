using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem1 / Step3
/// - 실제 로직은 Problem1_ThoughtFactSortStepBase에서 모두 처리.
/// - 이 클래스는
///   - films 데이터
///   - 각종 UI 참조들
///   만 SerializeField로 가지고 있고,
///   베이스의 추상 프로퍼티를 override 해서 매핑만 담당.
/// </summary>
public class Director_Problem1_Step3 : Director_Problem1_Step3_Logic
{
    [Serializable]
    public class FilmItem
    {
        public int id;           // 1..N (인스펙터에서 부여)
        [TextArea]
        public string text;      // 문장 내용
        public bool isThought;   // true=생각, false=사실
    }

    [Header("문항 설정 (인스펙터에서 입력)")]
    [SerializeField] private FilmItem[] films;

    [Header("현재 필름 UI")]
    [SerializeField] private RectTransform currentFilmRoot;
    [SerializeField] private GameObject currentFilmPrefab;

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

    // ===== Problem1_ThoughtFactSortStepBase 추상 프로퍼티 구현 =====

    protected override int FilmCount => films != null ? films.Length : 0;

    protected override int GetFilmId(int index)
    {
        if (films == null || index < 0 || index >= films.Length)
            return -1;
        return films[index].id;
    }

    protected override string GetFilmText(int index)
    {
        if (films == null || index < 0 || index >= films.Length)
            return null;
        return films[index].text;
    }

    protected override bool IsFilmThought(int index)
    {
        if (films == null || index < 0 || index >= films.Length)
            return false;
        return films[index].isThought;
    }

    protected override RectTransform CurrentFilmRoot => currentFilmRoot;
    protected override GameObject CurrentFilmPrefab => currentFilmPrefab;

    protected override GameObject AnswerButtonsRoot => answerButtonsRoot;
    protected override GameObject SummaryButtonRoot => summaryButtonRoot;

    protected override Transform ThoughtsContainer => thoughtsContainer;
    protected override Transform FactsContainer => factsContainer;
    protected override GameObject BinItemPrefab => binItemPrefab;
    protected override float SortAdvanceDelay => sortAdvanceDelay;

    protected override MicRecordingIndicator MicIndicator => micIndicator;

    protected override GameObject StepRoot => stepRoot;
    protected override GameObject SummaryPanelRoot => summaryPanelRoot;
}
