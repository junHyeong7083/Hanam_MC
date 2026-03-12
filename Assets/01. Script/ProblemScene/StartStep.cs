using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// StartStep - 문제 시작 시 첫 진입 화면 (스텝 타이틀 박스)
///
/// 【역할】 문제에 진입하면 가장 먼저 보여지는 인트로 화면.
///          현재 문제 번호(stageIndex)에 맞는 타이틀과 설명 텍스트를 CSV DataTable에서 가져와 표시한다.
///          "시작하기" 버튼을 누르면 StepFlowController.NextStep()으로 실제 문제 스텝으로 이동한다.
/// 【참조하는 곳】 StepFlowController의 stepPanels[0]에 배치 (가장 첫 번째 스텝)
/// 【참조되는 곳】 ProblemRuntime.L() (텍스트 조회), ProblemSession (문제 번호)
/// 【흐름】 OnStepEnter() → stageIndex 기반 textId 계산 → 타이틀/설명 표시 → 사용자가 "시작하기" 클릭 → NextStep()
///
/// ※ 텍스트 ID 규칙:
///   - 타이틀: 101000054 + stageIndex (예: 문제1 → 101000055)
///   - 설명:   101000064 + stageIndex (예: 문제1 → 101000065)
/// </summary>
public class StartStep : ProblemStepBase
{
    [Header("UI")]
    [SerializeField] private Text titleText;       // 문제 타이틀 텍스트 UI
    [SerializeField] private Text descriptionText;  // 문제 설명 텍스트 UI

    private const int TitleBaseId = 101000054;  // 타이틀 textId 기준값 (+ stageIndex)
    private const int DescBaseId = 101000064;   // 설명 textId 기준값 (+ stageIndex)

    /// <summary>
    /// 스텝 진입 시 호출. ProblemSession에서 현재 문제 번호를 가져와
    /// 타이틀과 설명 텍스트를 DataTable에서 읽어 표시한다.
    /// </summary>
    protected override void OnStepEnter()
    {
        int stageIndex = ProblemSession.CurrentProblemIndex; // 현재 문제 번호 (1~10)
        if (stageIndex <= 0) return;

        if (titleText != null)
            titleText.text = ProblemRuntime.L(TitleBaseId + stageIndex);

        if (descriptionText != null)
            descriptionText.text = ProblemRuntime.L(DescBaseId + stageIndex);
    }
}
