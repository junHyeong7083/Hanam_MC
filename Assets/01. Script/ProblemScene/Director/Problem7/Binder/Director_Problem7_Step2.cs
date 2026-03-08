using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem7 / Step2
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem7_Step2_Logic(부모)에 있음.
/// </summary>
public class Director_Problem7_Step2 : Director_Problem7_Step2_Logic
{
    [Header("===== HanamBox 가이드 텍스트 =====")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextId_SelectMask;
    [SerializeField] private int guideTextId_SelectFeeling;
    [SerializeField] private int guideTextId_Complete;

    [Header("===== 가면 선택 화면 =====")]
    [SerializeField] private GameObject selectMaskRoot;
    [Tooltip("4개 가면: id/labelTextId/button 설정")]
    [SerializeField] private ChoiceItem[] maskChoices;

    [Header("===== 진짜 마음 선택 화면 =====")]
    [SerializeField] private GameObject selectFeelingRoot;
    [Tooltip("4개 감정: id/labelTextId/button 설정")]
    [SerializeField] private ChoiceItem[] feelingChoices;

    [Header("===== 전환 딜레이 (초) =====")]
    [SerializeField] private float maskSelectDelay = 2.0f;
    [SerializeField] private float feelingSelectDelay = 2.0f;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override Text GuideText => guideText;
    protected override int GuideTextId_SelectMask => guideTextId_SelectMask;
    protected override int GuideTextId_SelectFeeling => guideTextId_SelectFeeling;
    protected override int GuideTextId_Complete => guideTextId_Complete;

    protected override GameObject SelectMaskRoot => selectMaskRoot;
    protected override ChoiceItem[] MaskChoices => maskChoices;

    protected override GameObject SelectFeelingRoot => selectFeelingRoot;
    protected override ChoiceItem[] FeelingChoices => feelingChoices;

    protected override float MaskSelectDelay => maskSelectDelay;
    protected override float FeelingSelectDelay => feelingSelectDelay;
}
