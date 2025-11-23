#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(StepCompletionGate))]
public class StepCompletionGateEditor : Editor
{
    SerializedProperty useProgressFillProp;
    SerializedProperty progressFillImageProp;

    SerializedProperty useCompleteProp;
    SerializedProperty completeRootProp;

    SerializedProperty useHideProp;
    SerializedProperty hideRootProp;

    private void OnEnable()
    {
        useProgressFillProp = serializedObject.FindProperty("useProgressFill");
        progressFillImageProp = serializedObject.FindProperty("progressFillImage");

        useCompleteProp = serializedObject.FindProperty("useCompleteRoot");
        completeRootProp = serializedObject.FindProperty("completeRoot");

        useHideProp = serializedObject.FindProperty("useHideRoot");
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
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 3) Complete Root 사용 여부 + 대상
        EditorGUILayout.PropertyField(useCompleteProp);
        if (useCompleteProp.boolValue)
        {
            EditorGUILayout.PropertyField(completeRootProp);
        }

        EditorGUILayout.Space();

        // 4) Hide Root 사용 여부 + 대상
        EditorGUILayout.PropertyField(useHideProp);
        if (useHideProp.boolValue)
        {
            EditorGUILayout.PropertyField(hideRootProp);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
