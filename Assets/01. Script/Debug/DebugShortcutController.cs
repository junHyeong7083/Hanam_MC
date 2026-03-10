using UnityEngine;

/// <summary>
/// 기획 테스트용 단축키 컨트롤러.
/// 빌드 시 삭제할 것.
/// - ESC       : 종료 (스테이지 선택으로)
/// - Backspace : 이전 스텝
/// - Enter     : 대사 넘김
/// </summary>
public class DebugShortcutController : MonoBehaviour
{
    private ProblemSceneController _sceneController;
    private DebugStepController _stepController;

    private void OnEnable()
    {
        _sceneController = FindAnyObjectByType<ProblemSceneController>();
        _stepController = FindAnyObjectByType<DebugStepController>();
    }

    private void Update()
    {
        // ESC: 종료 → 스테이지 선택
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (_sceneController != null)
                _sceneController.GoToStageSelect();
        }

        // Backspace: 이전 스텝
        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            if (_stepController != null)
                _stepController.PrevStep();
        }

        // Enter: 대사 넘김
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            var sequencer = FindActiveDialogueSequencer();
            if (sequencer != null)
                sequencer.AdvanceNext();
        }
    }

    private DialogueSequencer FindActiveDialogueSequencer()
    {
        var sequencers = FindObjectsByType<DialogueSequencer>(FindObjectsSortMode.None);
        foreach (var s in sequencers)
        {
            if (s.isActiveAndEnabled)
                return s;
        }
        return null;
    }
}
