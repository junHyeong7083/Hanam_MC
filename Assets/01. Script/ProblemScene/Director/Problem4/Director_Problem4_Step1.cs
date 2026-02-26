using System.Collections;
using UnityEngine;

/// <summary>
/// Director / Problem4 / Step1
/// - DB에서 가위 보유 확인 후 자동 활성화
/// - 이펙트는 EffectController에 위임
/// </summary>
public class Director_Problem4_Step1 : InventoryDropTargetStepBase
{
    [Header("완료 게이트 (옵션)")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem4_Step1_EffectController effectController;

    [Header("완료 후 딜레이")]
    [SerializeField] private float delayBeforeComplete = 1.5f;

    // === 베이스로 넘겨줄 프로퍼티들 ===
    protected override RectTransform TargetVisualRoot => null; // EffectController가 관리
    protected override GameObject InstructionRoot => null; // EffectController가 관리
    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float ActivateScale => 1f;
    protected override float ActivateDuration => 0f;
    protected override float DelayBeforeComplete => delayBeforeComplete;

    protected override void OnStepEnterExtra()
    {
        if (effectController != null)
            effectController.ResetForNextStep();
    }

    protected override IEnumerator PlayActivateAnimation()
    {
        if (effectController == null)
            yield break;

        bool complete = false;
        effectController.PlayActivateSequence(() => complete = true);

        while (!complete)
            yield return null;
    }

    protected override void OnStepExit()
    {
        base.OnStepExit();

        if (effectController != null)
        {
            effectController.HideDropIndicator();
            effectController.HideSparkle();
        }
    }
}
