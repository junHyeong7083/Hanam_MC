using UnityEngine;
using UnityEditor;

/// <summary>
/// Director 하위 Problem 패널들의 자식(Step)들을 모두 비활성화하는 에디터 도구
/// 메뉴: Tools > Hanam > Director 자식 모두 끄기
/// </summary>
public class DirectorChildrenDisabler : EditorWindow
{
    [MenuItem("Tools/Hanam/Director 자식 모두 끄기")]
    public static void DisableAllDirectorChildren()
    {
        // Director 오브젝트 찾기
        GameObject director = GameObject.Find("Canvas/Panel/Director");

        if (director == null)
        {
            // 다른 경로로도 시도
            director = GameObject.Find("Director");
        }

        if (director == null)
        {
            EditorUtility.DisplayDialog("오류", "Director 오브젝트를 찾을 수 없습니다.\nCanvas/Panel/Director 경로를 확인하세요.", "확인");
            return;
        }

        int disabledCount = 0;

        // Director의 각 Problem 자식들 순회
        foreach (Transform problem in director.transform)
        {
            // Problem의 자식들(Step들) 순회하며 DFS로 비활성화
            disabledCount += DisableChildrenDFS(problem);
        }

        // Undo 기록
        Undo.RegisterCompleteObjectUndo(director, "Director 자식 모두 끄기");

        Debug.Log($"[DirectorChildrenDisabler] {disabledCount}개의 오브젝트를 비활성화했습니다.");
        EditorUtility.DisplayDialog("완료", $"{disabledCount}개의 오브젝트를 비활성화했습니다.", "확인");
    }

    /// <summary>
    /// DFS로 자식들을 비활성화
    /// </summary>
    private static int DisableChildrenDFS(Transform parent)
    {
        int count = 0;

        foreach (Transform child in parent)
        {
            if (child.gameObject.activeSelf)
            {
                Undo.RecordObject(child.gameObject, "Disable Child");
                child.gameObject.SetActive(false);
                count++;
                Debug.Log($"[DirectorChildrenDisabler] 비활성화: {GetFullPath(child)}");
            }

            // 재귀적으로 자식의 자식도 처리 (이미 비활성화된 부모 아래는 건너뛸 수 있지만, 일단 기록)
            // count += DisableChildrenDFS(child); // 필요시 주석 해제
        }

        return count;
    }

    /// <summary>
    /// Transform의 전체 경로 반환
    /// </summary>
    private static string GetFullPath(Transform t)
    {
        string path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
