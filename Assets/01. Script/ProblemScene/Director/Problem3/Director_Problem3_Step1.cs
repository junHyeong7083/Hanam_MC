using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;

/// <summary>
/// Director / Problem_3 / Step1
/// - 인벤토리에서 '시나리오 펜'을 드래그해서 책 위로 올리는 단계.
/// - 공통 로직은 InventoryDropTargetStepBase에서 처리.
/// </summary>
public class Director_Problem3_Step1 : InventoryDropTargetStepBase
{
    [Header("책 드롭 타겟")]
    [SerializeField] private RectTransform bookDropArea;
    [SerializeField] private GameObject dropIndicatorRoot;
    [SerializeField] private float dropRadius = 200f;

    [Header("책 활성화 연출")]
    [SerializeField] private RectTransform bookVisualRoot;
    [SerializeField] private float activateScale = 1.05f;
    [SerializeField] private float activateDuration = 0.6f;
    [SerializeField] private float delayBeforeComplete = 1.5f;

    [Header("안내 텍스트 / 기타 루트")]
    [SerializeField] private GameObject instructionRoot;

    [Header("완료 게이트 (옵션)")]
    [SerializeField] private StepCompletionGate completionGate;

    // === 베이스에 넘겨줄 프로퍼티들 ===
    protected override RectTransform DropTargetRect => bookDropArea;
    protected override GameObject DropIndicatorRoot => dropIndicatorRoot;
    protected override RectTransform TargetVisualRoot => bookVisualRoot;
    protected override GameObject InstructionRoot => instructionRoot;
    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float DropRadius => dropRadius;
    protected override float ActivateScale => activateScale;
    protected override float ActivateDuration => activateDuration;
    protected override float DelayBeforeComplete => delayBeforeComplete;

}
