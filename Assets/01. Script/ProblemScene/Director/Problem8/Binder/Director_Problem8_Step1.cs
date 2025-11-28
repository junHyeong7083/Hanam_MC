using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Director / Problem8 / Step1
/// - 인스펙터에서 UI 참조를 갖고 있는 Binder.
/// - 실제 로직은 Director_Problem8_Step1_Logic(부모)에 있음.
/// </summary>
public class Director_Problem8_Step1 : Director_Problem8_Step1_Logic
{
    [Header("===== 스토리보드 버튼 =====")]
    [SerializeField] private Button storyboardButton;

    [Header("===== 완료 게이트 =====")]
    [SerializeField] private StepCompletionGate completionGate;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override Button StoryboardButton => storyboardButton;
    protected override StepCompletionGate CompletionGateRef => completionGate;
}
