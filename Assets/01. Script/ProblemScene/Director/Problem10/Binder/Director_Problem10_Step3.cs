using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem10_Step3_Logic(부모)에 있음.
/// </summary>
public class Director_Problem10_Step3 : Director_Problem10_Step3_Logic
{
    [Header("===== 장르별 다짐 데이터 =====")]
    [Tooltip("인덱스 0~3: Step2에서 선택한 장르에 따라 다짐 안내/STT 키워드 결정")]
    [SerializeField] private GenreCommitmentData[] genreCommitments;

    [Header("===== 실패 안내 =====")]
    [SerializeField] private int failGuideTextId;

    [Header("===== 마이크 =====")]
    [SerializeField] private GameObject micRoot;
    [SerializeField] private Button micButton;
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("===== 포스터 =====")]
    [SerializeField] private Image genreCardImage;
    [SerializeField] private Text posterCommitmentText;

    [Header("===== 공유 데이터 =====")]
    [SerializeField] private Problem10SharedData sharedData;

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
