#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(IntroStepController))]
public class IntroStepControllerEditor : Editor
{
    SerializedProperty dialogueText;
    SerializedProperty nextDialogueButton;
    SerializedProperty proceedButton;

    SerializedProperty useProceedButton;
    SerializedProperty onFinished;

    SerializedProperty dialogueTextIds;
    SerializedProperty usePlaceholder;
    SerializedProperty placeholderTextId;
    SerializedProperty placeholderCount;

    void OnEnable()
    {
        dialogueText = serializedObject.FindProperty("dialogueText");
        nextDialogueButton = serializedObject.FindProperty("nextDialogueButton");
        proceedButton = serializedObject.FindProperty("proceedButton");

        useProceedButton = serializedObject.FindProperty("useProceedButton");
        onFinished = serializedObject.FindProperty("onFinished");

        dialogueTextIds = serializedObject.FindProperty("dialogueTextIds");
        usePlaceholder = serializedObject.FindProperty("usePlaceholder");
        placeholderTextId = serializedObject.FindProperty("placeholderTextId");
        placeholderCount = serializedObject.FindProperty("placeholderCount");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.LabelField("UI Refs", EditorStyles.boldLabel);
        DrawProp(dialogueText);
        DrawProp(nextDialogueButton);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Flow Mode", EditorStyles.boldLabel);
        DrawProp(useProceedButton);

        // Proceed 버튼은 useProceedButton=true일 때만 보여주기
        if (useProceedButton != null && useProceedButton.boolValue)
            DrawProp(proceedButton);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Callbacks", EditorStyles.boldLabel);
        DrawProp(onFinished);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Dialogue Source", EditorStyles.boldLabel);
        DrawProp(usePlaceholder);

        if (usePlaceholder != null && usePlaceholder.boolValue)
        {
            DrawProp(placeholderTextId);
            DrawProp(placeholderCount);
        }
        else
        {
            // 인덱스 입력칸(리스트)
            if (dialogueTextIds != null)
                EditorGUILayout.PropertyField(dialogueTextIds, true);
            else
                EditorGUILayout.HelpBox("dialogueTextIds 필드를 찾지 못했어요. IntroStepController의 변수명을 확인하세요.", MessageType.Warning);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawProp(SerializedProperty prop)
    {
        if (prop == null)
        {
            EditorGUILayout.HelpBox("에디터 스크립트가 IntroStepController의 필드명을 못 찾았습니다(필드명 변경/삭제됨).", MessageType.Info);
            return;
        }

        EditorGUILayout.PropertyField(prop);
    }
}
#endif