using UnityEngine;

/// <summary>
/// Director / Problem7 / Step1
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem7_Step1_Logic(부모)에 있음.
/// </summary>
public class Director_Problem7_Step1 : Director_Problem7_Step1_Logic
{
    [Header("드롭 대상 타깃 영역 (NG 모니터 박스)")]
    [SerializeField] private RectTransform megaphoneDropTargetRect;

    [Header("드롭 인디케이터 (드래그 중 테두리 박스)")]
    [SerializeField] private GameObject megaphoneDropIndicatorRoot;

    [Header("활성화 연출용 비주얼 루트 (스케일 튕김 대상)")]
    [SerializeField] private RectTransform megaphoneTargetVisualRoot;

    [Header("NG 모니터 루트 (드롭 전 표시)")]
    [SerializeField] private GameObject ngMonitorRoot;

    [Header("완료 게이트 (CompleteRoot에 활성화 패널 연결)")]
    [SerializeField] private StepCompletionGate completionGate;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override RectTransform MegaphoneDropTargetRect => megaphoneDropTargetRect;
    protected override GameObject MegaphoneDropIndicatorRoot => megaphoneDropIndicatorRoot;
    protected override RectTransform MegaphoneTargetVisualRoot => megaphoneTargetVisualRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
    protected override GameObject NGMonitorRoot => ngMonitorRoot;
}
