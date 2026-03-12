using System;

/// <summary>
/// HomeReturnTarget - HomeScene 복귀 시 어떤 패널을 표시할지 지정하는 열거형.
/// ProblemScene에서 문제 완료 후 HomeScene으로 돌아갈 때,
/// 이 값에 따라 HomeScene이 적절한 패널을 자동으로 연다.
/// </summary>
public enum HomeReturnTarget
{
    None,           // 기본 (테마 선택 패널)
    LevelSelect,    // Director LevelSelectPanel로 바로 이동
    Ending          // Director EndingPanel로 이동 (P10 완주)
}

/// <summary>
/// ProblemSession - 씬 간 문제 컨텍스트를 전달하는 정적 컨테이너
///
/// 【역할】 HomeScene에서 문제를 선택하면 이 정적 클래스에 테마, 문제 번호 등을 설정하고,
///          ProblemScene이 로드된 후 이 값들을 읽어 적절한 문제를 활성화한다.
///          static class이므로 씬 전환 시에도 데이터가 유지된다.
/// 【참조하는 곳】 ProblemSceneController (테마/문제번호 읽기),
///                StepFlowController (ProblemEnd → ReturnTarget 설정),
///                CommonRewardStep (GoToHome → ReturnTarget 설정),
///                InventoryDropTargetStepBase (DemoMode 체크),
///                StartStep (CurrentProblemIndex로 텍스트 조회)
/// 【참조되는 곳】 HomeScene의 문제 선택 UI에서 값 설정
/// 【흐름】 HomeScene에서 문제 선택 → ProblemSession 값 설정 → ProblemScene 로드
///          → ProblemSceneController가 값 읽기 → 문제 완료 후 ReturnTarget 설정 → HomeScene 복귀
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
    public static HomeReturnTarget ReturnTarget { get; set; } = HomeReturnTarget.None;

    /// <summary>시연 모드: true이면 모든 문제 해금 + 아이템 전체 보유 처리. 시연 후 false로 되돌릴 것.</summary>
    public static bool DemoMode { get; set; } = true;
}
