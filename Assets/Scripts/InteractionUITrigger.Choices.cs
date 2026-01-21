using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class InteractionUITrigger : MonoBehaviour
{
    [System.Serializable]
    public class ChoiceItem
    {
        public int choiceId = 0;
        [TextArea] public string content = string.Empty;
    }

    [SerializeField] private Button choiceButtonPrefab;
    [SerializeField] private RectTransform choiceContainer;
    [SerializeField] private float choiceSpacing = 20f;
    [SerializeField] private float choiceButtonWidth = 0f;
    [SerializeField] private float choiceButtonHeight = 0f;
    [SerializeField] private float choiceOffsetY = 0f;
    [SerializeField] private float choiceTitleOffsetY = 0f;

    private readonly List<Button> spawnedChoiceButtons = new List<Button>();
    private TMP_Text spawnedChoiceTitle;
    private static InteractionUITrigger choiceOwner;
    private InteractAudioItem activeChoiceItem;
    private bool choicesVisible;
    private bool warnedMissingChoiceSetup;
    private int resolvedChoiceVoiceId = int.MinValue;
    private int triggeredChoiceVoiceId = int.MinValue;
    private bool awaitingChoiceInput;
    private bool rotationLockedByChoices;
    private CanvasGroup choiceCanvasGroup;
    private UnityEngine.UI.GraphicRaycaster choiceRaycaster;

    private void InitializeChoices()
    {
        SetChoicesVisible(false, true);
    }

    private bool TryShowChoicesForCurrentVoiceId()
    {
        if (isAudioPlaying)
        {
            awaitingChoiceInput = false;
            return false;
        }

        int currentVoiceId = PanelTimelineController.GlobalVoiceId;
        if (resolvedChoiceVoiceId == currentVoiceId)
        {
            awaitingChoiceInput = false;
            return false;
        }

        InteractAudioItem choiceItem = FindMultiChoiceItem();
        if (choiceItem == null || choiceItem.choices == null || choiceItem.choices.Count == 0)
        {
            awaitingChoiceInput = false;
            return false;
        }

        if (!IsAudioItemAllowed(choiceItem))
        {
            awaitingChoiceInput = false;
            SetVisible(false, true);
            return false;
        }

        bool inRange = playerInRange || !hasTriggerCollider;
        if (!inRange && !choiceItem.autoPlay)
        {
            awaitingChoiceInput = false;
            return false;
        }

        if (!choiceItem.autoPlay)
        {
            if (triggeredChoiceVoiceId != currentVoiceId)
            {
                if (inRange)
                    SetVisible(true);

                awaitingChoiceInput = true;
                if (!IsFallbackInteractPressed())
                    return true; // keep showing the prompt while waiting

                triggeredChoiceVoiceId = currentVoiceId;
            }
        }
        else
        {
            awaitingChoiceInput = false;
        }

        if (choiceButtonPrefab == null || choiceContainer == null)
        {
            WarnMissingChoiceSetup();
            return false;
        }

        if (activeChoiceItem != choiceItem || spawnedChoiceButtons.Count == 0)
            SetActiveChoiceItem(choiceItem);

        SetChoicesVisible(true, true);
        return true;
    }

    private void SetActiveChoiceItem(InteractAudioItem item)
    {
        if (activeChoiceItem == item && spawnedChoiceButtons.Count > 0)
            return;

        activeChoiceItem = item;
        ClearChoices();

        if (item != null)
            BuildChoiceButtons(item);
    }

    private void SetChoicesVisible(bool isVisible, bool immediate = false)
    {
        if (choicesVisible == isVisible && !immediate)
            return;

        if (!isVisible && choiceOwner != null && choiceOwner != this)
            return;

        if (isVisible)
        {
            if (choiceOwner != null && choiceOwner != this)
                choiceOwner.ReleaseChoices();
            choiceOwner = this;
        }
        else if (choiceOwner == this)
            choiceOwner = null;

        choicesVisible = isVisible;
        UpdateChoiceRotationLock();

        if (choiceContainer != null)
        {
            EnsureChoiceCanvasGroup();
            EnsureChoiceRaycaster();

            choiceContainer.gameObject.SetActive(isVisible);

            if (choiceCanvasGroup != null)
            {
                choiceCanvasGroup.alpha = isVisible ? 1f : 0f;
                choiceCanvasGroup.interactable = isVisible;
                choiceCanvasGroup.blocksRaycasts = isVisible;
            }
            if (choiceRaycaster != null)
                choiceRaycaster.enabled = isVisible;
        }

        if (!isVisible)
            ClearChoices();

        SetCameraInputLocked(choicesVisible && lockCameraOnChoices);
    }

    private void EnsureChoiceCanvasGroup()
    {
        if (choiceContainer == null)
            return;

        if (choiceCanvasGroup == null)
            choiceCanvasGroup = choiceContainer.GetComponent<CanvasGroup>();
        if (choiceCanvasGroup == null)
            choiceCanvasGroup = choiceContainer.gameObject.AddComponent<CanvasGroup>();
    }

    private void EnsureChoiceRaycaster()
    {
        if (choiceContainer == null)
            return;

        Canvas canvas = choiceContainer.GetComponentInParent<Canvas>(true);
        if (canvas == null)
            canvas = choiceContainer.GetComponent<Canvas>();
        if (canvas != null)
        {
            choiceRaycaster = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (choiceRaycaster == null)
                choiceRaycaster = canvas.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
    }

    private void UpdateChoiceRotationLock()
    {
        bool shouldLock = choicesVisible;

        if (shouldLock && !rotationLockedByChoices)
        {
            PlayerControllers.AcquireRotationLock();
            rotationLockedByChoices = true;
        }
        else if (!shouldLock && rotationLockedByChoices)
        {
            PlayerControllers.ReleaseRotationLock();
            rotationLockedByChoices = false;
        }
    }

    private void ReleaseChoices()
    {
        activeChoiceItem = null;
        choicesVisible = false;
        ClearChoices();
        UpdateChoiceRotationLock();
    }

    private void BuildChoiceButtons(InteractAudioItem item)
    {
        if (item == null || item.choices == null || item.choices.Count == 0)
            return;

        BuildChoiceTitle(item);

        for (int i = 0; i < item.choices.Count; i++)
        {
            ChoiceItem choice = item.choices[i];
            Button buttonInstance = Instantiate(choiceButtonPrefab, choiceContainer);
            SetupChoiceButton(buttonInstance, choice, i);
            spawnedChoiceButtons.Add(buttonInstance);
        }

        LayoutChoiceButtons();
    }

    private void BuildChoiceTitle(InteractAudioItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.title))
            return;

        TMP_Text template = item.titlePrefab != null ? item.titlePrefab : FindChoiceTitleTemplate(item);
        if (template == null)
            return;

        spawnedChoiceTitle = Instantiate(template, choiceContainer);
        spawnedChoiceTitle.text = item.title;
    }

    private TMP_Text FindChoiceTitleTemplate(InteractAudioItem item)
    {
        TMP_Text taggedTemplate = FindChoiceTitleTemplateByTag(item);
        if (taggedTemplate != null)
            return taggedTemplate;

        return GetChoiceTitleTemplate();
    }

    private TMP_Text FindChoiceTitleTemplateByTag(InteractAudioItem item)
    {
        if (item == null || string.IsNullOrWhiteSpace(item.titlePrefabTag))
            return null;

        GameObject taggedObject;
        try
        {
            taggedObject = GameObject.FindGameObjectWithTag(item.titlePrefabTag);
        }
        catch (UnityException)
        {
            return null;
        }

        if (taggedObject == null)
            return null;

        return taggedObject.GetComponentInChildren<TMP_Text>(true);
    }

    private TMP_Text GetChoiceTitleTemplate()
    {
        if (choiceButtonPrefab == null)
            return null;

        return choiceButtonPrefab.GetComponentInChildren<TMP_Text>(true);
    }

    private void SetupChoiceButton(Button button, ChoiceItem choice, int listIndex)
    {
        if (button == null)
            return;

        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = choice != null ? choice.content : string.Empty;

        button.onClick.RemoveAllListeners();
        int choiceIndex = ResolveChoiceIndex(choice, listIndex);
        button.onClick.AddListener(() => SelectChoice(choiceIndex));
    }

    private int ResolveChoiceIndex(ChoiceItem choice, int listIndex)
    {
        if (choice != null && choice.choiceId >= 0)
            return choice.choiceId;

        return listIndex;
    }

    private void SelectChoice(int choiceIndex)
    {
        globalChoiceIndex = choiceIndex;
        resolvedChoiceVoiceId = PanelTimelineController.GlobalVoiceId;
        SetActiveChoiceItem(null);
        SetChoicesVisible(false, true);

        if (FindMatchingItem() == null)
            PanelTimelineController.AdvanceGlobalVoiceId();

        UpdateState();
    }

    private void LayoutChoiceButtons()
    {
        if (choiceContainer == null || spawnedChoiceButtons.Count == 0)
            return;

        int count = spawnedChoiceButtons.Count;
        float[] widths = new float[count];
        float totalWidth = 0f;

        for (int i = 0; i < count; i++)
        {
            Button button = spawnedChoiceButtons[i];
            if (button == null)
                continue;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
                continue;

            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);

            Vector2 size = rect.sizeDelta;
            if (choiceButtonWidth > 0f)
                size.x = choiceButtonWidth;
            if (choiceButtonHeight > 0f)
                size.y = choiceButtonHeight;
            rect.sizeDelta = size;

            widths[i] = rect.sizeDelta.x;
            totalWidth += widths[i];
        }

        float containerWidth = choiceContainer.rect.width;
        float spacing = Mathf.Max(0f, choiceSpacing);
        float startX;

        if (containerWidth > 0f)
        {
            float evenSpacing = (containerWidth - totalWidth) / (count + 1);
            spacing = Mathf.Max(spacing, evenSpacing);

            if (evenSpacing >= spacing)
                startX = -containerWidth * 0.5f + spacing;
            else
                startX = -(totalWidth + spacing * (count - 1)) * 0.5f;
        }
        else
        {
            startX = -(totalWidth + spacing * (count - 1)) * 0.5f;
        }

        float titleHeight = 0f;
        if (spawnedChoiceTitle != null)
        {
            RectTransform titleRect = spawnedChoiceTitle.rectTransform;
            if (titleRect != null)
            {
                titleRect.anchorMin = new Vector2(0.5f, 1f);
                titleRect.anchorMax = new Vector2(0.5f, 1f);
                titleRect.pivot = new Vector2(0.5f, 1f);

                titleHeight = titleRect.rect.height;
                if (titleHeight <= 0f)
                    titleHeight = titleRect.sizeDelta.y;
                titleRect.anchoredPosition = new Vector2(0f, -choiceTitleOffsetY);
            }
        }

        float rowOffset = titleHeight > 0f ? titleHeight + spacing : 0f;
        float cursor = startX;
        for (int i = 0; i < count; i++)
        {
            Button button = spawnedChoiceButtons[i];
            if (button == null)
                continue;

            RectTransform rect = button.GetComponent<RectTransform>();
            if (rect == null)
                continue;

            float halfWidth = widths[i] * 0.5f;
            rect.anchoredPosition = new Vector2(cursor + halfWidth, -rowOffset - choiceOffsetY);
            cursor += widths[i] + spacing;
        }
    }

    private void ClearChoices()
    {
        if (spawnedChoiceTitle != null)
        {
            Destroy(spawnedChoiceTitle.gameObject);
            spawnedChoiceTitle = null;
        }

        for (int i = 0; i < spawnedChoiceButtons.Count; i++)
        {
            Button button = spawnedChoiceButtons[i];
            if (button != null)
                Destroy(button.gameObject);
        }

        spawnedChoiceButtons.Clear();
    }

    private void WarnMissingChoiceSetup()
    {
        if (warnedMissingChoiceSetup)
            return;

        warnedMissingChoiceSetup = true;
        Debug.LogWarning("InteractionUITrigger: Missing choiceButtonPrefab or choiceContainer.");
    }
}
