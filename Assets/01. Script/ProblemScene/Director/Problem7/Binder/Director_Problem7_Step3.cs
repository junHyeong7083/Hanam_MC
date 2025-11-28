using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem7 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem7_Step3_Logic(부모)에 있음.
/// </summary>
public class Director_Problem7_Step3 : Director_Problem7_Step3_Logic
{
    [Header("===== Intro 화면 =====")]
    [SerializeField] private GameObject introRoot;
    [SerializeField] private Button introNextButton;

    [Header("===== 대사 선택 화면 =====")]
    [SerializeField] private GameObject selectDialogueRoot;
    [Tooltip("3개 대사: id/text/button 설정")]
    [SerializeField] private DialogueItem[] dialogueChoices;
    [SerializeField] private Button recordButton;

    [Header("===== 녹음 화면 =====")]
    [SerializeField] private GameObject recordingRoot;

    [Header("===== 완료 게이트 (CompleteRoot에 Result 화면 연결) =====")]
    [SerializeField] private StepCompletionGate completionGate;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override GameObject IntroRoot => introRoot;
    protected override Button IntroNextButton => introNextButton;

    protected override GameObject SelectDialogueRoot => selectDialogueRoot;
    protected override DialogueItem[] DialogueChoices => dialogueChoices;
    protected override Button RecordButton => recordButton;

    protected override GameObject RecordingRoot => recordingRoot;

    protected override StepCompletionGate CompletionGateRef => completionGate;
}
