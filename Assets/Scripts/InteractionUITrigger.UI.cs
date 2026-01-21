using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public partial class InteractionUITrigger : MonoBehaviour
{
    private const string DefaultInteractLabel = "F để nói chuyện";

    [SerializeField] private Button targetButton;
    [SerializeField] private string buttonTag = "InteractButton";
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private float flashDuration = 0.1f;
    [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, 0.5f);

    private CanvasGroup canvasGroup;
    private Coroutine fadeRoutine;
    private Coroutine flashRoutine;
    private bool isButtonVisible;

    private void SetVisible(bool isVisible, bool immediate = false)
    {
        if (targetButton == null)
            return;

        if (isButtonVisible == isVisible && !immediate)
            return;

        isButtonVisible = isVisible;
        EnsureCanvasGroup();

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        if (immediate || fadeDuration <= 0f || canvasGroup == null)
        {
            ApplyVisibleState(isVisible);
            return;
        }

        if (isVisible)
            targetButton.gameObject.SetActive(true);

        fadeRoutine = StartCoroutine(FadeButton(isVisible));
    }

    private void TryFindButtonByTag()
    {
        if (string.IsNullOrEmpty(buttonTag))
            return;

        GameObject buttonObject = GameObject.FindGameObjectWithTag(buttonTag);
        if (buttonObject != null)
        {
            targetButton = buttonObject.GetComponent<Button>();
            ApplyDefaultLabel();
        }
    }

    private void EnsureCanvasGroup()
    {
        if (targetButton == null)
            return;

        if (canvasGroup == null)
            canvasGroup = targetButton.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = targetButton.gameObject.AddComponent<CanvasGroup>();

        ApplyDefaultLabel();
    }

    private void ApplyVisibleState(bool isVisible)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = isVisible ? 1f : 0f;
            canvasGroup.interactable = isVisible;
            canvasGroup.blocksRaycasts = isVisible;
        }

        targetButton.gameObject.SetActive(isVisible);
    }

    private IEnumerator FadeButton(bool isVisible)
    {
        if (canvasGroup == null)
            yield break;

        float startAlpha = canvasGroup.alpha;
        float endAlpha = isVisible ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = endAlpha;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;

        if (!isVisible)
            targetButton.gameObject.SetActive(false);
    }

    private void FlashButton()
    {
        if (targetButton == null)
            return;

        Graphic graphic = targetButton.targetGraphic;
        if (graphic == null)
            return;

        if (flashRoutine != null)
            StopCoroutine(flashRoutine);

        flashRoutine = StartCoroutine(FlashGraphic(graphic));
    }

    private IEnumerator FlashGraphic(Graphic graphic)
    {
        Color original = graphic.color;
        Color pressed = flashColor;
        pressed.a = original.a;

        graphic.color = pressed;
        if (flashDuration > 0f)
            yield return new WaitForSeconds(flashDuration);
        graphic.color = original;
    }

    private void ApplyDefaultLabel()
    {
        if (targetButton == null)
            return;

        Text uiText = targetButton.GetComponentInChildren<Text>(true);
        if (uiText != null)
            uiText.text = DefaultInteractLabel;

        TMP_Text tmpText = targetButton.GetComponentInChildren<TMP_Text>(true);
        if (tmpText != null)
            tmpText.text = DefaultInteractLabel;
    }
}
