using System.Collections;
using UnityEngine;

/// <summary>
/// Part5 / Step1
/// - 인벤토리의 '줌 렌즈'를 모니터 위로 드래그해서 놓으면
///   => closeUpRoot 팝업 연출 + 다음 스텝으로 넘어가는 Step.
/// </summary>
public class Director_Problem5_Step1 : InventoryDropTargetStepBase
{
    [Header("드롭 타겟 (모니터)")]
    [SerializeField] private RectTransform dropTargetRect;
    [SerializeField] private RectTransform targetVisualRoot;

    [Header("드롭 인디케이터")]
    [SerializeField] private GameObject dropIndicatorRoot;

    [Header("안내 텍스트/패널 루트")]
    [SerializeField] private GameObject instructionRoot;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("이펙트 컨트롤러")]
    [SerializeField] private Problem5_Step1_EffectController effectController;

    // ================================
    // InventoryDropTargetStepBase Override
    // ================================
    protected override RectTransform DropTargetRect => dropTargetRect;
    protected override GameObject DropIndicatorRoot => dropIndicatorRoot;
    protected override RectTransform TargetVisualRoot => targetVisualRoot;
    protected override GameObject InstructionRoot => instructionRoot;
    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float DropRadius => 250f;
    protected override float ActivateScale => 1.0f;
    protected override float ActivateDuration => 0f;  // 기본 애니메이션 비활성화
    protected override float DelayBeforeComplete => 2.0f;  // 2초 후 다음 스텝

    private bool _animationComplete;

    protected override void OnStepEnterExtra()
    {
        // EffectController 초기화
        if (effectController != null)
            effectController.ResetToInitial();
    }

    protected override IEnumerator PlayActivateAnimation()
    {
        if (effectController == null)
            yield break;

        _animationComplete = false;

        // EffectController에서 클로즈업 팝업 재생
        effectController.PlayCloseUpPopup(() => _animationComplete = true);

        // 애니메이션 완료 대기
        while (!_animationComplete)
            yield return null;
    }
}
