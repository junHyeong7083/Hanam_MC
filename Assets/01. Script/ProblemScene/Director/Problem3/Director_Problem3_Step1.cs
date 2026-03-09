using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem3 / Step1
/// </summary>
public class Director_Problem3_Step1 : Director_Problem2_Step1_Logic
{
    [Header("Drop Box 영역")]
    [SerializeField] private UIDropBoxArea dropBoxArea;

    [Header("인트로 애니메이션")]
    [SerializeField] private RectTransform leftEnterRoot;
    [SerializeField] private RectTransform rightEnterRoot;
    [SerializeField] private float introDuration = 0.5f;
    [SerializeField] private float leftStartOffsetX = -300f;
    [SerializeField] private float rightStartOffsetX = 300f;
    [SerializeField] private float introDelay = 0.1f;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("드래그 상태 텍스트")]
    [SerializeField] private Text dragStateText;
    [SerializeField] private int beforeDragTextId;
    [SerializeField] private int afterDragTextId;
    [SerializeField] private Color afterDragTextColor = Color.white;

    protected override UIDropBoxArea DropBoxArea => dropBoxArea;
    protected override RectTransform LeftEnterRoot => leftEnterRoot;
    protected override RectTransform RightEnterRoot => rightEnterRoot;
    protected override float IntroDuration => introDuration;
    protected override float LeftStartOffsetX => leftStartOffsetX;
    protected override float RightStartOffsetX => rightStartOffsetX;
    protected override float IntroDelay => introDelay;
    protected override StepCompletionGate CompletionGate => completionGate;

    protected override Text DragStateText => dragStateText;
    protected override int BeforeDragTextId => beforeDragTextId;
    protected override int AfterDragTextId => afterDragTextId;
    protected override Color AfterDragTextColor => afterDragTextColor;
}
