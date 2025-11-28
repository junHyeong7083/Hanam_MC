using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Director / Problem7 / Step1 로직 베이스
/// - 인벤토리에서 '격려의 메가폰'을 NG 모니터 근처로 드래그하면
///   NG 모니터가 숨겨지고, CompletionGate의 CompleteRoot(활성화 패널)가 표시됨.
/// </summary>
public abstract class Director_Problem7_Step1_Logic : InventoryDropTargetStepBase
{
    // ----- 파생 클래스(Binder)에서 넘겨줄 UI 참조 -----
    protected abstract RectTransform MegaphoneDropTargetRect { get; }
    protected abstract GameObject MegaphoneDropIndicatorRoot { get; }
    protected abstract RectTransform MegaphoneTargetVisualRoot { get; }
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    protected abstract GameObject NGMonitorRoot { get; }  // NG 모니터 (드롭 전)
    // ActivatedPanel은 StepCompletionGate의 CompleteRoot로 사용

    // InventoryDropTargetStepBase 에 연결
    protected override RectTransform DropTargetRect => MegaphoneDropTargetRect;
    protected override GameObject DropIndicatorRoot => MegaphoneDropIndicatorRoot;
    protected override RectTransform TargetVisualRoot => MegaphoneTargetVisualRoot;
    protected override GameObject InstructionRoot => null;  // 안내 텍스트 사용 안함
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    // 드롭 설정 값
    protected override float DropRadius => 250f;
    protected override float ActivateScale => 1.05f;
    protected override float ActivateDuration => 0.5f;
    protected override float DelayBeforeComplete => 2.0f;

    /// <summary>
    /// 스텝 진입 시: 기본 상태 셋업
    /// </summary>
    protected override void OnStepEnterExtra()
    {
        // 베이스에서 indicator/instruction/scale 초기화는 이미 처리.
        if (NGMonitorRoot != null) NGMonitorRoot.SetActive(true);
        // ActivatedPanel(CompleteRoot)은 Gate.ResetGate()에서 자동으로 숨겨짐
    }

    /// <summary>
    /// 드롭 성공 시: NG 모니터 숨기기 (활성화 패널은 Gate에서 자동 표시)
    /// </summary>
    protected override void OnDropSuccess(StepInventoryItem item, PointerEventData eventData)
    {
        // 1) 기본 처리 (활성화 코루틴 → MarkOneDone 호출 → CompleteRoot 표시)
        base.OnDropSuccess(item, eventData);

        // 2) NG 모니터 숨기기
        if (NGMonitorRoot != null) NGMonitorRoot.SetActive(false);
    }
}
