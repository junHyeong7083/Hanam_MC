using System;

/// <summary>
/// HomeScene 복귀 시 표시할 패널 지정
/// </summary>
public enum HomeReturnTarget
{
    None,           // 기본 (테마 선택 패널)
    LevelSelect,    // Director LevelSelectPanel로 바로 이동
    Ending          // Director EndingPanel로 이동 (P10 완주)
}

/// <summary>
/// ProblemScene에서 필요한 컨텍스트를 임시로 담아두는 정적 컨테이너.
/// </summary>
public static class ProblemSession
{
    /// <summary>테마: Director, Gardener 등</summary>
    public static ProblemTheme CurrentTheme { get; set; }

    /// <summary>그룹 안에서의 문제 번호(1~10)</summary>
    public static int CurrentProblemIndex { get; set; }

    /// <summary>Problem.Id (고유 문자열 ID). 필요 없으면 안 써도 됨.</summary>
    public static string CurrentProblemId { get; set; }

    /// <summary>HomeScene 복귀 시 어떤 패널을 열지 지정</summary>
    public static HomeReturnTarget ReturnTarget { get; set; } = HomeReturnTarget.LevelSelect;
}
