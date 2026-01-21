using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControllers : MonoBehaviour {
    [SerializeField] private float playerSpeed = 5.0f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravityValue = -9.81f;
    [SerializeField] private Transform cameraTransform;

    public CharacterController controller;
    private Vector3 playerVelocity;
    private bool groundedPlayer;
    private InputManager inputManager;
    private static int rotationLockCount;

    public static void AcquireRotationLock()
    {
        rotationLockCount++;
    }

    public static void ReleaseRotationLock()
    {
        rotationLockCount = Mathf.Max(0, rotationLockCount - 1);
    }

    public static bool IsRotationLocked => rotationLockCount > 0;


    private void Start() {
        controller = GetComponent<CharacterController>();
        inputManager = InputManager.Instance;
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;
    }
    void Update()
    {
        if (controller == null || !controller.enabled)
            return;

        groundedPlayer = controller.isGrounded;

        if (groundedPlayer)
        {
            // Slight downward velocity to keep grounded stable
            if (playerVelocity.y < -2f)
                playerVelocity.y = -2f;
        }

        // Read input
        Vector2 input = inputManager.GetPlayerMovement();
        Vector3 move;
        if (cameraTransform != null)
        {
            Vector3 cameraForward = cameraTransform.forward;
            Vector3 cameraRight = cameraTransform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            move = cameraForward * input.y + cameraRight * input.x;
        }
        else
        {
            move = new Vector3(input.x, 0, input.y);
        }
        move = Vector3.ClampMagnitude(move, 1f);

        if (cameraTransform != null)
        {
            if (!IsRotationLocked)
            {
                Vector3 flatForward = cameraTransform.forward;
                flatForward.y = 0f;
                if (flatForward.sqrMagnitude > 0.001f)
                    transform.rotation = Quaternion.LookRotation(flatForward, Vector3.up);
            }
        }
        else if (move != Vector3.zero)
        {
            if (!IsRotationLocked)
                transform.forward = move;
        }

        // Jump using WasPressedThisFrame()
        if (groundedPlayer && inputManager.IsJumpPressed())
        {
            playerVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravityValue);
        }

        // Apply gravity
        playerVelocity.y += gravityValue * Time.deltaTime;

        // Move
        Vector3 finalMove = move * playerSpeed + Vector3.up * playerVelocity.y;
        controller.Move(finalMove * Time.deltaTime);
    }
}
