using UnityEditor;
using UnityEngine;

/// <summary>
/// 시연 모드 토글 - 모든 문제 해금
/// 메뉴: Tools > Hanam > 시연 모드 토글
/// </summary>
public static class DemoModeToggle
{
    private const string MenuPath = "Tools/Hanam/시연 모드 (전체 해금)";
    private const string PrefKey = "HanamDemoMode";

    [MenuItem(MenuPath, priority = 100)]
    public static void Toggle()
    {
        bool current = ProblemSession.DemoMode;
        ProblemSession.DemoMode = !current;
        EditorPrefs.SetBool(PrefKey, ProblemSession.DemoMode);

        Debug.Log($"[DemoMode] 시연 모드: {(ProblemSession.DemoMode ? "ON" : "OFF")}");
    }

    [MenuItem(MenuPath, true)]
    public static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, ProblemSession.DemoMode);
        return true;
    }

    /// <summary>
    /// 에디터 시작 시 저장된 상태 복원
    /// </summary>
    [InitializeOnLoadMethod]
    private static void RestoreOnLoad()
    {
        ProblemSession.DemoMode = EditorPrefs.GetBool(PrefKey, false);
    }
}
