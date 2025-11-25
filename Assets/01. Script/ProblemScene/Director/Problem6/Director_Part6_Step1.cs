using UnityEngine;

public class Director_Part6_Step1 : InventoryDropTargetStepBase
{
    [Header("드롭 타겟 / 연출")]
    [SerializeField] private RectTransform dropTargetRect;     // 의자 놓일 빈 공간 Rect
    [SerializeField] private RectTransform targetVisualRoot;   // 실제 의자/배경 비주얼 루트
    [SerializeField] private GameObject dropIndicatorRoot;     // 드래그 중 하이라이트 박스
    [SerializeField] private GameObject instructionRoot;       // "의자를 여기로 드래그해 주세요" 안내 루트

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    [Header("파라미터")]
    [SerializeField] private float dropRadius = 250f;          // 드롭 성공 반경 (화면 기준)
    [SerializeField] private float activateScale = 1.05f;      // 살짝 커지는 정도
    [SerializeField] private float delayBeforeComplete = 1.5f; // 연출 후 Gate까지 대기 시간

    [Header("성공 연출 (옵션)")]
    [SerializeField] private GameObject chairPlacedFxRoot;     // 스파클 / 글로우 등 성공 시 켜줄 오브젝트

    // === InventoryDropTargetStepBase 추상 프로퍼티 구현 ===
    protected override RectTransform DropTargetRect => dropTargetRect;
    protected override GameObject DropIndicatorRoot => dropIndicatorRoot;
    protected override RectTransform TargetVisualRoot => targetVisualRoot;
    protected override GameObject InstructionRoot => instructionRoot;
    protected override StepCompletionGate CompletionGate => completionGate;

    // === 파라미터 오버라이드 ===
    protected override float DropRadius => dropRadius;
    protected override float ActivateScale => activateScale;
    protected override float ActivateDuration => 0.6f;
    protected override float DelayBeforeComplete => delayBeforeComplete;

    /// <summary>
    /// 드롭 성공 후 연출이 끝나기 직전에 호출됨.
    /// (InventoryDropTargetStepBase.HandleActivatedRoutine → OnDropComplete)
    /// </summary>
    protected override void OnDropComplete()
    {
        // 의자 성공 배치 FX 켜기
        if (chairPlacedFxRoot != null)
            chairPlacedFxRoot.SetActive(true);

        // 안내 텍스트는 더 이상 필요 없으니 끔
        if (instructionRoot != null)
            instructionRoot.SetActive(false);

        // 여기서 추가로 사운드, 대사 UI, etc.도 켤 수 있음
    }
}
