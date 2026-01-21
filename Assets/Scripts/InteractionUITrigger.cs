using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public partial class InteractionUITrigger : MonoBehaviour
{
    private static int globalChoiceIndex = -1;
    public static int GlobalChoiceIndex => globalChoiceIndex;

    [SerializeField] private string playerTag = "Player";
    [SerializeField] private InputActionReference interactAction;
    [Header("Camera Lock")]
    [SerializeField] private bool lockCameraOnChoices = true;
    [SerializeField] private CinemachineInputAxisController cameraInputController;
    [Header("Look At Player On Voice")]
    [SerializeField] private bool lookAtPlayerWhileAudio = false;
    [SerializeField] private Transform playerLookTarget;
    [SerializeField] private float lookAtPlayerYawOffset = 0f;
    [SerializeField] private float lookAtPlayerSmoothing = 5f;
    [SerializeField] private float lookAtPlayerReturnSmoothing = 5f;

    private bool playerInRange;
    private bool warnedMissingPlayerTag;
    private bool hasTriggerCollider;
    private int lastLoggedVoiceId = int.MinValue;
    private bool cameraLockManaged;
    private bool isLookingAtPlayer;
    private Quaternion originalRotation;

    private void Awake()
    {
        if (targetButton == null)
            TryFindButtonByTag();

        EnsureCanvasGroup();
        InitializeChoices();
        InitializeNameTag();
        hasTriggerCollider = HasTriggerCollider();
        EnsureCameraInputController();

        if (hideOnStart)
        {
            SetVisible(false, true);
        }
    }

    private void OnEnable()
    {
        hasTriggerCollider = HasTriggerCollider();
        if (interactAction != null)
        {
            interactAction.action.performed += OnInteract;
            interactAction.action.Enable();
        }
    }

    private void Update()
    {
        UpdateState();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsMatchingCollider(other))
        {
            playerInRange = true;
            if (targetButton == null)
                TryFindButtonByTag();
            UpdateState();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsMatchingCollider(other))
        {
            playerInRange = false;
            ClearPendingPlay(true);
            SetVisible(false);
            awaitingChoiceInput = false;
        }
    }

    private void OnDisable()
    {
        if (interactAction != null)
        {
            interactAction.action.performed -= OnInteract;
            interactAction.action.Disable();
        }

        playerInRange = false;
        ClearPendingPlay();
        SetVisible(false, true);
        SetActiveChoiceItem(null);
        SetChoicesVisible(false, true);
        SetNameTagHighlighted(false, true);
        triggeredChoiceVoiceId = int.MinValue;
        awaitingChoiceInput = false;
        PlayerControllers.ReleaseRotationLock();
        SetCameraInputLocked(false);
    }

    private void UpdateState()
    {
        int currentVoiceId = PanelTimelineController.GlobalVoiceId;
        if (currentVoiceId != lastLoggedVoiceId)
        {
            lastLoggedVoiceId = currentVoiceId;
            Debug.Log($"InteractionUITrigger: GlobalVoiceId={currentVoiceId}");
        }

        if (TryShowChoicesForCurrentVoiceId())
        {
            if (awaitingChoiceInput)
            {
                ClearPendingPlay();
                SetVisible(true);
                return;
            }

            ClearPendingPlay();
            SetVisible(false, true);
            return;
        }

        if (activeChoiceItem != null && choicesVisible)
        {
            ClearPendingPlay();
            SetVisible(false, true);
            return;
        }

        bool inRange = playerInRange || !hasTriggerCollider;
        if (isAudioPlaying)
        {
            SetVisible(false);
            SetChoicesVisible(false, true);
            HandleLookAtPlayer(true);
            return;
        }
        else
        {
            HandleLookAtPlayer(false);
        }

        InteractAudioItem match = FindMatchingItem();
        if (match == null)
        {
            ClearPendingPlay();
            SetVisible(false);
            SetActiveChoiceItem(null);
            SetChoicesVisible(false, true);
            return;
        }

        if (!IsAudioItemAllowed(match))
        {
            ClearPendingPlay();
            SetVisible(false, true);
            SetActiveChoiceItem(null);
            SetChoicesVisible(false, true);
            return;
        }

        SetActiveChoiceItem(null);
        SetChoicesVisible(false, true);

        if (match.clip == null)
        {
            ClearPendingPlay();
            SetVisible(false);
            return;
        }

        if (!inRange && !match.autoPlay)
        {
            SetVisible(false, true);
            return;
        }

        if (pendingItem != null && pendingItem != match)
            ClearPendingPlay();

        if (match.autoPlay)
        {
            SetVisible(false);
            TrySchedulePlay(match);
            print("aaa0");
        }
        else
        {
            SetVisible(pendingPlayRoutine == null);
            if (interactAction == null && pendingPlayRoutine == null && IsFallbackInteractPressed())
            {
                FlashButton();
                TrySchedulePlay(match);
            }
        }
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        if (activeChoiceItem != null)
            return;

        if ((!playerInRange && hasTriggerCollider) || isAudioPlaying)
            return;

        InteractAudioItem match = FindMatchingItem();
        if (match == null || match.multiChoice || match.autoPlay || match.clip == null)
            return;

        if (pendingPlayRoutine != null)
            return;

        FlashButton();
        TrySchedulePlay(match);
    }

    private void WarnMissingPlayerTag()
    {
        if (warnedMissingPlayerTag)
            return;

        warnedMissingPlayerTag = true;
        Debug.LogWarning("InteractionUITrigger: playerTag is empty. Please set a valid Player tag.");
    }

    private bool IsFallbackInteractPressed()
    {
        Keyboard keyboard = Keyboard.current;
        return keyboard != null && keyboard.fKey.wasPressedThisFrame;
    }

    private void EnsureCameraInputController()
    {
        if (cameraInputController != null)
            return;

        cameraInputController = FindObjectOfType<CinemachineInputAxisController>(true);
    }

    private void SetCameraInputLocked(bool locked)
    {
        if (!lockCameraOnChoices)
            return;

        if (cameraInputController == null)
            return;

        if (locked)
        {
            if (cameraInputController.enabled)
            {
                cameraInputController.enabled = false;
                cameraLockManaged = true;
            }
        }
        else
        {
            if (cameraLockManaged)
            {
                cameraInputController.enabled = true;
                cameraLockManaged = false;
            }
        }
    }

    private void ApplyLookAtPlayer()
    {
        if (!lookAtPlayerWhileAudio)
            return;

        if (playerLookTarget == null)
            return;

        Vector3 toPlayer = playerLookTarget.position - transform.position;
        toPlayer.y = 0f;
        if (toPlayer.sqrMagnitude < 0.0001f)
            return;

        Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
        if (Mathf.Abs(lookAtPlayerYawOffset) > 0.001f)
            targetRot *= Quaternion.Euler(0f, lookAtPlayerYawOffset, 0f);
        if (lookAtPlayerSmoothing > 0f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * lookAtPlayerSmoothing);
        }
        else
        {
            transform.rotation = targetRot;
        }
    }

    private void HandleLookAtPlayer(bool isAudioActive)
    {
        if (!lookAtPlayerWhileAudio)
        {
            isLookingAtPlayer = false;
            return;
        }

        if (isAudioActive)
        {
            if (!isLookingAtPlayer)
            {
                originalRotation = transform.rotation;
                isLookingAtPlayer = true;
            }

            ApplyLookAtPlayer();
        }
        else if (isLookingAtPlayer)
        {
            if (playerLookTarget == null)
            {
                isLookingAtPlayer = false;
                return;
            }

            float smoothing = lookAtPlayerReturnSmoothing > 0f ? lookAtPlayerReturnSmoothing : lookAtPlayerSmoothing;
            if (smoothing > 0f)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, originalRotation, Time.deltaTime * smoothing);
                if (Quaternion.Angle(transform.rotation, originalRotation) < 0.1f)
                {
                    transform.rotation = originalRotation;
                    isLookingAtPlayer = false;
                }
            }
            else
            {
                transform.rotation = originalRotation;
                isLookingAtPlayer = false;
            }
        }
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
}
