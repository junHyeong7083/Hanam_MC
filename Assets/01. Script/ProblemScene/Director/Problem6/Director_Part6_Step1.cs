using UnityEngine;

/// <summary>
/// Director / Problem6 / Step1
/// - 인스펙터에서 UI 참조만 들고 있는 래퍼.
/// - 실제 로직은 Director_Problem6_Step1_Logic(부모)에 있음.
/// </summary>
public class Director_Problem6_Step1 : Director_Problem6_Step1_Logic
{
    [Header("의자 드롭 타겟 영역 (빈 공간 박스)")]
    [SerializeField] private RectTransform emptySpaceRect;        // TS의 emptySpaceRef

    [Header("드롭 인디케이터 (드래그 중 테두리 박스)")]
    [SerializeField] private GameObject dropIndicatorRoot;

    [Header("활성화 연출용 비주얼 루트 (스케일 튕길 대상)")]
    [SerializeField] private RectTransform chairTargetVisualRoot; // 박스 전체 or 안쪽 카드

    [Header("안내 텍스트 루트")]
    [SerializeField] private GameObject instructionRoot;          // "의자를 드래그해서 올려주세요"

    [Header("박스 안 아이콘 루트")]
    [SerializeField] private GameObject emptyIconRoot;            // 텅 빈 공간 아이콘 + 텍스트 묶음
    [SerializeField] private GameObject chairPlacedIconRoot;      // 의자 + 스파클 묶음 (초기 비활성)

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override RectTransform ChairDropTargetRect => emptySpaceRect;
    protected override GameObject ChairDropIndicatorRoot => dropIndicatorRoot;
    protected override RectTransform ChairTargetVisualRoot => chairTargetVisualRoot;
    protected override GameObject InstructionRootObject => instructionRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
    protected override GameObject EmptyIconRoot => emptyIconRoot;
    protected override GameObject ChairPlacedIconRoot => chairPlacedIconRoot;
}
