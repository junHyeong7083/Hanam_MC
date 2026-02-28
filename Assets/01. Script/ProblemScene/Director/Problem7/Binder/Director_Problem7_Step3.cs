using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem7 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem7_Step3_Logic(부모)에 있음.
/// </summary>
public class Director_Problem7_Step3 : Director_Problem7_Step3_Logic
{
    [Header("===== HanamBox 가이드 텍스트 =====")]
    [SerializeField] private Text guideText;
    [SerializeField] private int guideTextId_Select;
    [SerializeField] private int guideTextId_Complete;
    [SerializeField] private int guideTextId_Retry;

    [Header("===== 대사 선택 화면 =====")]
    [SerializeField] private GameObject selectDialogueRoot;
    [Tooltip("3개 대사: id/textId/button/selectImg 설정")]
    [SerializeField] private DialogueItem[] dialogueChoices;

    [Header("===== 마이크 STT =====")]
    [SerializeField] private MicRecordingIndicator micIndicator;
    [SerializeField] private GameObject micButtonRoot;

    [Header("===== 완료 후 NextStep 버튼 =====")]
    [SerializeField] private GameObject nextStepButtonRoot;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override Text GuideText => guideText;
    protected override int GuideTextId_Select => guideTextId_Select;
    protected override int GuideTextId_Complete => guideTextId_Complete;
    protected override int GuideTextId_Retry => guideTextId_Retry;

    protected override GameObject SelectDialogueRoot => selectDialogueRoot;
    protected override DialogueItem[] DialogueChoices => dialogueChoices;

    protected override MicRecordingIndicator MicIndicator => micIndicator;
    protected override GameObject MicButtonRoot => micButtonRoot;

    protected override GameObject NextStepButtonRoot => nextStepButtonRoot;
}
