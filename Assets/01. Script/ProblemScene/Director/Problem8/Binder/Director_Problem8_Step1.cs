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

    [Header("===== 이펙트 컨트롤러 =====")]
    [SerializeField] private Problem8_Step1_EffectController effectController;

    // ----- 부모 추상 프로퍼티 구현 -----
    protected override Button StoryboardButton => storyboardButton;
    protected override StepCompletionGate CompletionGateRef => completionGate;

    // ----- 시각 효과 연결 -----
    protected override void OnStepEnterVisual()
    {
        base.OnStepEnterVisual();

        // 인트로 애니메이션 중 버튼 비활성화
        if (storyboardButton != null)
            storyboardButton.interactable = false;

        if (effectController != null)
        {
            effectController.PlayIntroAnimation(() =>
            {
                // 애니메이션 완료 후 버튼 활성화
                if (storyboardButton != null)
                    storyboardButton.interactable = true;
            });
        }
        else
        {
            // effectController 없으면 바로 활성화
            if (storyboardButton != null)
                storyboardButton.interactable = true;
        }
    }

    protected override void OnStoryboardClickedVisual()
    {
        base.OnStoryboardClickedVisual();

        // ButtonHover 비활성화
        if (storyboardButton != null)
        {
            var buttonHover = storyboardButton.GetComponent<ButtonHover>();
            if (buttonHover != null)
                buttonHover.SetInteractable(false);
        }

        if (effectController != null)
        {
            effectController.PlayStoryboardFlip();
        }
    }
}
