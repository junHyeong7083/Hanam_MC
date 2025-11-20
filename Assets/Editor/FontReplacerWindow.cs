// Assets/Editor/FontReplacerWindow.cs

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class FontReplacerWindow : EditorWindow
{
    // 설정값들
    GameObject rootObject;
    Font targetFont;       // 이 폰트만 교체하고 싶으면 지정 (없으면 전부)
    Font newFont;          // 교체할 폰트
    bool onlyIfFontMatches = true;

    [MenuItem("Tools/Font/Replace Fonts (DFS)...")]
    public static void OpenWindow()
    {
        var window = GetWindow<FontReplacerWindow>("Font Replacer");
        window.minSize = new Vector2(350, 180);
    }

    void OnGUI()
    {
        GUILayout.Label("Font Replacer (DFS)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        rootObject = (GameObject)EditorGUILayout.ObjectField(
            "Root Object",
            rootObject,
            typeof(GameObject),
            true
        );

        targetFont = (Font)EditorGUILayout.ObjectField(
            new GUIContent("Target Font (Optional)", "이 폰트만 교체하고 싶으면 지정. 비우면 전부 대상."),
            targetFont,
            typeof(Font),
            false
        );

        newFont = (Font)EditorGUILayout.ObjectField(
            new GUIContent("New Font", "모든 대상 Text가 이 폰트로 교체됩니다."),
            newFont,
            typeof(Font),
            false
        );

        onlyIfFontMatches = EditorGUILayout.Toggle(
            new GUIContent("Only If Font Matches", "체크 시 targetFont와 같은 Text만 교체"),
            onlyIfFontMatches
        );
        if (onlyIfFontMatches)
        {
            EditorGUILayout.HelpBox(
                "현재 설정: targetFont에 지정한 폰트를 가진 Text만 교체합니다.\n" +
                "targetFont가 비어 있으면 아무 것도 교체되지 않습니다.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "현재 설정: 모든 Text 컴포넌트의 폰트를 newFont로 교체합니다.\n" +
                "기존에 어떤 폰트를 쓰고 있던지 상관없이 전부 바뀝니다.",
                MessageType.Info
            );
        }
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        EditorGUI.BeginDisabledGroup(rootObject == null || newFont == null);
        if (GUILayout.Button("자식 Text 폰트 교체 (DFS 실행)", GUILayout.Height(30)))
        {
            ReplaceFonts();
        }
        EditorGUI.EndDisabledGroup();

        if (rootObject == null)
            EditorGUILayout.HelpBox("Root Object를 선택하세요.", MessageType.Info);
        else if (newFont == null)
            EditorGUILayout.HelpBox("New Font를 지정해야 합니다.", MessageType.Info);
    }

    void ReplaceFonts()
    {
        if (rootObject == null)
        {
            Debug.LogWarning("[FontReplacerWindow] Root Object가 없습니다.");
            return;
        }
        if (newFont == null)
        {
            Debug.LogError("[FontReplacerWindow] New Font가 비어 있습니다.");
            return;
        }

        int count = 0;
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        DFS(rootObject.transform, targetFont, newFont, onlyIfFontMatches, ref count);

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[FontReplacerWindow] {rootObject.name} 기준으로 {count}개의 Text 폰트를 교체했습니다.");
    }

    static void DFS(Transform node, Font targetFont, Font newFont, bool onlyIfMatch, ref int count)
    {
        if (node == null) return;

        var text = node.GetComponent<Text>();
        if (text != null)
        {
            bool shouldChange = true;

            if (onlyIfMatch)
            {
                if (targetFont != null)
                    shouldChange = (text.font == targetFont);
                else
                    shouldChange = true; // 타겟 지정 안 했으면 사실상 전부
            }

            if (shouldChange)
            {
                Undo.RecordObject(text, "Replace Text Font");
                text.font = newFont;
                EditorUtility.SetDirty(text);
                count++;
            }
        }

        foreach (Transform child in node)
            DFS(child, targetFont, newFont, onlyIfMatch, ref count);
    }
}
