using UnityEngine;

/// <summary>
/// DebugShortcutController - 기획 테스트용 키보드 단축키 컨트롤러
///
/// 【역할】 개발/기획 테스트 중 물리 키보드 단축키로 빠르게 동작을 실행한다.
///         - ESC: 스테이지 선택 화면으로 복귀
///         - Backspace: 이전 스텝으로 이동
///         - Enter: 현재 활성화된 DialogueSequencer의 대사 넘기기
///         ※ 빌드 배포 시 제거해야 함 (키오스크 환경에서는 물리 키보드 없음)
/// 【씬】 ProblemScene (문제 풀이 화면)
/// 【참조하는 곳】 ProblemScene에 부착하여 독립적으로 동작
/// 【참조되는 곳】 ProblemSceneController (ESC→씬 이동), DebugStepController (Backspace→이전 스텝),
///               DialogueSequencer (Enter→대사 넘기기)
/// 【흐름】 Update() → 키 입력 감지 → 해당 동작 실행
/// </summary>
public class DebugShortcutController : MonoBehaviour
{
    private ProblemSceneController _sceneController;  // ProblemScene 전체 제어용 (ESC → 씬 이동)
    private DebugStepController _stepController;       // 스텝 디버그 제어용 (Backspace → 이전 스텝)

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

    /// <summary>현재 활성화된 DialogueSequencer를 씬에서 찾아 반환한다 (없으면 null)</summary>
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
