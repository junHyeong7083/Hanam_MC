using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director_Problem8_Step2 - 문제8 스텝2 Binder (concrete 클래스)
///
/// 【역할】 인스펙터에서 캐러셀 UI, 드래그 프록시, 씬 카드, 슬롯, 가이드 텍스트의 참조를 바인딩한다.
/// 【패턴】 Binder/Logic 패턴의 Binder 계층.
/// 【문제/스텝】 Director 테마 > 문제8 > 스텝2 (메인 활동 - 스토리보드 드래그&드롭)
/// 【부모 클래스】 Director_Problem8_Step2_Logic → ProblemStepBase
/// 【참조하는 곳】 씬의 Problem8 Step2 GameObject에 부착
/// 【참조되는 곳】 Director_Problem8_Step2_Logic (스토리보드 로직)
/// </summary>
public class Director_Problem8_Step2 : Director_Problem8_Step2_Logic
{
    [Header("===== 캐러셀 UI =====")]
    [SerializeField] private Button prevButton;                    // 이전 카드 버튼
    [SerializeField] private Button nextButton;                    // 다음 카드 버튼
    [SerializeField] private Image cardDisplayImage;               // 현재 카드 표시 이미지
    [SerializeField] private CanvasGroup cardDisplayCanvasGroup;   // 드래그 시 고스트 알파 적용용

    [Header("===== 드래그 프록시 =====")]
    [SerializeField] private RectTransform dragProxy;              // 드래그 중 따라다니는 프록시
    [SerializeField] private Image dragProxyImage;                 // 프록시 이미지
    [SerializeField] private Canvas dragCanvas;                    // 프록시의 부모 캔버스

    [Header("===== 카드 데이터 =====")]
    [SerializeField] private SceneCardItem[] sceneCards;           // 씬 카드 배열 (5장)

    [Header("===== 슬롯들 =====")]
    [SerializeField] private SlotItem[] slots;                     // 스토리보드 슬롯 배열 (5칸)

    [Header("===== 가이드 텍스트 =====")]
    [SerializeField] private Text guideText;                       // 가이드 텍스트 UI
    [SerializeField] private int guideTextId_Main;                 // 메인 안내 textId
    [SerializeField] private int guideTextId_Fail;                 // 오답 실패 textId
    [SerializeField] private int guideTextId_Success;              // 전체 성공 textId

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
