using UnityEngine;
using UnityEditor;

/// <summary>
/// Director 하위 Problem/Step 비활성화 에디터 도구
/// 메뉴: Tools > Hanam >
///   - Director 자식(Step) 모두 끄기
///   - Problem 모두 끄기
///   - Problem + 자식(Step) 모두 끄기
/// </summary>
public class DirectorChildrenDisabler : EditorWindow
{
    private static GameObject FindDirector()
    {
        var director = GameObject.Find("Canvas/Panel/Director");
        if (director == null)
            director = GameObject.Find("Director");

        if (director == null)
        {
            EditorUtility.DisplayDialog("오류",
                "Director 오브젝트를 찾을 수 없습니다.\nCanvas/Panel/Director 경로를 확인하세요.", "확인");
        }
        return director;
    }

    // ============================
    // 1) 기존: Director 자식(Step) 모두 끄기
    // ============================

    [MenuItem("Tools/Hanam/Director 자식(Step) 모두 끄기")]
    public static void DisableAllDirectorChildren()
    {
        var director = FindDirector();
        if (director == null) return;

        int count = 0;
        foreach (Transform problem in director.transform)
        {
            count += DisableChildren(problem);
        }

        Undo.RegisterCompleteObjectUndo(director, "Director 자식(Step) 모두 끄기");
        Debug.Log($"[DirectorChildrenDisabler] Step {count}개 비활성화");
        EditorUtility.DisplayDialog("완료", $"Step {count}개를 비활성화했습니다.", "확인");
    }

    // ============================
    // 2) Problem 모두 끄기
    // ============================

    [MenuItem("Tools/Hanam/Problem 모두 끄기")]
    public static void DisableAllProblems()
    {
        var director = FindDirector();
        if (director == null) return;

        int count = 0;
        foreach (Transform problem in director.transform)
        {
            if (problem.gameObject.activeSelf)
            {
                Undo.RecordObject(problem.gameObject, "Disable Problem");
                problem.gameObject.SetActive(false);
                count++;
            }
        }

        Debug.Log($"[DirectorChildrenDisabler] Problem {count}개 비활성화");
        EditorUtility.DisplayDialog("완료", $"Problem {count}개를 비활성화했습니다.", "확인");
    }

    // ============================
    // 3) Problem + 자식(Step) 모두 끄기
    // ============================

    [MenuItem("Tools/Hanam/Problem + Step 모두 끄기")]
    public static void DisableAllProblemsAndChildren()
    {
        var director = FindDirector();
        if (director == null) return;

        int problemCount = 0;
        int stepCount = 0;

        foreach (Transform problem in director.transform)
        {
            stepCount += DisableChildren(problem);

            if (problem.gameObject.activeSelf)
            {
                Undo.RecordObject(problem.gameObject, "Disable Problem");
                problem.gameObject.SetActive(false);
                problemCount++;
            }
        }

        Debug.Log($"[DirectorChildrenDisabler] Problem {problemCount}개 + Step {stepCount}개 비활성화");
        EditorUtility.DisplayDialog("완료",
            $"Problem {problemCount}개 + Step {stepCount}개를 비활성화했습니다.", "확인");
    }

    // ============================
    // 유틸
    // ============================

    private static int DisableChildren(Transform parent)
    {
        int count = 0;
        foreach (Transform child in parent)
        {
            if (child.gameObject.activeSelf)
            {
                Undo.RecordObject(child.gameObject, "Disable Child");
                child.gameObject.SetActive(false);
                count++;
            }
        }
        return count;
    }
}
