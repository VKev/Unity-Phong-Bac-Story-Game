using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Simple trigger-driven interaction prompt that mirrors InteractionUITrigger's button flow.
/// Shows a button with the label "F để chọn đồ" and invokes an event when pressed.
/// </summary>
public class ItemSelectInteraction : MonoBehaviour
{
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private string buttonTag = "ItemSelectButton";
    [SerializeField] private Button targetButton;
    [SerializeField] private bool hideOnStart = true;
    [SerializeField] private bool requireVoiceId = false;
    [SerializeField] private int requiredVoiceId = 0;
    [Header("Attachment Targets")]
    [SerializeField] private Transform leftHand;
    [SerializeField] private Transform rightHand;
    [SerializeField] private bool attachToRightHand = true;
    [SerializeField] private UnityEvent onInteract;

    private CanvasGroup canvasGroup;
    private bool playerInRange;
    private bool hasTriggerCollider;
    private bool warnedMissingPlayerTag;
    private bool hasSelected;

    public bool HasSelected => hasSelected;

    private const string DefaultLabel = "F để chọn đồ";

    private void Awake()
    {
        if (targetButton == null)
            TryFindButtonByTag();

        EnsureCanvasGroup();
        SetButtonLabel(DefaultLabel);

        hasTriggerCollider = HasTriggerCollider();
        if (hideOnStart)
            SetVisible(false, true);
    }

    private void OnEnable()
    {
        hasTriggerCollider = HasTriggerCollider();
    }

    private void OnDisable()
    {
        playerInRange = false;
        SetVisible(false, true);
    }

    private void Update()
    {
        if (!playerInRange && hasTriggerCollider)
            return;

        if (hasSelected)
        {
            SetVisible(false, true);
            return;
        }

        if (!IsVoiceAllowed())
        {
            SetVisible(false);
            return;
        }

        EnsureButtonReadyAndVisible();

        if (IsFallbackInteractPressed())
            TriggerInteract();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsMatchingCollider(other))
            return;

        playerInRange = true;
        if (targetButton == null)
            TryFindButtonByTag();

        SetButtonLabel(DefaultLabel);
        SetVisible(IsVoiceAllowed());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsMatchingCollider(other))
            return;

        playerInRange = false;
        SetVisible(false);
        hasSelected = false;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!IsMatchingCollider(other))
            return;

        playerInRange = true;
        if (targetButton == null)
            TryFindButtonByTag();

        SetButtonLabel(DefaultLabel);
        SetVisible(IsVoiceAllowed());
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (!IsMatchingCollider(other))
            return;

        playerInRange = false;
        SetVisible(false);
    }

    private void TriggerInteract()
    {
        if (!IsVoiceAllowed())
            return;

        if (hasSelected)
            return;

        FlashButton();
        SetVisible(false);
        onInteract?.Invoke();
        AttachToHand();
        hasSelected = true;
        DisableInteraction();
    }

    private void SetVisible(bool isVisible, bool immediate = false)
    {
        if (targetButton == null)
            return;

        EnsureCanvasGroup();

        if (canvasGroup == null || immediate)
        {
            ApplyVisibleState(isVisible);
            return;
        }

        canvasGroup.alpha = isVisible ? 1f : 0f;
        canvasGroup.interactable = isVisible;
        canvasGroup.blocksRaycasts = isVisible;
        targetButton.gameObject.SetActive(isVisible);
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

    private void EnsureCanvasGroup()
    {
        if (targetButton == null)
            return;

        if (canvasGroup == null)
            canvasGroup = targetButton.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = targetButton.gameObject.AddComponent<CanvasGroup>();
    }

    private void TryFindButtonByTag()
    {
        if (string.IsNullOrEmpty(buttonTag))
            return;

        GameObject buttonObject = GameObject.FindGameObjectWithTag(buttonTag);
        if (buttonObject != null)
        {
            targetButton = buttonObject.GetComponent<Button>();
            EnsureCanvasGroup();
            SetButtonLabel(DefaultLabel);
        }
        else
        {
            Debug.LogWarning($"ItemSelectInteraction: No button found with tag '{buttonTag}'. Assign targetButton or set a valid tag.");
        }
    }

    private void SetButtonLabel(string label)
    {
        if (targetButton == null)
            return;

        Text uiText = targetButton.GetComponentInChildren<Text>(true);
        if (uiText != null)
        {
            uiText.text = label;
            return;
        }

        TMPro.TMP_Text tmpText = targetButton.GetComponentInChildren<TMPro.TMP_Text>(true);
        if (tmpText != null)
            tmpText.text = label;
    }

    private bool IsMatchingCollider(Collider other)
    {
        if (other == null)
            return false;

        if (string.IsNullOrEmpty(playerTag))
        {
            WarnMissingPlayerTag();
            return true;
        }

        return other.CompareTag(playerTag);
    }

    private bool IsMatchingCollider(Collider2D other)
    {
        if (other == null)
            return false;

        if (string.IsNullOrEmpty(playerTag))
        {
            WarnMissingPlayerTag();
            return true;
        }

        return other.CompareTag(playerTag);
    }

    private void WarnMissingPlayerTag()
    {
        if (warnedMissingPlayerTag)
            return;

        warnedMissingPlayerTag = true;
        Debug.LogWarning("ItemSelectInteraction: playerTag is empty. Please set a valid Player tag.");
    }

    private bool HasTriggerCollider()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null && colliders[i].isTrigger)
                return true;
        }

        Collider2D[] colliders2D = GetComponents<Collider2D>();
        for (int i = 0; i < colliders2D.Length; i++)
        {
            if (colliders2D[i] != null && colliders2D[i].isTrigger)
                return true;
        }

        return false;
    }

    private bool IsFallbackInteractPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.fKey.wasPressedThisFrame;
    }

    private void EnsureButtonReadyAndVisible()
    {
        if (targetButton == null)
            TryFindButtonByTag();

        SetButtonLabel(DefaultLabel);
        SetVisible(true);
    }

    private void FlashButton()
    {
        if (targetButton == null)
            return;

        Graphic graphic = targetButton.targetGraphic;
        if (graphic == null)
            return;

        StartCoroutine(FlashGraphic(graphic));
    }

    private System.Collections.IEnumerator FlashGraphic(Graphic graphic)
    {
        Color original = graphic.color;
        Color pressed = Color.white;
        pressed.a = original.a;

        graphic.color = pressed;
        yield return new WaitForSeconds(0.1f);
        graphic.color = original;
    }

    private bool IsVoiceAllowed()
    {
        if (!requireVoiceId)
            return true;

        return PanelTimelineController.GlobalVoiceId == requiredVoiceId;
    }

    private void AttachToHand()
    {
        Transform target = null;
        if (attachToRightHand)
            target = rightHand;
        else
            target = leftHand;

        if (target == null)
        {
            Debug.LogWarning("ItemSelectInteraction: No hand target assigned for attachment.");
            return;
        }

        transform.SetParent(target, worldPositionStays: false);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private void DisableInteraction()
    {
        Collider[] colliders = GetComponents<Collider>();
        for (int i = 0; i < colliders.Length; i++)
        {
            if (colliders[i] != null)
                colliders[i].enabled = false;
        }

        Collider2D[] colliders2D = GetComponents<Collider2D>();
        for (int i = 0; i < colliders2D.Length; i++)
        {
            if (colliders2D[i] != null)
                colliders2D[i].enabled = false;
        }

        enabled = false;
    }
}
