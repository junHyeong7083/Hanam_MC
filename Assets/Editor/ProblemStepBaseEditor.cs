#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(ProblemStepBase), true)]   // true = 자식 타입에도 적용
public class ProblemStepBaseEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // 1) 스크립트 필드(m_Script)는 그냥 표시
        GUI.enabled = false;
        EditorGUILayout.ObjectField(
            "Script",
            MonoScript.FromMonoBehaviour((MonoBehaviour)target),
            typeof(MonoScript),
            false
        );
        GUI.enabled = true;
        EditorGUILayout.Space();

        // 2) useDbSave 먼저 노출
        var useDbSaveProp = serializedObject.FindProperty("useDBSave");
        EditorGUILayout.PropertyField(useDbSaveProp);

        bool useDb = useDbSaveProp.boolValue;

        // 3) useDbSave == true일 때만 context / stepKey 노출
        if (useDb)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("context"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stepKey"));
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 4) 나머지 필드들 그리기 (중복 방지를 위해 제외 목록 지정)
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",      // 스크립트
            "useDBSave",
            "context",
            "stepKey"
        );

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
