using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
[RequireComponent(typeof(CanvasGroup))]
[RequireComponent(typeof(Image))]
public class MissionUIActionController : MonoBehaviour
{
    [System.Serializable]
    public class MissionAction
    {
        public string missionText = string.Empty;
        public bool startAfterVoiceId = false;
        public int startVoiceId = 0;
        public int endVoiceId = 0;
        public float slideInDuration = 0.4f;
        public float flashDuration = 0.25f;
        public float fadeOutDuration = 0.4f;
        public float slideOutDuration = 0.3f;
        public Color flashColor = Color.white;
    }

    [SerializeField] private TMP_Text missionTextTarget;
    [SerializeField] private float hiddenOffsetX = 600f;
    [SerializeField] private bool startHidden = true;
    [SerializeField] private List<MissionAction> actions = new List<MissionAction>();

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image image;
    private Vector2 shownPosition;
    private Vector2 hiddenPosition;
    private Color baseImageColor = Color.white;
    private Color baseTextColor = Color.white;
    private int activeIndex = -1;
    private bool isTransitioning;
    private bool[] actionStarted;
    private bool[] actionCompleted;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
        if (missionTextTarget == null)
            missionTextTarget = GetComponentInChildren<TMP_Text>(true);

        shownPosition = rectTransform.anchoredPosition;
        hiddenPosition = shownPosition + new Vector2(-Mathf.Abs(hiddenOffsetX), 0f);

        if (image != null)
            baseImageColor = image.color;
        if (missionTextTarget != null)
            baseTextColor = missionTextTarget.color;

        EnsureActionState();

        if (startHidden)
        {
            rectTransform.anchoredPosition = hiddenPosition;
            canvasGroup.alpha = 0f;
        }
    }

    private void Update()
    {
        if (actions == null || actions.Count == 0)
            return;

        EnsureActionState();

        int voiceId = PanelTimelineController.GlobalVoiceId;
        if (activeIndex >= 0)
        {
            MissionAction action = actions[activeIndex];
            if (!actionCompleted[activeIndex] && !isTransitioning && voiceId == action.endVoiceId)
                StartCoroutine(CompleteAction(activeIndex));
            return;
        }

        if (isTransitioning)
            return;

        for (int i = 0; i < actions.Count; i++)
        {
            if (actionStarted[i] || actionCompleted[i])
                continue;

            MissionAction action = actions[i];
            if (ShouldStart(action, voiceId))
            {
                actionStarted[i] = true;
                activeIndex = i;
                StartCoroutine(ShowAction(i));
                break;
            }
        }
    }

    private bool ShouldStart(MissionAction action, int voiceId)
    {
        if (action == null)
            return false;

        return action.startAfterVoiceId ? voiceId >= action.startVoiceId : voiceId == action.startVoiceId;
    }

    private void EnsureActionState()
    {
        int count = actions != null ? actions.Count : 0;
        if (actionStarted == null || actionStarted.Length != count)
        {
            actionStarted = new bool[count];
            actionCompleted = new bool[count];
        }
    }

    private IEnumerator ShowAction(int index)
    {
        isTransitioning = true;

        MissionAction action = actions[index];
        if (missionTextTarget != null)
            missionTextTarget.text = action.missionText ?? string.Empty;

        if (image != null)
            image.color = baseImageColor;
        if (missionTextTarget != null)
            missionTextTarget.color = baseTextColor;

        canvasGroup.alpha = 1f;
        rectTransform.anchoredPosition = hiddenPosition;

        float duration = Mathf.Max(0f, action.slideInDuration);
        if (duration <= 0f)
        {
            rectTransform.anchoredPosition = shownPosition;
        }
        else
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                rectTransform.anchoredPosition = Vector2.Lerp(hiddenPosition, shownPosition, t);
                yield return null;
            }

            rectTransform.anchoredPosition = shownPosition;
        }

        isTransitioning = false;
    }

    private IEnumerator CompleteAction(int index)
    {
        isTransitioning = true;

        MissionAction action = actions[index];

        if (action.flashDuration > 0f)
            yield return FlashGraphics(action.flashColor, action.flashDuration);

        float fadeDuration = Mathf.Max(0f, action.fadeOutDuration);
        if (fadeDuration <= 0f)
        {
            canvasGroup.alpha = 0f;
        }
        else
        {
            float startAlpha = canvasGroup.alpha;
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / fadeDuration);
                canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, t);
                yield return null;
            }

            canvasGroup.alpha = 0f;
        }

        float slideDuration = Mathf.Max(0f, action.slideOutDuration);
        if (slideDuration <= 0f)
        {
            rectTransform.anchoredPosition = hiddenPosition;
        }
        else
        {
            Vector2 startPosition = rectTransform.anchoredPosition;
            float elapsed = 0f;
            while (elapsed < slideDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slideDuration);
                rectTransform.anchoredPosition = Vector2.Lerp(startPosition, hiddenPosition, t);
                yield return null;
            }

            rectTransform.anchoredPosition = hiddenPosition;
        }

        actionCompleted[index] = true;
        activeIndex = -1;
        isTransitioning = false;
    }

    private IEnumerator FlashGraphics(Color flashColor, float duration)
    {
        if (duration <= 0f)
            yield break;

        if (image != null)
            image.color = flashColor;
        if (missionTextTarget != null)
            missionTextTarget.color = flashColor;

        yield return new WaitForSeconds(duration);

        if (image != null)
            image.color = baseImageColor;
        if (missionTextTarget != null)
            missionTextTarget.color = baseTextColor;
    }
}
