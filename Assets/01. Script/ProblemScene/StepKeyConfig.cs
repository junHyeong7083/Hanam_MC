// StepKeyConfig.cs
using System;
using UnityEngine;

[Serializable]
public struct StepKeyConfig
{
    [Tooltip("이 스텝이 몇 번째 스텝인지 / 어떤 스텝인지")]
    public StepId stepId;

    public string BuildKey(ProblemContext ctx)
    {
        // 1) Theme 결정
        ProblemTheme theme;
        theme = ctx.Theme;

        // 2) ProblemIndex는 무조건 Context에서만 가져간다
        int problemIndex = 1;
        if (ctx != null)
        {
            problemIndex = ctx.ProblemIndex;
        }

        if (problemIndex <= 0)
            problemIndex = 1;
        return $"{theme}_P{problemIndex}_Step{(int)stepId}";
    }
}
