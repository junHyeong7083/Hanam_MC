using System;
using UnityEngine;

/// <summary>
/// 스크립트/타입 이름 규칙:
///   예) Director_Problem1_Step3
///       Gardener_Problem2_Step1
/// 이런 이름에서
///   - ProblemTheme (Director / Gardener)
///   - ProblemIndex (1..N)
///   - StepIndex (1..N)
/// 를 파싱하는 공통 유틸.
/// </summary>
public static class ProblemMetaUtility
{
    /// <summary>
    /// MonoBehaviour 인스턴스 기준으로 메타 파싱.
    /// 예) ProblemMetaUtility.ResolveFromBehaviour(this, out theme, out pIdx, out sIdx);
    /// </summary>
    public static void ResolveFromBehaviour(
        MonoBehaviour behaviour,
        out ProblemTheme theme,
        out int problemIndex,
        out int stepIndex)
    {
        if (behaviour == null)
        {
            Debug.LogWarning("[ProblemMetaUtility] behaviour is null");
            theme = ProblemTheme.Director;
            problemIndex = 0;
            stepIndex = 0;
            return;
        }

        ResolveFromType(behaviour.GetType(), out theme, out problemIndex, out stepIndex);
    }

    /// <summary>
    /// Type 기준 파싱.
    /// 예) ProblemMetaUtility.ResolveFromType(typeof(Director_Problem1_Step3), ...)
    /// </summary>
    public static void ResolveFromType(
        Type type,
        out ProblemTheme theme,
        out int problemIndex,
        out int stepIndex)
    {
        if (type == null)
        {
            theme = ProblemTheme.Director;
            problemIndex = 0;
            stepIndex = 0;
            return;
        }

        ResolveFromTypeName(type.Name, out theme, out problemIndex, out stepIndex);
    }

    /// <summary>
    /// 클래스 이름 문자열에서 직접 파싱.
    /// 예) "Director_Problem1_Step3"
    /// </summary>
    public static void ResolveFromTypeName(
        string typeName,
        out ProblemTheme theme,
        out int problemIndex,
        out int stepIndex)
    {
        // 기본값 (파싱 실패 대비)
        theme = ProblemTheme.Director;
        problemIndex = 0;
        stepIndex = 0;

        if (string.IsNullOrWhiteSpace(typeName))
            return;

        // 예: "Director_Problem1_Step3"
        var parts = typeName.Split('_');
        // 기대: ["Director", "Problem1", "Step3"]

        if (parts.Length >= 3)
        {
            // 1) theme (Director / Gardener ...)
            //    enum 이름과 동일하다고 가정 (ProblemTheme 참조)
            Enum.TryParse(parts[0], out theme);

            // 2) ProblemX → X만 파싱
            if (parts[1].StartsWith("Problem", StringComparison.OrdinalIgnoreCase))
            {
                string num = parts[1].Substring("Problem".Length);  // "1"
                int.TryParse(num, out problemIndex);
            }

            // 3) StepY → Y만 파싱
            if (parts[2].StartsWith("Step", StringComparison.OrdinalIgnoreCase))
            {
                string num = parts[2].Substring("Step".Length);     // "3"
                int.TryParse(num, out stepIndex);
            }
        }
    }
}
