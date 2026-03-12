using System;
using UnityEngine;

/// <summary>
/// Director_Problem1_Step3 - 문제1 스텝3의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 필름 데이터(FilmItem 배열), 카드 UI, 분류 슬롯, 버튼, 마이크 등을 바인딩한다.
///         실제 분류 로직(생각/사실 판정, 애니메이션, DB 저장)은 부모(Director_Problem1_Step3_Logic)에 있다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제1 / 스텝3 (마무리 - 필름 카드 생각/사실 분류)
/// 【부모 클래스】 Director_Problem1_Step3_Logic → RandomCardSequenceStepBase → ProblemStepBase
/// 【참조하는 곳】 씬의 Step3 GameObject에 부착
/// 【참조되는 곳】 Director_Problem1_Step3_Logic (abstract property/method 구현 제공)
/// </summary>
public class Director_Problem1_Step3 : Director_Problem1_Step3_Logic
{
    /// <summary>
    /// 개별 필름 아이템 데이터. 인스펙터에서 각 필름의 ID, 텍스트, 이미지, 유형을 설정한다.
    /// </summary>
    [Serializable]
    public class FilmItem
    {
        public int id;                  // 필름 고유 ID (로그/식별용)

        [Tooltip("DataTable 텍스트 ID (필름 카드 내용)")]
        public int filmTextId;          // CSV에서 가져올 필름 내용 텍스트 ID

        [Tooltip("카드 표시 시 하남이 대사 textId")]
        public int hanamiTextId;        // 이 카드가 표시될 때 하남이가 말하는 대사 ID

        public Sprite filmSprite;       // 필름 카드 이미지

        public bool isThought;          // true="생각", false="사실"
    }

    [Header("문항 설정")]
    [SerializeField] private FilmItem[] films;    // 필름 아이템 배열 (분류할 카드들)

    [Header("현재 필름 UI")]
    [SerializeField] private RectTransform currentFilmRoot;      // 현재 카드가 표시될 부모 영역
    [SerializeField] private GameObject currentFilmPrefab;       // 필름 카드 프리팹

    [Header("분류 후 배치 슬롯 (하이라키 pos들 순서대로)")]
    [Tooltip("생각 필름통 슬롯들")]
    [SerializeField] private Transform[] thoughtSlots;           // "생각" 분류된 카드가 이동할 슬롯들

    [Tooltip("사실 필름통 슬롯들")]
    [SerializeField] private Transform[] factSlots;              // "사실" 분류된 카드가 이동할 슬롯들

    [Header("정답 버튼 / 다음촬영 버튼 루트")]
    [SerializeField] private GameObject answerButtonsRoot;       // 생각/사실 선택 버튼 루트
    [SerializeField] private GameObject summaryButtonRoot;       // 모든 카드 분류 후 표시되는 요약 버튼

    [Header("분류 연출 딜레이")]
    [SerializeField] private float sortAdvanceDelay = 0.6f;      // 분류 후 다음 카드까지 대기 시간

    [Header("마이크")]
    [SerializeField] private MicRecordingIndicator micIndicator; // STT 마이크 인디케이터

    [Header("패널 전환")]
    [SerializeField] private GameObject stepRoot;                // 분류 UI 루트 (요약 시 숨김)
    [SerializeField] private GameObject summaryPanelRoot;        // 요약 패널 루트 (분류 완료 후 표시)

    // ===== 베이스 추상 프로퍼티/메서드 구현 (인스펙터 값을 Logic에 전달) =====

    protected override int FilmCount => films != null ? films.Length : 0;

    /// <summary>index번째 필름의 고유 ID를 반환한다.</summary>
    protected override int GetFilmId(int index)
    {
        if (films == null || index < 0 || index >= films.Length) return -1;
        return films[index].id;
    }

    protected override string GetFilmText(int index)
    {
        if (films == null || index < 0 || index >= films.Length) return null;
        return ProblemRuntime.L(films[index].filmTextId);
    }

    protected override Sprite GetFilmSprite(int index)
    {
        if (films == null || index < 0 || index >= films.Length) return null;
        return films[index].filmSprite;
    }

    protected override bool IsFilmThought(int index)
    {
        if (films == null || index < 0 || index >= films.Length) return false;
        return films[index].isThought;
    }

    protected override int GetFilmHanamiTextId(int index)
    {
        if (films == null || index < 0 || index >= films.Length) return -1;
        return films[index].hanamiTextId;
    }

    protected override RectTransform CurrentFilmRoot => currentFilmRoot;
    protected override GameObject CurrentFilmPrefab => currentFilmPrefab;

    protected override Transform[] ThoughtSlots => thoughtSlots;
    protected override Transform[] FactSlots => factSlots;

    protected override GameObject AnswerButtonsRoot => answerButtonsRoot;
    protected override GameObject SummaryButtonRoot => summaryButtonRoot;
    protected override float SortAdvanceDelay => sortAdvanceDelay;

    protected override MicRecordingIndicator MicIndicator => micIndicator;

    protected override GameObject StepRoot => stepRoot;
    protected override GameObject SummaryPanelRoot => summaryPanelRoot;
}
