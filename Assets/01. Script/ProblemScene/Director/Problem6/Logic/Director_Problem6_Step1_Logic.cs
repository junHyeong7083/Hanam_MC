using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Director / Problem6 / Step1 로직 베이스
/// - 인벤토리에서 '휴식용 의자'를 빈 박스 영역에 드롭하면
///   박스 내 아이콘이 빈 상태에서 의자 아이콘으로 바뀌고,
///   완료 후 Gate 완료.
/// </summary>
public abstract class Director_Problem6_Step1_Logic : InventoryDropTargetStepBase
{
    // ----- 파생 클래스에서 넘겨줄 UI 프로퍼티 -----
    protected abstract RectTransform ChairDropTargetRect { get; }
    protected abstract GameObject ChairDropIndicatorRoot { get; }
    protected abstract RectTransform ChairTargetVisualRoot { get; }
    protected abstract GameObject InstructionRootObject { get; }
    protected abstract StepCompletionGate StepCompletionGateRef { get; }

    protected abstract GameObject ChairPlacedIconRoot { get; }         // 의자 아이콘 (드롭 완료 시 활성화)
    protected abstract GameObject GlowImage { get; }                   // 글로우 이미지 (드롭 완료 시 활성화)
    protected abstract GameObject SparkleImage { get; }                // 스파클 이미지 (드롭 완료 시 활성화)

    // InventoryDropTargetStepBase 속성 연결
    protected override RectTransform DropTargetRect => ChairDropTargetRect;
    protected override GameObject DropIndicatorRoot => ChairDropIndicatorRoot;
    protected override RectTransform TargetVisualRoot => ChairTargetVisualRoot;
    protected override GameObject InstructionRoot => InstructionRootObject;
    protected override StepCompletionGate CompletionGate => StepCompletionGateRef;

    // TS 기준 설정
    protected override float DropRadius => 250f;      // distance < 250
    protected override float ActivateScale => 1.02f;  // 살짝 튀어오름
    protected override float ActivateDuration => 0.5f;
    protected override float DelayBeforeComplete => 2.5f; // setTimeout 2500ms

    /// <summary>
    /// 스텝 진입 시: 기본 상태 세팅
    /// </summary>
    protected override void OnStepEnterExtra()
    {
        // 드롭 인디케이터 숨김 (확실히)
        if (ChairDropIndicatorRoot != null)
            ChairDropIndicatorRoot.SetActive(false);

        // 드롭 완료 시 등장할 것들 숨김
        if (ChairPlacedIconRoot != null)
            ChairPlacedIconRoot.SetActive(false);
        if (GlowImage != null)
            GlowImage.SetActive(false);
        if (SparkleImage != null)
            SparkleImage.SetActive(false);
    }

    /// <summary>
    /// 드롭 성공 시: 인벤토리 아이템 처리 + 박스 내 아이콘 교체
    /// </summary>
    protected override void OnDropSuccess(StepInventoryItem item, PointerEventData eventData)
    {
        // 1) 기본 처리 (활성화 코루틴)
        base.OnDropSuccess(item, eventData);

        // 2) 드롭 완료 시 3개 등장: chairPlacedIconRoot, glowImage, sparkleImage
        if (ChairPlacedIconRoot != null)
            ChairPlacedIconRoot.SetActive(true);
        if (GlowImage != null)
            GlowImage.SetActive(true);
        if (SparkleImage != null)
            SparkleImage.SetActive(true);
    }

}
