using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem10_Step3_Logic(부모)에 있음.
/// </summary>
public class Director_Problem10_Step3 : Director_Problem10_Step3_Logic
{
    [Header("===== 라운드 데이터 =====")]
    [SerializeField] private RoundData[] rounds = new RoundData[]
    {
        new RoundData
        {
            guideTextId = 101100007,
            hardcodedGuide = "",
            sttKeyword = "나의 용기있는 첫걸음",
            transitionGuideTextId = 101100008
        },
        new RoundData
        {
            guideTextId = 0,
            hardcodedGuide = "말하기 버튼을 누르고 \"완벽하지 않아도 괜찮아\"라는 다짐을 말해주세요.",
            sttKeyword = "완벽하지 않아도 괜찮아",
            transitionGuideTextId = 0
        }
    };

    [Header("===== 하남박스 =====")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextId_Success = 101100009;
    [SerializeField] private string failGuideText = "잘 들리지 않았어요. 다시 말해주세요.";
    [SerializeField] private Button nextDialogueBtn;

    [Header("===== 마이크 =====")]
    [SerializeField] private GameObject micRoot;
    [SerializeField] private Button micButton;
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("===== 포스터 =====")]
    [SerializeField] private Image genreCardImage;
    [SerializeField] private Text posterTitleText;
    [SerializeField] private Text posterCommitmentText;

    [Header("===== 공유 데이터 =====")]
    [SerializeField] private Problem10SharedData sharedData;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override RoundData[] Rounds => rounds;
    protected override Text GuideText => guideText;
    protected override int GuideTextId_Success => guideTextId_Success;
    protected override string FailGuideText => failGuideText;
    protected override Button NextDialogueBtn => nextDialogueBtn;
    protected override GameObject MicRoot => micRoot;
    protected override Button MicButton => micButton;
    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override Image GenreCardImage => genreCardImage;
    protected override Text PosterTitleText => posterTitleText;
    protected override Text PosterCommitmentText => posterCommitmentText;
    protected override Problem10SharedData SharedData => sharedData;
}
