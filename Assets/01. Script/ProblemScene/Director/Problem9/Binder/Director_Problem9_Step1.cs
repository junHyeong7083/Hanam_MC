using UnityEngine;

/// <summary>
/// Director / Problem9 / Step1
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem9_Step1_Logic(부모)에 있음.
/// </summary>
public class Director_Problem9_Step1 : Director_Problem9_Step1_Logic
{
    [Header("스케일 애니메이션 대상")]
    [SerializeField] private RectTransform conflictVisualRoot;

    [Header("화면 루트 (활성화 시 숨김)")]
    [SerializeField] private GameObject introRoot;

    [Header("완료 게이트")]
    [SerializeField] private StepCompletionGate completionGate;

    protected override RectTransform ConflictVisualRoot => conflictVisualRoot;
    protected override GameObject IntroRoot => introRoot;
    protected override StepCompletionGate StepCompletionGateRef => completionGate;
}
