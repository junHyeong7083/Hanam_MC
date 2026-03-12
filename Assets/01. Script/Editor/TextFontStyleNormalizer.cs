using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// Text 컴포넌트의 FontStyle을 Normal로 변경하는 에디터 도구
/// - DFS로 모든 자식 오브젝트 탐색
/// - FontStyle이 Normal이 아니면 Normal로 변경
/// </summary>
public class TextFontStyleNormalizer : EditorWindow
{
    private GameObject rootObject;
    private int changedCount = 0;
    private int totalTextCount = 0;

    [MenuItem("Tools/Font/Text FontStyle Normal로 변경")]
    public static void ShowWindow()
    {
        GetWindow<TextFontStyleNormalizer>("FontStyle Normalizer");
    }

    private void OnGUI()
    {
        GUILayout.Label("Text FontStyle Normalizer", EditorStyles.boldLabel);
        GUILayout.Space(10);

        rootObject = (GameObject)EditorGUILayout.ObjectField("Root Object", rootObject, typeof(GameObject), true);

        GUILayout.Space(10);

        if (GUILayout.Button("FontStyle Normal로 변경", GUILayout.Height(30)))
        {
            if (rootObject != null)
            {
                changedCount = 0;
                totalTextCount = 0;
                NormalizeFontStyleDFS(rootObject.transform);
                EditorUtility.DisplayDialog("완료",
                    $"총 {totalTextCount}개 Text 중 {changedCount}개 변경됨", "확인");
            }
            else
            {
                EditorUtility.DisplayDialog("오류", "Root Object를 선택해주세요.", "확인");
            }
        }

        GUILayout.Space(10);
        GUILayout.Label($"마지막 결과: {changedCount}/{totalTextCount} 변경됨");
    }

    private void NormalizeFontStyleDFS(Transform parent)
    {
        // 현재 오브젝트의 Text 컴포넌트 확인
        Text textComponent = parent.GetComponent<Text>();
        if (textComponent != null)
        {
            totalTextCount++;

            if (textComponent.fontStyle != FontStyle.Normal)
            {
                Undo.RecordObject(textComponent, "Normalize FontStyle");
                textComponent.fontStyle = FontStyle.Normal;
                EditorUtility.SetDirty(textComponent);
                changedCount++;
                Debug.Log($"[FontStyle 변경] {GetFullPath(parent)}: {textComponent.fontStyle} → Normal");
            }
        }

        // 자식들 DFS 탐색
        for (int i = 0; i < parent.childCount; i++)
        {
            NormalizeFontStyleDFS(parent.GetChild(i));
        }
    }

    private string GetFullPath(Transform t)
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
