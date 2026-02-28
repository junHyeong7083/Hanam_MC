using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem10 / Step2
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem10_Step2_Logic(부모)에 있음.
/// </summary>
public class Director_Problem10_Step2 : Director_Problem10_Step2_Logic
{
    [Header("===== 장르 데이터 =====")]
    [SerializeField] private GenreCardData[] genreCardsData;

    [Header("===== 하남박스 =====")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextId;
    [SerializeField] private int guideTextId_Success;
    [SerializeField] private GameObject nextStepButtonRoot;

    [Header("===== 선택 화면 =====")]
    [SerializeField] private GameObject selectRoot;
    [SerializeField] private Button[] genreButtons;
    [SerializeField] private GameObject[] selectIndicators;
    [SerializeField] private Text[] genreLabels;

    [Header("===== 완료 화면 =====")]
    [SerializeField] private GameObject completeRoot;
    [SerializeField] private Image completeCardImage;
    [SerializeField] private Text completeCardLabel;

    [Header("===== 공유 데이터 =====")]
    [SerializeField] private Problem10SharedData sharedData;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override GenreCardData[] GenreCardsData => genreCardsData;
    protected override Text GuideText => guideText;
    protected override int GuideTextId => guideTextId;
    protected override int GuideTextId_Success => guideTextId_Success;
    protected override GameObject NextStepButtonRoot => nextStepButtonRoot;
    protected override GameObject SelectRoot => selectRoot;
    protected override Button[] GenreButtons => genreButtons;
    protected override GameObject[] SelectIndicators => selectIndicators;
    protected override Text[] GenreLabels => genreLabels;
    protected override GameObject CompleteRoot => completeRoot;
    protected override Image CompleteCardImage => completeCardImage;
    protected override Text CompleteCardLabel => completeCardLabel;
    protected override Problem10SharedData SharedData => sharedData;
}
