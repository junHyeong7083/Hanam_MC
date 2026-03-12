using UnityEngine;
using UnityEditor;

/// <summary>
/// IntroElement 커스텀 PropertyDrawer
/// - Slide 타입일 때: Direction, Distance만 표시
/// - Scale 타입일 때: Start Scale만 표시
/// </summary>
[CustomPropertyDrawer(typeof(EffectControllerBase.IntroElement))]
public class IntroElementDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!property.isExpanded)
            return EditorGUIUtility.singleLineHeight;

        var animationType = property.FindPropertyRelative("animationType");
        bool isSlide = animationType.enumValueIndex == 0;

        // 기본 4줄: target, animationType, delay, duration
        // Slide: +2줄 (direction, distance)
        // Scale: +1줄 (startScale)
        int lines = 4 + (isSlide ? 2 : 1);

        return EditorGUIUtility.singleLineHeight * (lines + 1) + EditorGUIUtility.standardVerticalSpacing * lines;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        // Foldout
        Rect foldoutRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
        property.isExpanded = EditorGUI.Foldout(foldoutRect, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float y = position.y + EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
            float lineHeight = EditorGUIUtility.singleLineHeight;
            float spacing = EditorGUIUtility.standardVerticalSpacing;

            // Target
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                property.FindPropertyRelative("target"));
            y += lineHeight + spacing;

            // Animation Type
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                property.FindPropertyRelative("animationType"));
            y += lineHeight + spacing;

            // Delay
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                property.FindPropertyRelative("delay"));
            y += lineHeight + spacing;

            // Duration
            EditorGUI.PropertyField(
                new Rect(position.x, y, position.width, lineHeight),
                property.FindPropertyRelative("duration"));
            y += lineHeight + spacing;

            var animationType = property.FindPropertyRelative("animationType");
            bool isSlide = animationType.enumValueIndex == 0;

            if (isSlide)
            {
                // Direction
                EditorGUI.PropertyField(
                    new Rect(position.x, y, position.width, lineHeight),
                    property.FindPropertyRelative("direction"));
                y += lineHeight + spacing;

                // Distance
                EditorGUI.PropertyField(
                    new Rect(position.x, y, position.width, lineHeight),
                    property.FindPropertyRelative("distance"));
            }
            else
            {
                // Start Scale
                EditorGUI.PropertyField(
                    new Rect(position.x, y, position.width, lineHeight),
                    property.FindPropertyRelative("startScale"));
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }
}
