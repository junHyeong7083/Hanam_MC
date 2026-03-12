using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem10_Step3 - 문제10 스텝3 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 장르별 다짐 데이터, 마이크, 포스터, 공유 데이터의 참조를 바인딩한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제10 > 스텝3 (마무리 - STT 다짐 말하기 + 포스터 작성)
/// 【부모 클래스】 Director_Problem10_Step3_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem10 Step3 GameObject에 부착
/// 【참조되는 곳】 Director_Problem10_Step3_Logic (다짐 말하기 로직)
/// </summary>
public class Director_Problem10_Step3 : Director_Problem10_Step3_Logic
{
    [Header("===== 장르별 다짐 데이터 =====")]
    [Tooltip("인덱스 0~3: Step2에서 선택한 장르에 따라 다짐 안내/STT 키워드 결정")]
    [SerializeField] private GenreCommitmentData[] genreCommitments; // 4개 장르의 다짐 데이터

    [Header("===== 실패 안내 =====")]
    [SerializeField] private int failGuideTextId;                    // STT 실패 시 안내 textId

    [Header("===== 마이크 =====")]
    [SerializeField] private GameObject micRoot;                     // 마이크 UI 루트
    [SerializeField] private Button micButton;                       // 마이크 녹음 버튼
    [SerializeField] private MicRecordingIndicator micIndicator;     // STT 녹음 인디케이터

    [Header("===== 포스터 =====")]
    [SerializeField] private Image genreCardImage;                   // 장르별 포스터 이미지
    [SerializeField] private Text posterCommitmentText;              // 포스터 다짐 텍스트

    [Header("===== 공유 데이터 =====")]
    [SerializeField] private Problem10SharedData sharedData;         // Step2와 공유하는 ScriptableObject

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override GenreCommitmentData[] GenreCommitments => genreCommitments;
    protected override int FailGuideTextId => failGuideTextId;
    protected override GameObject MicRoot => micRoot;
    protected override Button MicButton => micButton;
    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override Image GenreCardImage => genreCardImage;
    protected override Text PosterCommitmentText => posterCommitmentText;
    protected override Problem10SharedData SharedData => sharedData;
}
