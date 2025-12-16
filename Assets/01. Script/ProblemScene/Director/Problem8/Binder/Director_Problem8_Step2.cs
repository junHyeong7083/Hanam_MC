using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem8 / Step2
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem8_Step2_Logic(부모)에 있음.
/// </summary>
public class Director_Problem8_Step2 : Director_Problem8_Step2_Logic
{
    [Header("===== 장면 카드들 =====")]
    [Tooltip("5개 카드: Ghost(알파0.5) + Draggable(알파1) 구조")]
    [SerializeField] private SceneCardItem[] sceneCards;

    [Header("===== 슬롯들 =====")]
    [Tooltip("5개 슬롯: 드롭 영역 + 빈 상태/채워진 상태 UI")]
    [SerializeField] private SlotItem[] slots;

    [Header("===== 카드 선택 영역 =====")]
    [Tooltip("모든 카드 배치 완료 시 숨겨지는 영역")]
    [SerializeField] private GameObject cardSelectionRoot;

    [Header("===== 드래그용 Canvas =====")]
    [Tooltip("드래그 중 카드가 최상위에 렌더링되도록 하는 Canvas")]
    [SerializeField] private Canvas dragCanvas;

    [Header("===== 완료 게이트 =====")]
    [SerializeField] private StepCompletionGate completionGate;
    [SerializeField] private GameObject fillImageRoot;
    [SerializeField] private Image fillImage;

    [Header("===== 선택 안내 텍스트 이미지 =====")]
    [SerializeField] private GameObject selectTextImage;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override SceneCardItem[] SceneCards => sceneCards;
    protected override SlotItem[] Slots => slots;
    protected override GameObject CardSelectionRoot => cardSelectionRoot;
    protected override Canvas DragCanvas => dragCanvas;
    protected override StepCompletionGate CompletionGateRef => completionGate;

    protected override GameObject FillImageRoot => fillImageRoot;
    protected override Image FillImage => fillImage;
    protected override GameObject SelectTextImage => selectTextImage;
}
