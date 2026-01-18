using UnityEngine;

/// <summary>
/// 테스트용 Step 컨트롤러
/// - 현재 활성화된 StepFlowController를 자동으로 찾아서 사용
/// - 버튼 OnClick에 NextStep/PrevStep 연결
/// </summary>
public class DebugStepController : MonoBehaviour
{
    private StepFlowController _cachedController;

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
}
