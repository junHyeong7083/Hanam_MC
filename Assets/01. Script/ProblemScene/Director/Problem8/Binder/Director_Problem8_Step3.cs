using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem8 / Step3
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem8_Step3_Logic(부모)에 있음.
/// </summary>
public class Director_Problem8_Step3 : Director_Problem8_Step3_Logic
{
    [Header("===== 액션 선택 화면 =====")]
    [SerializeField] private GameObject selectActionRoot;
    [SerializeField] private ActionItem[] actionChoices;

    [Header("===== 녹음 화면 =====")]
    [SerializeField] private GameObject recordingRoot;
    [SerializeField] private Button recordButton;

    [Header("===== 결과 화면 =====")]
    [SerializeField] private GameObject resultRoot;
    [SerializeField] private Text resultText;

    [Header("===== 완료 게이트 =====")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("===== 이펙트 컨트롤러 =====")]
    [SerializeField] private Problem8_Step3_EffectController effectController;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override GameObject SelectActionRoot => selectActionRoot;
    protected override ActionItem[] ActionChoices => actionChoices;
    protected override GameObject RecordingRoot => recordingRoot;
    protected override Button RecordButton => recordButton;
    protected override GameObject ResultRoot => resultRoot;
    protected override Text ResultText => resultText;
    protected override StepCompletionGate CompletionGateRef => completionGate;

    // ----- 시각 효과 연결 -----
    protected override void OnRecordingStarted()
    {
        base.OnRecordingStarted();

        // 녹음 시작 시 StatusArea 등장 + 선택한 액션 텍스트 표시
        if (effectController != null && SelectedAction != null)
        {
            effectController.StartRecordingAnimation(SelectedAction.text);
        }
    }

    protected override void OnRecordingEnded()
    {
        base.OnRecordingEnded();

        // 녹음 종료 시 녹음 애니메이션 정지 + 결과 화면 애니메이션
        if (effectController != null)
        {
            effectController.StopRecordingAnimation();
            effectController.PlayResultAnimation();
        }
    }
}
