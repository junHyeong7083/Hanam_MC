using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem9 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem9_Step3_Logic(부모)에 있음.
/// </summary>
public class Director_Problem9_Step3 : Director_Problem9_Step3_Logic
{
    [Header("===== 라운드 데이터 =====")]
    [SerializeField] private RoundData[] rounds;

    [Header("===== 하남박스 =====")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextId_Fail;
    [SerializeField] private int guideTextId_Success;
    [SerializeField] private GameObject nextStepButtonRoot;

    [Header("===== 퍼즐 이미지 =====")]
    [SerializeField] private Image puzzleImage;
    [SerializeField] private Text puzzleText;

    [Header("===== 마이크 =====")]
    [SerializeField] private Button micButton;
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("===== 루트 =====")]
    [SerializeField] private GameObject mainRoot;
    [SerializeField] private GameObject completeRoot;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override RoundData[] Rounds => rounds;
    protected override Text GuideText => guideText;
    protected override int GuideTextId_Fail => guideTextId_Fail;
    protected override int GuideTextId_Success => guideTextId_Success;
    protected override GameObject NextStepButtonRoot => nextStepButtonRoot;
    protected override Image PuzzleImage => puzzleImage;
    protected override Text PuzzleText => puzzleText;
    protected override Button MicButton => micButton;
    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override GameObject MainRoot => mainRoot;
    protected override GameObject CompleteRoot => completeRoot;
}
