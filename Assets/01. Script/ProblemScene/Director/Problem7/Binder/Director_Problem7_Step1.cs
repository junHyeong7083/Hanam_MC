using UnityEngine;

/// <summary>
/// Director / Problem7 / Step1
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem7_Step1_Logic(부모)에 있음.
/// </summary>
public class Director_Problem7_Step1 : Director_Problem7_Step1_Logic
{
    [Header("활성화 연출용 비주얼 루트")]
    [SerializeField] private RectTransform megaphoneTargetVisualRoot;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    protected override RectTransform MegaphoneTargetVisualRoot => megaphoneTargetVisualRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
}
