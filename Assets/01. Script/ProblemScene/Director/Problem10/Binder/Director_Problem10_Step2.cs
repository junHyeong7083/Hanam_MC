using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem10_Step2 - 문제10 스텝2 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 장르 데이터, 선택 화면, 완료 화면, 공유 데이터의 참조를 바인딩한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제10 > 스텝2 (메인 활동 - 장르 선택)
/// 【부모 클래스】 Director_Problem10_Step2_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem10 Step2 GameObject에 부착
/// 【참조되는 곳】 Director_Problem10_Step2_Logic (장르 선택 로직)
/// </summary>
public class Director_Problem10_Step2 : Director_Problem10_Step2_Logic
{
    [Header("===== 장르 데이터 =====")]
    [SerializeField] private GenreCardData[] genreCardsData;       // 4개 장르 카드 데이터

    [Header("===== 선택 화면 =====")]
    [SerializeField] private GameObject selectRoot;                // 장르 선택 UI 루트
    [SerializeField] private Button[] genreButtons;                // 장르 선택 버튼 (4개)
    [SerializeField] private GameObject[] selectIndicators;        // 선택 인디케이터 (4개)
    [SerializeField] private Text[] genreLabels;                   // 장르 라벨 텍스트 (4개)

    [Header("===== 완료 화면 =====")]
    [SerializeField] private GameObject completeRoot;              // 완료 UI 루트
    [SerializeField] private Image completeCardImage;              // 선택된 장르 카드 이미지
    [SerializeField] private Text completeCardLabel;               // 선택된 장르 라벨

    [Header("===== 공유 데이터 =====")]
    [SerializeField] private Problem10SharedData sharedData;       // Step3과 공유하는 ScriptableObject

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override GenreCardData[] GenreCardsData => genreCardsData;
    protected override GameObject SelectRoot => selectRoot;
    protected override Button[] GenreButtons => genreButtons;
    protected override GameObject[] SelectIndicators => selectIndicators;
    protected override Text[] GenreLabels => genreLabels;
    protected override GameObject CompleteRoot => completeRoot;
    protected override Image CompleteCardImage => completeCardImage;
    protected override Text CompleteCardLabel => completeCardLabel;
    protected override Problem10SharedData SharedData => sharedData;
}
