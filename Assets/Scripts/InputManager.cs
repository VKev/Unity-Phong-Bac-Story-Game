using UnityEngine;

public class InputManager : MonoBehaviour
{
    private PlayerController playerController;
    private void Awake() {
        if (instance != null && instance != this) {
            Destroy(this.gameObject);
            return;
        }else {
            instance = this;
        }
        playerController = new PlayerController();

    }

    private static InputManager instance;
    public static InputManager Instance {
        get {
            
            return instance;
        }
    }

    private void OnEnable() {
        playerController.Enable();
    }

    private void OnDisable() {
        playerController.Disable();
    }

    public Vector2 GetPlayerMovement() {
        return playerController.Player.Movement.ReadValue<Vector2>();
    }

    public Vector2 getMouseDelta() {
        return playerController.Player.Look.ReadValue<Vector2>();
    }

    public bool IsJumpPressed() {
        return playerController.Player.Jump.triggered;
    }
}
