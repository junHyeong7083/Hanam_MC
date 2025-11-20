using System;
using Unity.VisualScripting;

/// <summary>
/// ProblemScene에서 필요한 컨텍스트를 임시로 담아두는 스태틱 컨테이너.
/// 나중에 필요하면 SessionManager로 옮겨도 됨.
/// </summary>
public static class ProblemSession
{
    /// <summary>예: Director, Gardener 등</summary>
    public static ProblemTheme CurrentTheme { get; set; }
    
    /// <summary>테마 안에서의 문제 번호(1~10)</summary>
    public static int CurrentProblemIndex { get; set; }

    /// <summary>Problem.Id (문제 마스터 ID). 필요 없으면 안 써도 됨.</summary>
    public static string CurrentProblemId { get; set; }
}
