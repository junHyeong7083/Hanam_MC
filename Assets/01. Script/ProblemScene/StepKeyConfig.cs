// StepKeyConfig.cs
using System;
using UnityEngine;

/// <summary>
/// StepKeyConfig - DataService에 진행도를 저장할 때 사용하는 키 설정 구조체
///
/// 【역할】 각 스텝(Step)의 고유 식별 키를 생성한다.
///          Theme + ProblemIndex + StepId를 조합하여 "Director_P1_Step2" 형태의 문자열 키를 만든다.
/// 【참조하는 곳】 ProblemStepBase.BuildStepKey() → SaveAttempt() / SaveReward() 시 사용
/// 【참조되는 곳】 ProblemContext (Theme, ProblemIndex 제공), StepId enum
/// 【흐름】 인스펙터에서 stepId 설정 → BuildKey(ctx) 호출 시 "Director_P1_Step2" 형태 반환
/// </summary>
[Serializable]
public struct StepKeyConfig
{
    [Tooltip("이 스텝이 몇 번째 스텝인지 / 어떤 스텝인지 지정하는 enum")]
    public StepId stepId; // 스텝 식별용 enum 값 (예: Step1, Step2, Step3 등)

    /// <summary>
    /// ProblemContext의 테마와 문제 번호, 그리고 이 구조체의 stepId를 조합하여
    /// DB 저장용 고유 키 문자열을 생성한다.
    /// </summary>
    /// <param name="ctx">현재 문제의 ProblemContext (Theme, ProblemIndex 참조)</param>
    /// <returns>"Director_P1_Step2" 형태의 키 문자열</returns>
    public string BuildKey(ProblemContext ctx)
    {
        // 1) Theme 결정
        ProblemTheme theme;
        theme = ctx.Theme;

        // 2) ProblemIndex는 가능한 Context에서부터 가져오기
        int problemIndex = 1;
        if (ctx != null)
        {
            problemIndex = ctx.ProblemIndex;
        }

        if (problemIndex <= 0)
            problemIndex = 1;

        // 결과 예: "Director_P1_Step2", "Gardener_P3_Step1"
        return $"{theme}_P{problemIndex}_Step{(int)stepId}";
    }
}
