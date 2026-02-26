using System.Collections;
using UnityEngine;

/// <summary>
/// Part5 / Step1
/// - DB에서 줌 렌즈 보유 확인 후 자동 활성화
/// - closeUpRoot 팝업 연출 후 다음 스텝으로 넘어감
/// </summary>
public class Director_Problem5_Step1 : InventoryDropTargetStepBase
{
    [Header("비주얼")]
    [SerializeField] private RectTransform targetVisualRoot;

    [Header("안내 텍스트/패널 루트")]
    [SerializeField] private GameObject instructionRoot;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem5_Step1_EffectController effectController;

    // === Base Override ===
    protected override RectTransform TargetVisualRoot => targetVisualRoot;
    protected override GameObject InstructionRoot => instructionRoot;
    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float ActivateScale => 1.0f;
    protected override float ActivateDuration => 0f;
    protected override float DelayBeforeComplete => 2.0f;

    private bool _animationComplete;

    protected override void OnStepEnterExtra()
    {
        if (effectController != null)
            effectController.ResetToInitial();
    }

    protected override IEnumerator PlayActivateAnimation()
    {
        if (effectController == null)
            yield break;

        _animationComplete = false;
        effectController.PlayCloseUpPopup(() => _animationComplete = true);

        while (!_animationComplete)
            yield return null;
    }
}
