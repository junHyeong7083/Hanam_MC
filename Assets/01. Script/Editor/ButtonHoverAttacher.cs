using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Button 오브젝트에 ButtonHover 컴포넌트를 일괄 추가하는 에디터 윈도우
/// </summary>
public class ButtonHoverAttacher : EditorWindow
{
    private GameObject targetRoot;

    [MenuItem("Window/Button Hover Attacher")]
    public static void ShowWindow()
    {
        GetWindow<ButtonHoverAttacher>("Button Hover Attacher");
    }

    private void OnGUI()
    {
        GUILayout.Label("ButtonHover 일괄 추가", EditorStyles.boldLabel);
        GUILayout.Space(10);

        targetRoot = (GameObject)EditorGUILayout.ObjectField(
            "대상 오브젝트",
            targetRoot,
            typeof(GameObject),
            true
        );

        GUILayout.Space(10);

        EditorGUI.BeginDisabledGroup(targetRoot == null);
        if (GUILayout.Button("ButtonHover 추가", GUILayout.Height(30)))
        {
            int count = AttachButtonHoverRecursive(targetRoot.transform);
            EditorUtility.DisplayDialog(
                "완료",
                $"{count}개의 Button에 ButtonHover를 추가했습니다.",
                "확인"
            );
        }
        EditorGUI.EndDisabledGroup();
    }

    private int AttachButtonHoverRecursive(Transform parent)
    {
        int count = 0;

        // 현재 오브젝트에 Button이 있는지 확인
        Button button = parent.GetComponent<Button>();
        if (button != null)
        {
            // ButtonHover가 없으면 추가
            if (parent.GetComponent<ButtonHover>() == null)
            {
                Undo.AddComponent<ButtonHover>(parent.gameObject);
                count++;
            }
        }

        // 자식들 DFS 탐색
        for (int i = 0; i < parent.childCount; i++)
        {
            count += AttachButtonHoverRecursive(parent.GetChild(i));
        }

        return count;
    }
}
