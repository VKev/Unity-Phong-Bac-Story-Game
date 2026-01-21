using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(InteractionUITrigger.InteractAudioItem))]
public class InteractionUITriggerAudioItemDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        if (!property.isExpanded)
            return lineHeight;

        SerializedProperty voiceId = property.FindPropertyRelative("voiceId");
        SerializedProperty useChoiceCondition = property.FindPropertyRelative("useChoiceCondition");
        SerializedProperty requiredChoiceIndex = property.FindPropertyRelative("requiredChoiceIndex");
        SerializedProperty multiChoice = property.FindPropertyRelative("multiChoice");
        SerializedProperty title = property.FindPropertyRelative("title");
        SerializedProperty titlePrefab = property.FindPropertyRelative("titlePrefab");
        SerializedProperty titlePrefabTag = property.FindPropertyRelative("titlePrefabTag");
        SerializedProperty choices = property.FindPropertyRelative("choices");
        SerializedProperty autoPlay = property.FindPropertyRelative("autoPlay");
        SerializedProperty delay = property.FindPropertyRelative("delay");
        SerializedProperty subtitleTarget = property.FindPropertyRelative("subtitleTarget");
        SerializedProperty subtitleText = property.FindPropertyRelative("subtitleText");
        SerializedProperty clearSubtitleOnEnd = property.FindPropertyRelative("clearSubtitleOnEnd");
        SerializedProperty clip = property.FindPropertyRelative("clip");
        SerializedProperty mixerGroup = property.FindPropertyRelative("mixerGroup");

        float height = lineHeight;
        height += spacing + EditorGUI.GetPropertyHeight(voiceId);
        height += spacing + EditorGUI.GetPropertyHeight(useChoiceCondition);
        if (useChoiceCondition != null && useChoiceCondition.boolValue)
            height += spacing + EditorGUI.GetPropertyHeight(requiredChoiceIndex);
        height += spacing + EditorGUI.GetPropertyHeight(multiChoice);
        height += spacing + EditorGUI.GetPropertyHeight(autoPlay);
        height += spacing + EditorGUI.GetPropertyHeight(delay);

        bool isMultiChoice = multiChoice != null && multiChoice.boolValue;
        if (isMultiChoice)
        {
            if (title != null)
                height += spacing + EditorGUI.GetPropertyHeight(title);
            else
                height += spacing + lineHeight;
            if (titlePrefab != null)
                height += spacing + EditorGUI.GetPropertyHeight(titlePrefab);
            else
                height += spacing + lineHeight;
            if (titlePrefabTag != null)
                height += spacing + EditorGUI.GetPropertyHeight(titlePrefabTag);
            else
                height += spacing + lineHeight;
            if (choices != null)
                height += spacing + EditorGUI.GetPropertyHeight(choices, true);
            else
                height += spacing + lineHeight;
        }
        else
        {
            height += spacing + EditorGUI.GetPropertyHeight(subtitleTarget);
            height += spacing + EditorGUI.GetPropertyHeight(subtitleText);
            height += spacing + EditorGUI.GetPropertyHeight(clearSubtitleOnEnd);
            height += spacing + EditorGUI.GetPropertyHeight(clip);
            height += spacing + EditorGUI.GetPropertyHeight(mixerGroup);
        }

        return height;
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;
        Rect line = new Rect(position.x, position.y, position.width, lineHeight);

        property.isExpanded = EditorGUI.Foldout(line, property.isExpanded, label, true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;

            SerializedProperty voiceId = property.FindPropertyRelative("voiceId");
            SerializedProperty useChoiceCondition = property.FindPropertyRelative("useChoiceCondition");
            SerializedProperty requiredChoiceIndex = property.FindPropertyRelative("requiredChoiceIndex");
            SerializedProperty multiChoice = property.FindPropertyRelative("multiChoice");
            SerializedProperty title = property.FindPropertyRelative("title");
            SerializedProperty titlePrefab = property.FindPropertyRelative("titlePrefab");
            SerializedProperty titlePrefabTag = property.FindPropertyRelative("titlePrefabTag");
            SerializedProperty choices = property.FindPropertyRelative("choices");
            SerializedProperty autoPlay = property.FindPropertyRelative("autoPlay");
            SerializedProperty delay = property.FindPropertyRelative("delay");
            SerializedProperty subtitleTarget = property.FindPropertyRelative("subtitleTarget");
            SerializedProperty subtitleText = property.FindPropertyRelative("subtitleText");
            SerializedProperty clearSubtitleOnEnd = property.FindPropertyRelative("clearSubtitleOnEnd");
            SerializedProperty clip = property.FindPropertyRelative("clip");
            SerializedProperty mixerGroup = property.FindPropertyRelative("mixerGroup");

            line.y += lineHeight + spacing;
            line.height = EditorGUI.GetPropertyHeight(voiceId);
            EditorGUI.PropertyField(line, voiceId);

            line.y += line.height + spacing;
            line.height = EditorGUI.GetPropertyHeight(useChoiceCondition);
            if (useChoiceCondition != null)
                EditorGUI.PropertyField(line, useChoiceCondition, new GUIContent("Use Choice Condition"));
            else
                EditorGUI.LabelField(line, "Use Choice Condition", "Missing field");

            if (useChoiceCondition != null && useChoiceCondition.boolValue)
            {
                line.y += line.height + spacing;
                line.height = EditorGUI.GetPropertyHeight(requiredChoiceIndex);
                if (requiredChoiceIndex != null)
                    EditorGUI.PropertyField(line, requiredChoiceIndex, new GUIContent("Choice Index"));
                else
                    EditorGUI.LabelField(line, "Choice Index", "Missing field");
            }

            line.y += line.height + spacing;
            line.height = EditorGUI.GetPropertyHeight(multiChoice);
            EditorGUI.PropertyField(line, multiChoice, new GUIContent("Multi Choice"));

            line.y += line.height + spacing;
            line.height = EditorGUI.GetPropertyHeight(autoPlay);
            EditorGUI.PropertyField(line, autoPlay);

            line.y += line.height + spacing;
            line.height = EditorGUI.GetPropertyHeight(delay);
            EditorGUI.PropertyField(line, delay);

            bool isMultiChoice = multiChoice != null && multiChoice.boolValue;
            if (isMultiChoice)
            {
                line.y += line.height + spacing;
                if (title != null)
                {
                    line.height = EditorGUI.GetPropertyHeight(title);
                    EditorGUI.PropertyField(line, title, new GUIContent("Title"));
                }
                else
                {
                    line.height = lineHeight;
                    EditorGUI.LabelField(line, "Title", "Missing field");
                }

                line.y += line.height + spacing;
                if (titlePrefab != null)
                {
                    line.height = EditorGUI.GetPropertyHeight(titlePrefab);
                    EditorGUI.PropertyField(line, titlePrefab, new GUIContent("Title Prefab"));
                }
                else
                {
                    line.height = lineHeight;
                    EditorGUI.LabelField(line, "Title Prefab", "Missing field");
                }

                line.y += line.height + spacing;
                if (titlePrefabTag != null)
                {
                    line.height = EditorGUI.GetPropertyHeight(titlePrefabTag);
                    EditorGUI.PropertyField(line, titlePrefabTag, new GUIContent("Title Prefab Tag"));
                }
                else
                {
                    line.height = lineHeight;
                    EditorGUI.LabelField(line, "Title Prefab Tag", "Missing field");
                }

                line.y += line.height + spacing;
                if (choices != null)
                {
                    line.height = EditorGUI.GetPropertyHeight(choices, true);
                    EditorGUI.PropertyField(line, choices, true);
                }
                else
                {
                    line.height = lineHeight;
                    EditorGUI.LabelField(line, "Choices", "Missing field");
                }
            }
            else
            {
                line.y += line.height + spacing;
                line.height = EditorGUI.GetPropertyHeight(subtitleTarget);
                EditorGUI.PropertyField(line, subtitleTarget);

                line.y += line.height + spacing;
                line.height = EditorGUI.GetPropertyHeight(subtitleText);
                EditorGUI.PropertyField(line, subtitleText);

                line.y += line.height + spacing;
                line.height = EditorGUI.GetPropertyHeight(clearSubtitleOnEnd);
                EditorGUI.PropertyField(line, clearSubtitleOnEnd);

                line.y += line.height + spacing;
                line.height = EditorGUI.GetPropertyHeight(clip);
                EditorGUI.PropertyField(line, clip);

                line.y += line.height + spacing;
                line.height = EditorGUI.GetPropertyHeight(mixerGroup);
                EditorGUI.PropertyField(line, mixerGroup);
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }
}
