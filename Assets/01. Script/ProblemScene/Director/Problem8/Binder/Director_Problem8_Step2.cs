using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem8 / Step2
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem8_Step2_Logic(부모)에 있음.
/// </summary>
public class Director_Problem8_Step2 : Director_Problem8_Step2_Logic
{
    [Header("===== 캐러셀 UI =====")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;
    [SerializeField] private Image cardDisplayImage;
    [SerializeField] private CanvasGroup cardDisplayCanvasGroup;

    [Header("===== 드래그 프록시 =====")]
    [SerializeField] private RectTransform dragProxy;
    [SerializeField] private Image dragProxyImage;
    [SerializeField] private Canvas dragCanvas;

    [Header("===== 카드 데이터 =====")]
    [SerializeField] private SceneCardItem[] sceneCards;

    [Header("===== 슬롯들 =====")]
    [SerializeField] private SlotItem[] slots;

    [Header("===== 가이드 텍스트 =====")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextId_Main;
    [SerializeField] private int guideTextId_Fail;
    [SerializeField] private int guideTextId_Success;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override Button PrevButton => prevButton;
    protected override Button NextButton => nextButton;
    protected override Image CardDisplayImage => cardDisplayImage;
    protected override CanvasGroup CardDisplayCanvasGroup => cardDisplayCanvasGroup;

    protected override RectTransform DragProxy => dragProxy;
    protected override Image DragProxyImage => dragProxyImage;
    protected override Canvas DragCanvas => dragCanvas;

    protected override SceneCardItem[] SceneCards => sceneCards;
    protected override SlotItem[] Slots => slots;

    protected override Text GuideText => guideText;
    protected override int GuideTextId_Main => guideTextId_Main;
    protected override int GuideTextId_Fail => guideTextId_Fail;
    protected override int GuideTextId_Success => guideTextId_Success;
}
