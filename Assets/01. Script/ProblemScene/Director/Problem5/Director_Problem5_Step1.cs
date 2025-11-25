using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Part5 / Step1
/// - 인벤토리의 '줌 렌즈'를 모니터 위로 드래그해서 놓으면
///   => 모니터 연출 + 다음 스텝으로 넘어가는 Step.
/// - InventoryDropTargetStepBase 날먹 버전.
/// </summary>
public class Director_Problem5_Step1 : InventoryDropTargetStepBase
{
    [Header("드롭 타겟 (모니터)")]
    [SerializeField] private RectTransform dropTargetRect;      // 모니터 전체 Frame Rect
    [SerializeField] private RectTransform targetVisualRoot;    // 스케일 연출 줄 루트 (모니터 전체)

    [Header("드롭 인디케이터")]
    [SerializeField] private GameObject dropIndicatorRoot;      // TS의 dashed border 영역 느낌

    [Header("안내 텍스트/패널 루트")]
    [SerializeField] private GameObject instructionRoot;        // "줌 렌즈를 모니터 위에 올려주세요" 말풍선/텍스트

    [Header("표정/장면 전환 루트")]
    [SerializeField] private GameObject closeUpRoot;            // 😠 클로즈업만 보이는 루트
    [SerializeField] private GameObject zoomOutRoot;            // 상황 전체가 보이는 루트

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    // ================================
    // InventoryDropTargetStepBase Override
    // ================================
    protected override RectTransform DropTargetRect => dropTargetRect;
    protected override GameObject DropIndicatorRoot => dropIndicatorRoot;
    protected override RectTransform TargetVisualRoot => targetVisualRoot;
    protected override GameObject InstructionRoot => instructionRoot;
    protected override StepCompletionGate CompletionGate => completionGate;

    protected override float DropRadius => 250f;

    protected override float ActivateScale => 1.08f;

    protected override float ActivateDuration => 2.0f;
    protected override float DelayBeforeComplete => 0.5f;
}
