// Assets/Editor/FontReplacerWindow.cs

using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UGUI Text / TextMeshPro 둘 다 지원하는 폰트 교체 툴
/// </summary>
public class FontReplacerWindow : EditorWindow
{
    private enum TargetType
    {
        UGUI_Text,
        TextMeshPro
    }

    // 공통
    GameObject rootObject;
    TargetType targetType = TargetType.UGUI_Text;
    bool onlyIfFontMatches = true;

    // UGUI 전용
    Font targetFont;
    Font newFont;

    // TMP 전용
    TMP_FontAsset targetTMPFont;
    TMP_FontAsset newTMPFont;

    [MenuItem("Tools/Font/Replace Fonts (DFS)...")]
    public static void OpenWindow()
    {
        var window = GetWindow<FontReplacerWindow>("Font Replacer");
        window.minSize = new Vector2(380, 220);
    }

    void OnGUI()
    {
        GUILayout.Label("Font Replacer (DFS)", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1) 루트 오브젝트
        rootObject = (GameObject)EditorGUILayout.ObjectField(
            "Root Object",
            rootObject,
            typeof(GameObject),
            true
        );

        EditorGUILayout.Space();

        // 2) 어떤 타입의 텍스트를 바꿀지
        targetType = (TargetType)EditorGUILayout.EnumPopup(
            new GUIContent("Target Type", "UGUI Text 또는 TextMeshPro 중 어떤 컴포넌트를 대상으로 할지 선택"),
            targetType
        );

        EditorGUILayout.Space();

        // 3) 타입별 폰트 설정
        if (targetType == TargetType.UGUI_Text)
        {
            DrawUGUIFontSection();
        }
        else
        {
            DrawTMPFontSection();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 4) 실행 버튼
        bool hasNewFont =
            (targetType == TargetType.UGUI_Text && newFont != null) ||
            (targetType == TargetType.TextMeshPro && newTMPFont != null);

        EditorGUI.BeginDisabledGroup(rootObject == null || !hasNewFont);
        if (GUILayout.Button("자식 Text 폰트 교체 (DFS 실행)", GUILayout.Height(30)))
        {
            ReplaceFonts();
        }
        EditorGUI.EndDisabledGroup();

        // 5) 안내 메시지
        if (rootObject == null)
            EditorGUILayout.HelpBox("Root Object를 선택하세요.", MessageType.Info);
        else if (!hasNewFont)
            EditorGUILayout.HelpBox("교체할 New Font를 지정해야 합니다.", MessageType.Info);
    }

    void DrawUGUIFontSection()
    {
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
                "현재 설정: targetFont에 지정한 폰트를 가진 UGUI Text만 교체합니다.\n" +
                "targetFont가 비어 있으면 모든 UGUI Text가 대상이 됩니다.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "현재 설정: 모든 UGUI Text 컴포넌트의 폰트를 newFont로 교체합니다.\n" +
                "기존에 어떤 폰트를 쓰고 있던지 상관없이 전부 바뀝니다.",
                MessageType.Info
            );
        }
    }

    void DrawTMPFontSection()
    {
        targetTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            new GUIContent("Target TMP Font (Optional)", "이 TMP 폰트만 교체하고 싶으면 지정. 비우면 전부 대상."),
            targetTMPFont,
            typeof(TMP_FontAsset),
            false
        );

        newTMPFont = (TMP_FontAsset)EditorGUILayout.ObjectField(
            new GUIContent("New TMP Font", "모든 대상 TMP Text가 이 TMP 폰트로 교체됩니다."),
            newTMPFont,
            typeof(TMP_FontAsset),
            false
        );

        onlyIfFontMatches = EditorGUILayout.Toggle(
            new GUIContent("Only If Font Matches", "체크 시 targetTMPFont와 같은 TMP Text만 교체"),
            onlyIfFontMatches
        );

        if (onlyIfFontMatches)
        {
            EditorGUILayout.HelpBox(
                "현재 설정: targetTMPFont에 지정한 TMP 폰트를 가진 TMP Text만 교체합니다.\n" +
                "targetTMPFont가 비어 있으면 모든 TMP Text가 대상이 됩니다.",
                MessageType.Info
            );
        }
        else
        {
            EditorGUILayout.HelpBox(
                "현재 설정: 모든 TMP Text 컴포넌트의 폰트를 newTMPFont로 교체합니다.\n" +
                "기존에 어떤 폰트를 쓰고 있던지 상관없이 전부 바뀝니다.",
                MessageType.Info
            );
        }
    }

    void ReplaceFonts()
    {
        if (rootObject == null)
        {
            Debug.LogWarning("[FontReplacerWindow] Root Object가 없습니다.");
            return;
        }

        int count = 0;
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        if (targetType == TargetType.UGUI_Text)
        {
            DFS_UGUI(rootObject.transform, targetFont, newFont, onlyIfFontMatches, ref count);
        }
        else
        {
            DFS_TMP(rootObject.transform, targetTMPFont, newTMPFont, onlyIfFontMatches, ref count);
        }

        Undo.CollapseUndoOperations(undoGroup);
        Debug.Log($"[FontReplacerWindow] {rootObject.name} 기준으로 {count}개의 {targetType} 폰트를 교체했습니다.");
    }

    // UGUI Text용 DFS
    static void DFS_UGUI(Transform node, Font targetFont, Font newFont, bool onlyIfMatch, ref int count)
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
                Undo.RecordObject(text, "Replace UGUI Text Font");
                text.font = newFont;
                EditorUtility.SetDirty(text);
                count++;
            }
        }

        foreach (Transform child in node)
            DFS_UGUI(child, targetFont, newFont, onlyIfMatch, ref count);
    }

    // TextMeshPro용 DFS
    static void DFS_TMP(Transform node, TMP_FontAsset targetFont, TMP_FontAsset newFont, bool onlyIfMatch, ref int count)
    {
        if (node == null) return;

        var tmp = node.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            bool shouldChange = true;

            if (onlyIfMatch)
            {
                if (targetFont != null)
                    shouldChange = (tmp.font == targetFont);
                else
                    shouldChange = true; // 타겟 지정 안 했으면 사실상 전부
            }

            if (shouldChange)
            {
                Undo.RecordObject(tmp, "Replace TMP Font");
                tmp.font = newFont;
                EditorUtility.SetDirty(tmp);
                count++;
            }
        }

        foreach (Transform child in node)
            DFS_TMP(child, targetFont, newFont, onlyIfMatch, ref count);
    }
}
