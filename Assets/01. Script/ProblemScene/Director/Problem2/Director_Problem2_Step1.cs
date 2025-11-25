using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem2 / Step1
/// - 인스펙터에서 UI 오브젝트만 바인딩.
/// - 실제 동작은 Director_Problem2_Step1_Logic(부모)에서 처리.
/// </summary>
public class Director_Problem2_Step1 : Director_Problem2_Step1_Logic
{
    [Header("Drop Box 영역 (공통 컴포넌트)")]
    [SerializeField] private UIDropBoxArea dropBoxArea;

    [Header("Items")]
    [SerializeField] private Director_Problem2_DragItem[] dragItems;

    [Header("UI After Drop")]
    [SerializeField] private GameObject resultPanelRoot;

    [Header("Icon Images")]
    [SerializeField] private Image iconImageBackground;
    [SerializeField] private Image iconImage;

    [Header("Intro Animation Roots")]
    [SerializeField] private RectTransform leftEnterRoot;
    [SerializeField] private RectTransform rightEnterRoot;

    [Header("Intro Animation Settings")]
    [SerializeField] private float introDuration = 0.5f;
    [SerializeField] private float leftStartOffsetX = -300f;
    [SerializeField] private float rightStartOffsetX = 300f;
    [SerializeField] private float introDelay = 0.1f;

    [Header("완료 게이트 (Next 버튼용)")]
    [SerializeField] private StepCompletionGate completionGate;

    // ===== 베이스에 값 주입용 프로퍼티 구현 =====

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
}
