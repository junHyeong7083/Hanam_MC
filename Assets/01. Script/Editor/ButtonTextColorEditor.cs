using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public class ButtonTextColorEditor : EditorWindow
{
    private GameObject targetButton;
    private Color targetColor = Color.black;

    [MenuItem("Tools/Button Text Color Editor")]
    public static void ShowWindow()
    {
        GetWindow<ButtonTextColorEditor>("Button Text Color");
    }

    private void OnGUI()
    {
        GUILayout.Label("버튼 자식 Text 색상 일괄 변경", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        targetButton = (GameObject)EditorGUILayout.ObjectField("대상 버튼", targetButton, typeof(GameObject), true);
        targetColor = EditorGUILayout.ColorField("변경할 색상", targetColor);

        EditorGUILayout.Space(8);

        bool canApply = targetButton != null;
        EditorGUI.BeginDisabledGroup(!canApply);
        if (GUILayout.Button("색상 적용", GUILayout.Height(32)))
            Apply();
        EditorGUI.EndDisabledGroup();

        if (!canApply)
            EditorGUILayout.HelpBox("대상 버튼을 지정해주세요.", MessageType.Info);
    }

    private void Apply()
    {
        var texts = targetButton.GetComponentsInChildren<Text>(true);
        if (texts.Length == 0)
        {
            Debug.LogWarning($"[ButtonTextColorEditor] '{targetButton.name}'의 자식에 Text 컴포넌트가 없습니다.");
            return;
        }

        Undo.RecordObjects(texts, "Change Button Text Color");
        foreach (var t in texts)
        {
            t.color = targetColor;
            EditorUtility.SetDirty(t);
        }

        Debug.Log($"[ButtonTextColorEditor] '{targetButton.name}' 자식 Text {texts.Length}개 색상 변경 완료 → {targetColor}");
    }
}
