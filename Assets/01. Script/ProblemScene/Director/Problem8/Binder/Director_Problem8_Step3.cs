using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem8 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem8_Step3_Logic(부모)에 있음.
/// </summary>
public class Director_Problem8_Step3 : Director_Problem8_Step3_Logic
{
    [Header("===== 액션 카드 =====")]
    [SerializeField] private ActionItem[] actionChoices;

    [Header("===== 마이크 =====")]
    [SerializeField] private Button micButton;
    [SerializeField] private MicRecordingIndicator micIndicator;

    [Header("===== 가이드 텍스트 =====")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextId_Main;
    [SerializeField] private int guideTextId_Fail;
    [SerializeField] private int guideTextId_Success;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override ActionItem[] ActionChoices => actionChoices;
    protected override Button MicButton => micButton;
    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override Text GuideText => guideText;
    protected override int GuideTextId_Main => guideTextId_Main;
    protected override int GuideTextId_Fail => guideTextId_Fail;
    protected override int GuideTextId_Success => guideTextId_Success;
}
