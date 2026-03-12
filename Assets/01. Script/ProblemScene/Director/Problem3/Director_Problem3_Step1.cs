using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem3_Step1 - 문제3 스텝1의 Binder (인스펙터 바인딩) 클래스.
///
/// 【역할】 인스펙터에서 드롭 박스, 인트로 애니메이션, 드래그 상태 텍스트 등을 바인딩한다.
///         Problem2~9 공통 Step1 로직(Director_Problem2_Step1_Logic)을 상속한다.
///         Problem3에서는 추가로 드래그 전/후 상태 텍스트와 색상을 설정할 수 있다.
/// 【패턴】 Binder/Logic 패턴의 Binder 측.
/// 【문제/스텝】 Director 테마 / 문제3 / 스텝1 (도입부 - 아이템 드래그앤드롭)
/// 【부모 클래스】 Director_Problem2_Step1_Logic → ProblemStepBase
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
