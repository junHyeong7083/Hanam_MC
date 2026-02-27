using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem5 / Step1
/// - 줌 렌즈를 모니터 위에 드래그하여 드롭하는 스텝
/// - Problem2 Step1과 동일한 드래그앤드롭 로직 (리소스만 다름)
/// </summary>
public class Director_Problem5_Step1 : Director_Problem2_Step1_Logic
{
    [Header("Drop Box 영역")]
    [SerializeField] private UIDropBoxArea dropBoxArea;

    [Header("드래그 아이템")]
    [SerializeField] private Director_Problem2_DragItem[] dragItems;

    [Header("드롭 후 결과 패널")]
    [SerializeField] private GameObject resultPanelRoot;

    [Header("아이콘 이미지")]
    [SerializeField] private Image iconImageBackground;
    [SerializeField] private Image iconImage;

    [Header("인트로 애니메이션")]
    [SerializeField] private RectTransform leftEnterRoot;
    [SerializeField] private RectTransform rightEnterRoot;
    [SerializeField] private float introDuration = 0.5f;
    [SerializeField] private float leftStartOffsetX = -300f;
    [SerializeField] private float rightStartOffsetX = 300f;
    [SerializeField] private float introDelay = 0.1f;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("상단 가이드 텍스트")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextId = 101050002;

    [Header("다음 버튼")]
    [SerializeField] private GameObject nextButton;

    [Header("드래그 연출")]
    [SerializeField] private GameObject dragOutlineImage;
    [SerializeField] private GameObject textBox;

    // === 베이스 프로퍼티 구현 ===
    protected override UIDropBoxArea DropBoxArea => dropBoxArea;
    protected override Director_Problem2_DragItem[] DragItems => dragItems;
    protected override GameObject ResultPanelRoot => resultPanelRoot;
    protected override Image IconImageBackground => iconImageBackground;
    protected override Image IconImage => iconImage;
    protected override RectTransform LeftEnterRoot => leftEnterRoot;
    protected override RectTransform RightEnterRoot => rightEnterRoot;
    protected override float IntroDuration => introDuration;
    protected override float LeftStartOffsetX => leftStartOffsetX;
    protected override float RightStartOffsetX => rightStartOffsetX;
    protected override float IntroDelay => introDelay;
    protected override StepCompletionGate CompletionGate => completionGate;
    protected override Text GuideText => guideText;
    protected override int GuideTextId => guideTextId;
    protected override GameObject NextButton => nextButton;
    protected override GameObject DragOutlineImage => dragOutlineImage;
    protected override GameObject TextBox => textBox;
}
