using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스테이지 시작 화면 (스텝 타이틀 박스)
/// - 타이틀: 101000054 + stageIndex
/// - 설명:   101000064 + stageIndex
/// - 시작하기 버튼 → StepFlowController.NextStep()
/// </summary>
public class StartStep : ProblemStepBase
{
    [Header("UI")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text descriptionText;

    private const int TitleBaseId = 101000054;
    private const int DescBaseId = 101000064;

    protected override void OnStepEnter()
    {
        int stageIndex = ProblemSession.CurrentProblemIndex;
        if (stageIndex <= 0) return;

        if (titleText != null)
            titleText.text = ProblemRuntime.L(TitleBaseId + stageIndex);

        if (descriptionText != null)
            descriptionText.text = ProblemRuntime.L(DescBaseId + stageIndex);
    }
}
