using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Director / Problem6 / Step1 로직 베이스
/// - 인벤토리에서 '휴식용 감독 의자'를 드롭 박스 근처에 놓으면
///   박스 안 아이콘이 비어있던 상태에서 의자 아이콘으로 바뀌고,
///   잠시 후 Gate 완료.
/// </summary>
public abstract class Director_Problem6_Step1_Logic : InventoryDropTargetStepBase
{
    // ----- 파생 클래스(씬 래퍼)에서 넘겨줄 UI 참조들 -----
    protected abstract RectTransform ChairDropTargetRect { get; }
    protected abstract GameObject ChairDropIndicatorRoot { get; }
    protected abstract RectTransform ChairTargetVisualRoot { get; }
    protected abstract GameObject InstructionRootObject { get; }
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    protected abstract GameObject EmptyIconRoot { get; }        // "텅 빈 공간" 상태
    protected abstract GameObject ChairPlacedIconRoot { get; }  // 의자 + 스파클 상태

    // InventoryDropTargetStepBase 에 주입
    protected override RectTransform DropTargetRect => ChairDropTargetRect;
    protected override GameObject DropIndicatorRoot => ChairDropIndicatorRoot;
    protected override RectTransform TargetVisualRoot => ChairTargetVisualRoot;
    protected override GameObject InstructionRoot => InstructionRootObject;
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    // TS 기준 값들
    protected override float DropRadius => 250f;      // distance < 250
    protected override float ActivateScale => 1.02f;  // 살짝만 튀게
    protected override float ActivateDuration => 0.5f;
    protected override float DelayBeforeComplete => 2.5f; // setTimeout 2500ms

    /// <summary>
    /// 스텝 진입 시: 기본 상태 셋업
    /// </summary>
    protected override void OnStepEnterExtra()
    {
        // 베이스에서 indicator/instruction/scale 리셋은 이미 해줌.
        if (EmptyIconRoot != null) EmptyIconRoot.SetActive(true);
        if (ChairPlacedIconRoot != null) ChairPlacedIconRoot.SetActive(false);
    }

    /// <summary>
    /// 드롭 성공 시: 인벤토리 아이콘 처리 + 박스 안 아이콘 교체
    /// </summary>
    protected override void OnDropSuccess(StepInventoryItem item, PointerEventData eventData)
    {
        // 1) 기본 처리 ( 활성화 코루틴)
        base.OnDropSuccess(item, eventData);

        // 2) 박스 안 placeholder → 의자 아이콘으로 교체
        if (EmptyIconRoot != null) EmptyIconRoot.SetActive(false);
        if (ChairPlacedIconRoot != null) ChairPlacedIconRoot.SetActive(true);
        // 스파클/빛 효과는 ChairPlacedIconRoot 안에 넣고
        // OnEnable 시 애니메이션 재생되게 해두면 됨.
    }

}
