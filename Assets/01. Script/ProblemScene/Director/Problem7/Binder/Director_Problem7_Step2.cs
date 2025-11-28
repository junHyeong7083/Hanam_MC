using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem7 / Step2
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem7_Step2_Logic(부모)에 있음.
/// </summary>
public class Director_Problem7_Step2 : Director_Problem7_Step2_Logic
{
    [Header("===== Intro 화면 =====")]
    [SerializeField] private GameObject introRoot;
    [SerializeField] private Button introNextButton;

    [Header("===== 가면 선택 화면 =====")]
    [SerializeField] private GameObject selectMaskRoot;
    [Tooltip("4개 가면: id/label/button 설정")]
    [SerializeField] private ChoiceItem[] maskChoices;

    [Header("===== 진짜 마음 선택 화면 =====")]
    [SerializeField] private GameObject selectFeelingRoot;
    [Tooltip("4개 감정: id/label/button 설정")]
    [SerializeField] private ChoiceItem[] feelingChoices;

    [Header("===== 완료 게이트 (CompleteRoot에 Reveal 화면 연결) =====")]
    [SerializeField] private StepCompletionGate completionGate;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override GameObject IntroRoot => introRoot;
    protected override Button IntroNextButton => introNextButton;

    protected override GameObject SelectMaskRoot => selectMaskRoot;
    protected override ChoiceItem[] MaskChoices => maskChoices;

    protected override GameObject SelectFeelingRoot => selectFeelingRoot;
    protected override ChoiceItem[] FeelingChoices => feelingChoices;

    protected override StepCompletionGate CompletionGateRef => completionGate;
}
