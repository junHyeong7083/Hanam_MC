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

        // 2) useDBSave 먼저 노출
        var useDbSaveProp = serializedObject.FindProperty("useDBSave");
        if (useDbSaveProp == null)
        {
            // 필드 이름이 바뀌었을 때 안전장치
            DrawDefaultInspector();
            return;
        }

        EditorGUILayout.PropertyField(useDbSaveProp);
        bool useDb = useDbSaveProp.boolValue;

        // 3) useDBSave == true일 때만 context / stepKeyConfig 노출
        if (useDb)
        {
            EditorGUILayout.PropertyField(serializedObject.FindProperty("context"));
            EditorGUILayout.PropertyField(serializedObject.FindProperty("stepKeyConfig"));
        }



        EditorGUILayout.Space();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // 4) 나머지 필드들 그리기 (중복 방지를 위해 제외 목록 지정)
        DrawPropertiesExcluding(
            serializedObject,
            "m_Script",      // 스크립트
            "useDBSave",
            "context",
            "stepKeyConfig"
        );

        serializedObject.ApplyModifiedProperties();
    }
}
#endif
