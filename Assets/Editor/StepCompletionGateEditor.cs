#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StepCompletionGate))]
public class StepCompletionGateEditor : Editor
{
    SerializedProperty useProgressFillProp;
    SerializedProperty progressFillImageProp;
    SerializedProperty completeRootProp;
    SerializedProperty hideRootProp;

    private void OnEnable()
    {
        useProgressFillProp = serializedObject.FindProperty("useProgressFill");
        progressFillImageProp = serializedObject.FindProperty("progressFillImage");
        completeRootProp = serializedObject.FindProperty("completeRoot");
        hideRootProp = serializedObject.FindProperty("hideRoot");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Script 필드 (읽기 전용)
        GUI.enabled = false;
        EditorGUILayout.ObjectField(
            "Script",
            MonoScript.FromMonoBehaviour((MonoBehaviour)target),
            typeof(MonoScript),
            false
        );
        GUI.enabled = true;
        EditorGUILayout.Space();

        // 1) 진행바 사용 여부
        EditorGUILayout.PropertyField(useProgressFillProp);

        // 2) 진행바를 쓸 때만 Fill 이미지 노출
        if (useProgressFillProp.boolValue)
        {
            EditorGUILayout.PropertyField(progressFillImageProp);
        }

        EditorGUILayout.Space();

        // 3) completeRoot / hideRoot는 항상 보여줌
        EditorGUILayout.PropertyField(completeRootProp);
        EditorGUILayout.PropertyField(hideRootProp);

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
