using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

[CustomPropertyDrawer(typeof(PanelEvent))]
public class PanelEventDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float lineHeight = EditorGUIUtility.singleLineHeight;
        float spacing = EditorGUIUtility.standardVerticalSpacing;

        if (!property.isExpanded)
            return lineHeight;

        SerializedProperty useStartVoiceId = property.FindPropertyRelative("useStartVoiceId");
        bool useVoice = useStartVoiceId != null && useStartVoiceId.boolValue;
        SerializedProperty useStartChoiceId = property.FindPropertyRelative("useStartChoiceId");
        bool useChoice = useStartChoiceId != null && useStartChoiceId.boolValue;

        int lineCount = 1; // foldout
        lineCount += 1; // event id
        if (useVoice)
        {
            lineCount += 2; // use start voice id + voice id
        }
        else
        {
            lineCount += 1; // use start event id
            lineCount += 1; // start event id or time start
            lineCount += 1; // start delay or use start voice id
        }
        lineCount += 1; // use start choice id
        if (useChoice)
            lineCount += 1; // choice id

        lineCount += 1; // action

        PanelAction action = GetAction(property);
        if (action == PanelAction.Transparent || action == PanelAction.Black)
            lineCount += 2;
        else if (action == PanelAction.Sound)
            lineCount += 2;
        else if (action == PanelAction.Text)
            lineCount += 6;
        else if (action == PanelAction.SwitchScene)
            lineCount += 1;
        else if (action == PanelAction.LockPlayerCamera)
            lineCount += 2;
        else if (action == PanelAction.UnlockPlayerCamera)
            lineCount += 1;
        else if (action == PanelAction.RotatePlayerCamera)
            lineCount += 3;
        else if (action == PanelAction.RotateCameraToTarget)
            lineCount += 2;
        else if (action == PanelAction.LockPlayerPosition || action == PanelAction.UnlockPlayerPosition)
            lineCount += 1;
        else if (action == PanelAction.AfterCredit)
            lineCount += 6;

        return lineCount * lineHeight + (lineCount - 1) * spacing;
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

            SerializedProperty eventId = property.FindPropertyRelative("eventId");
            SerializedProperty timeStart = property.FindPropertyRelative("timeStart");
            SerializedProperty useTimeStartEventId = property.FindPropertyRelative("useTimeStartEventId");
            SerializedProperty timeStartEventId = property.FindPropertyRelative("timeStartEventId");
            SerializedProperty timeStartEventDelay = property.FindPropertyRelative("timeStartEventDelay");
            SerializedProperty useStartVoiceId = property.FindPropertyRelative("useStartVoiceId");
            SerializedProperty startVoiceId = property.FindPropertyRelative("startVoiceId");
            SerializedProperty useStartChoiceId = property.FindPropertyRelative("useStartChoiceId");
            SerializedProperty startChoiceId = property.FindPropertyRelative("startChoiceId");
            SerializedProperty transitionDelay = property.FindPropertyRelative("transitionDelay");
            SerializedProperty transitionTime = property.FindPropertyRelative("transitionTime");
            SerializedProperty actionProp = property.FindPropertyRelative("action");
            SerializedProperty audioClip = property.FindPropertyRelative("audioClip");
            SerializedProperty audioMixerGroup = property.FindPropertyRelative("audioMixerGroup");
            SerializedProperty transitionInTime = property.FindPropertyRelative("transitionInTime");
            SerializedProperty displayTime = property.FindPropertyRelative("displayTime");
            SerializedProperty useDisplayTimeEventId = property.FindPropertyRelative("useDisplayTimeEventId");
            SerializedProperty displayTimeEventId = property.FindPropertyRelative("displayTimeEventId");
            SerializedProperty transitionOutTime = property.FindPropertyRelative("transitionOutTime");
            SerializedProperty textTarget = property.FindPropertyRelative("textTarget");
            SerializedProperty textContent = property.FindPropertyRelative("textContent");
            SerializedProperty sceneName = property.FindPropertyRelative("sceneName");
            SerializedProperty cinemachineCamera = property.FindPropertyRelative("cinemachineCamera");
            SerializedProperty lockPlayerCamera = property.FindPropertyRelative("lockPlayerCamera");
            SerializedProperty rotatePanTilt = property.FindPropertyRelative("rotatePanTilt");
            SerializedProperty rotateTarget = property.FindPropertyRelative("rotateTarget");
            SerializedProperty afterCreditPanel = property.FindPropertyRelative("afterCreditPanel");
            SerializedProperty afterCreditText = property.FindPropertyRelative("afterCreditText");
            SerializedProperty afterCreditTextMoveSpeed = property.FindPropertyRelative("afterCreditTextMoveSpeed");
            SerializedProperty afterCreditMoveDuration = property.FindPropertyRelative("afterCreditMoveDuration");
            SerializedProperty playerTransform = property.FindPropertyRelative("playerTransform");

            line.y += lineHeight + spacing;
            EditorGUI.PropertyField(line, eventId);

            line.y += lineHeight + spacing;
            bool useVoice = useStartVoiceId != null && useStartVoiceId.boolValue;
            if (useVoice)
            {
                if (useStartVoiceId != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(line, useStartVoiceId, new GUIContent("Use Start Voice Id"));
                    if (EditorGUI.EndChangeCheck() && useStartVoiceId.boolValue && useTimeStartEventId != null)
                        useTimeStartEventId.boolValue = false;
                }
                else
                {
                    EditorGUI.LabelField(line, "Use Start Voice Id", "Missing field");
                }

                line.y += lineHeight + spacing;
                DrawVoiceIdPopup(line, startVoiceId, property);
            }
            else
            {
                if (useTimeStartEventId != null)
                {
                    EditorGUI.BeginChangeCheck();
                    EditorGUI.PropertyField(line, useTimeStartEventId, new GUIContent("Use Start Event Id"));
                    if (EditorGUI.EndChangeCheck() && useTimeStartEventId.boolValue && useStartVoiceId != null)
                        useStartVoiceId.boolValue = false;
                }
                else
                {
                    EditorGUI.LabelField(line, "Use Start Event Id", "Missing field");
                }

                line.y += lineHeight + spacing;
                if (useTimeStartEventId != null && useTimeStartEventId.boolValue)
                {
                    DrawEventIdPopup(line, timeStartEventId, property, "Start Event Id");
                    line.y += lineHeight + spacing;
                    if (timeStartEventDelay != null)
                        EditorGUI.PropertyField(line, timeStartEventDelay, new GUIContent("Start Delay"));
                    else
                        EditorGUI.LabelField(line, "Start Delay", "Missing field");
                }
                else
                {
                    EditorGUI.PropertyField(line, timeStart, new GUIContent("Time Start"));
                    line.y += lineHeight + spacing;
                    if (useStartVoiceId != null)
                    {
                        EditorGUI.BeginChangeCheck();
                        EditorGUI.PropertyField(line, useStartVoiceId, new GUIContent("Use Start Voice Id"));
                        if (EditorGUI.EndChangeCheck() && useStartVoiceId.boolValue && useTimeStartEventId != null)
                            useTimeStartEventId.boolValue = false;
                    }
                    else
                    {
                        EditorGUI.LabelField(line, "Use Start Voice Id", "Missing field");
                    }
                }
            }

            line.y += lineHeight + spacing;
            if (useStartChoiceId != null)
                EditorGUI.PropertyField(line, useStartChoiceId, new GUIContent("Use Start Choice Id"));
            else
                EditorGUI.LabelField(line, "Use Start Choice Id", "Missing field");

            if (useStartChoiceId != null && useStartChoiceId.boolValue)
            {
                line.y += lineHeight + spacing;
                if (startChoiceId != null)
                    EditorGUI.PropertyField(line, startChoiceId, new GUIContent("Choice Id"));
                else
                    EditorGUI.LabelField(line, "Choice Id", "Missing field");
            }

            line.y += lineHeight + spacing;
            EditorGUI.PropertyField(line, actionProp);

            line.y += lineHeight + spacing;
            PanelAction action = GetAction(property);
            if (action == PanelAction.Transparent || action == PanelAction.Black)
            {
                if (transitionDelay != null)
                    EditorGUI.PropertyField(line, transitionDelay, new GUIContent("Action Delay"));
                else
                    EditorGUI.LabelField(line, "Action Delay", "Missing field");
                line.y += lineHeight + spacing;
                if (transitionTime != null)
                    EditorGUI.PropertyField(line, transitionTime, new GUIContent("Transition Time"));
                else
                    EditorGUI.LabelField(line, "Transition Time", "Missing field");
            }
            else if (action == PanelAction.Sound)
            {
                EditorGUI.PropertyField(line, audioClip);
                line.y += lineHeight + spacing;
                if (audioMixerGroup != null)
                    EditorGUI.PropertyField(line, audioMixerGroup);
                else
                    EditorGUI.LabelField(line, "Audio Mixer Group", "Missing field");
            }
            else if (action == PanelAction.Text)
            {
                EditorGUI.PropertyField(line, textTarget);
                line.y += lineHeight + spacing;
                EditorGUI.PropertyField(line, textContent);
                line.y += lineHeight + spacing;
                EditorGUI.PropertyField(line, transitionInTime);
                line.y += lineHeight + spacing;
                if (useDisplayTimeEventId != null)
                    EditorGUI.PropertyField(line, useDisplayTimeEventId, new GUIContent("Use Event Id"));
                else
                    EditorGUI.LabelField(line, "Use Event Id", "Missing field");
                line.y += lineHeight + spacing;
                if (useDisplayTimeEventId != null && useDisplayTimeEventId.boolValue)
                    DrawEventIdPopup(line, displayTimeEventId, property, "Display Time Event");
                else
                    EditorGUI.PropertyField(line, displayTime);
                line.y += lineHeight + spacing;
                EditorGUI.PropertyField(line, transitionOutTime);
            }
            else if (action == PanelAction.SwitchScene)
            {
                if (sceneName != null)
                    EditorGUI.PropertyField(line, sceneName, new GUIContent("Scene Name"));
                else
                    EditorGUI.LabelField(line, "Scene Name", "Missing field");
            }
            else if (action == PanelAction.LockPlayerCamera)
            {
                if (cinemachineCamera != null)
                    EditorGUI.PropertyField(line, cinemachineCamera, new GUIContent("Cinemachine Camera"));
                else
                    EditorGUI.LabelField(line, "Cinemachine Camera", "Missing field");

                line.y += lineHeight + spacing;
                if (lockPlayerCamera != null)
                    EditorGUI.PropertyField(line, lockPlayerCamera, new GUIContent("Lock Camera"));
                else
                    EditorGUI.LabelField(line, "Lock Camera", "Missing field");
            }
            else if (action == PanelAction.UnlockPlayerCamera)
            {
                if (cinemachineCamera != null)
                    EditorGUI.PropertyField(line, cinemachineCamera, new GUIContent("Cinemachine Camera"));
                else
                    EditorGUI.LabelField(line, "Cinemachine Camera", "Missing field");
            }
            else if (action == PanelAction.RotatePlayerCamera)
            {
                if (cinemachineCamera != null)
                    EditorGUI.PropertyField(line, cinemachineCamera, new GUIContent("Cinemachine Camera"));
                else
                    EditorGUI.LabelField(line, "Cinemachine Camera", "Missing field");

                line.y += lineHeight + spacing;
                if (rotatePanTilt != null)
                    EditorGUI.PropertyField(line, rotatePanTilt, new GUIContent("Target Pan/Tilt"));
                else
                    EditorGUI.LabelField(line, "Target Pan/Tilt", "Missing field");

                line.y += lineHeight + spacing;
                if (transitionTime != null)
                    EditorGUI.PropertyField(line, transitionTime, new GUIContent("Rotation Time"));
                else
                    EditorGUI.LabelField(line, "Rotation Time", "Missing field");
            }
            else if (action == PanelAction.RotateCameraToTarget)
            {
                if (cinemachineCamera != null)
                    EditorGUI.PropertyField(line, cinemachineCamera, new GUIContent("Cinemachine Camera"));
                else
                    EditorGUI.LabelField(line, "Cinemachine Camera", "Missing field");

                line.y += lineHeight + spacing;
                if (rotateTarget != null)
                    EditorGUI.PropertyField(line, rotateTarget, new GUIContent("Target Transform"));
                else
                    EditorGUI.LabelField(line, "Target Transform", "Missing field");

                line.y += lineHeight + spacing;
                if (transitionTime != null)
                    EditorGUI.PropertyField(line, transitionTime, new GUIContent("Rotation Time"));
                else
                    EditorGUI.LabelField(line, "Rotation Time", "Missing field");
            }
            else if (action == PanelAction.AfterCredit)
            {
                if (transitionDelay != null)
                    EditorGUI.PropertyField(line, transitionDelay, new GUIContent("Action Delay"));
                else
                    EditorGUI.LabelField(line, "Action Delay", "Missing field");

                line.y += lineHeight + spacing;
                if (transitionTime != null)
                    EditorGUI.PropertyField(line, transitionTime, new GUIContent("Fade In Time"));
                else
                    EditorGUI.LabelField(line, "Fade In Time", "Missing field");

                line.y += lineHeight + spacing;
                if (afterCreditPanel != null)
                    EditorGUI.PropertyField(line, afterCreditPanel, new GUIContent("After Credit Panel"));
                else
                    EditorGUI.LabelField(line, "After Credit Panel", "Missing field");

                line.y += lineHeight + spacing;
                if (afterCreditText != null)
                    EditorGUI.PropertyField(line, afterCreditText, new GUIContent("After Credit Text"));
                else
                    EditorGUI.LabelField(line, "After Credit Text", "Missing field");

                line.y += lineHeight + spacing;
                if (afterCreditTextMoveSpeed != null)
                    EditorGUI.PropertyField(line, afterCreditTextMoveSpeed, new GUIContent("Scroll Speed"));
                else
                    EditorGUI.LabelField(line, "Scroll Speed", "Missing field");

                line.y += lineHeight + spacing;
                if (afterCreditMoveDuration != null)
                    EditorGUI.PropertyField(line, afterCreditMoveDuration, new GUIContent("Scroll Duration"));
                else
                    EditorGUI.LabelField(line, "Scroll Duration", "Missing field");
            }
            else if (action == PanelAction.LockPlayerPosition || action == PanelAction.UnlockPlayerPosition)
            {
                if (playerTransform != null)
                    EditorGUI.PropertyField(line, playerTransform, new GUIContent("Player Transform"));
                else
                    EditorGUI.LabelField(line, "Player Transform", "Missing field");
            }

            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private static PanelAction GetAction(SerializedProperty property)
    {
        SerializedProperty actionProp = property.FindPropertyRelative("action");
        if (actionProp == null)
            return PanelAction.Transparent;

        return (PanelAction)actionProp.enumValueIndex;
    }

    private static void DrawEventIdPopup(
        Rect line,
        SerializedProperty targetEventId,
        SerializedProperty property,
        string label)
    {
        if (targetEventId == null)
        {
            EditorGUI.LabelField(line, label, "Missing field");
            return;
        }

        SerializedProperty eventsArray = property.serializedObject.FindProperty("events");
        if (eventsArray == null || !eventsArray.isArray)
        {
            EditorGUI.PropertyField(line, targetEventId, new GUIContent(label));
            return;
        }

        List<string> labels = new List<string> { "None" };
        List<string> values = new List<string> { string.Empty };
        HashSet<string> seen = new HashSet<string>();

        for (int i = 0; i < eventsArray.arraySize; i++)
        {
            SerializedProperty element = eventsArray.GetArrayElementAtIndex(i);
            if (element == null)
                continue;

            SerializedProperty elementIdProp = element.FindPropertyRelative("eventId");
            if (elementIdProp == null)
                continue;

            string id = elementIdProp.stringValue;
            string normalized = PanelTimelineIdUtility.NormalizeId(id);
            if (string.IsNullOrEmpty(normalized) || seen.Contains(normalized))
                continue;

            seen.Add(normalized);
            labels.Add(id);
            values.Add(id);
        }

        string currentValue = targetEventId.stringValue ?? string.Empty;
        string currentNormalized = PanelTimelineIdUtility.NormalizeId(currentValue);
        int selectedIndex = 0;

        if (!string.IsNullOrEmpty(currentNormalized))
        {
            for (int i = 1; i < values.Count; i++)
            {
                if (PanelTimelineIdUtility.NormalizeId(values[i]) == currentNormalized)
                {
                    selectedIndex = i;
                    break;
                }
            }

            if (selectedIndex == 0)
            {
                labels.Add($"{currentValue} (Missing)");
                values.Add(currentValue);
                selectedIndex = values.Count - 1;
            }
        }

        EditorGUI.BeginChangeCheck();
        int newIndex = EditorGUI.Popup(line, label, selectedIndex, labels.ToArray());
        if (EditorGUI.EndChangeCheck())
            targetEventId.stringValue = values[newIndex];
    }

    private static void DrawVoiceIdPopup(Rect line, SerializedProperty targetVoiceId, SerializedProperty property)
    {
        if (targetVoiceId == null)
        {
            EditorGUI.LabelField(line, "Voice Id", "Missing field");
            return;
        }

        List<int> values = CollectVoiceIds(property);
        if (values.Count == 0)
        {
            EditorGUI.PropertyField(line, targetVoiceId, new GUIContent("Voice Id"));
            return;
        }

        values.Sort();

        List<string> labels = new List<string>(values.Count);
        foreach (int value in values)
            labels.Add(value.ToString());

        int currentValue = targetVoiceId.intValue;
        int selectedIndex = values.IndexOf(currentValue);
        if (selectedIndex < 0)
        {
            labels.Add($"{currentValue} (Missing)");
            values.Add(currentValue);
            selectedIndex = values.Count - 1;
        }

        int newIndex = EditorGUI.Popup(line, "Voice Id", selectedIndex, labels.ToArray());
        targetVoiceId.intValue = values[newIndex];
    }

    private static List<int> CollectVoiceIds(SerializedProperty property)
    {
        List<int> ids = new List<int>();
        HashSet<int> seen = new HashSet<int>();
        Scene targetScene = default;
        bool hasScene = TryGetTargetScene(property, out targetScene);

        InteractionUITrigger[] triggers = Resources.FindObjectsOfTypeAll<InteractionUITrigger>();
        foreach (InteractionUITrigger trigger in triggers)
        {
            if (trigger == null)
                continue;

            if (EditorUtility.IsPersistent(trigger))
                continue;

            if (hasScene && trigger.gameObject.scene != targetScene)
                continue;

            SerializedObject triggerObject = new SerializedObject(trigger);
            SerializedProperty clips = triggerObject.FindProperty("interactAudioClips");
            if (clips == null || !clips.isArray)
                continue;

            for (int i = 0; i < clips.arraySize; i++)
            {
                SerializedProperty element = clips.GetArrayElementAtIndex(i);
                if (element == null)
                    continue;

                SerializedProperty voiceIdProp = element.FindPropertyRelative("voiceId");
                if (voiceIdProp == null)
                    continue;

                int id = voiceIdProp.intValue;
                if (seen.Add(id))
                    ids.Add(id);
            }
        }

        return ids;
    }

    private static bool TryGetTargetScene(SerializedProperty property, out Scene scene)
    {
        scene = default;
        if (property == null)
            return false;

        Object target = property.serializedObject.targetObject;
        if (target is Component component)
        {
            scene = component.gameObject.scene;
            return scene.IsValid();
        }

        return false;
    }
}
