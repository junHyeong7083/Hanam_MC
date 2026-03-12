using UnityEngine;

/// <summary>
/// DebugStepController - 개발/테스트용 스텝 이동 컨트롤러
///
/// 【역할】 개발 중 디버그 패널을 통해 스텝을 강제로 전진/후퇴시키는 도구.
///         - NextStep(): 다음 스텝으로 이동 (디버그 패널 버튼 OnClick 연결)
///         - PrevStep(): 이전 스텝으로 이동 (디버그 패널 버튼 OnClick 연결)
///         - F12 키로 디버그 패널 토글
///         현재 활성화된 StepFlowController를 자동으로 찾아서 사용한다.
/// 【씬】 ProblemScene (문제 풀이 화면)
/// 【참조하는 곳】 ProblemScene에 부착, 디버그 패널 버튼에서 호출
/// 【참조되는 곳】 StepFlowController (스텝 이동), DebugShortcutController (Backspace → PrevStep)
/// 【흐름】 F12 → 디버그 패널 토글 / 버튼 클릭 → NextStep()/PrevStep() → StepFlowController 제어
/// </summary>
public class DebugStepController : MonoBehaviour
{
    private StepFlowController _cachedController;      // 캐시된 StepFlowController (비활성화되면 재탐색)
    [SerializeField] private GameObject debugPanel;    // F12로 토글하는 디버그 패널 오브젝트
    private bool isPanelOpen = false;                  // 디버그 패널 현재 열림 상태
    /// <summary>
    /// 현재 활성화된 StepFlowController 찾기
    /// </summary>
    private StepFlowController FindCurrentController()
    {
        if (_cachedController != null && _cachedController.isActiveAndEnabled)
            return _cachedController;

        var controllers = FindObjectsByType<StepFlowController>(FindObjectsSortMode.None);
        foreach (var ctrl in controllers)
        {
            if (ctrl.isActiveAndEnabled)
            {
                _cachedController = ctrl;
                return ctrl;
            }
        }

        return null;
    }

    /// <summary>
    /// 다음 스텝으로 이동 - 버튼 OnClick에 연결
    /// </summary>
    public void NextStep()
    {
        var controller = FindCurrentController();
        if (controller != null)
        {
            controller.NextStep();
        }
    }



    /// <summary>
    /// 이전 스텝으로 이동 - 버튼 OnClick에 연결
    /// </summary>
    public void PrevStep()
    {
        var controller = FindCurrentController();
        if (controller != null)
        {
            controller.PrevStep();
        }
    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.F12))
        {
            debugPanel.SetActive(!isPanelOpen);
            isPanelOpen = !isPanelOpen;
        }
    }
}
