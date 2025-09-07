using UnityEngine;

public class InputSystem : MonoBehaviour
{
    private InputActions inputActions;
    void Awake()
    {
        inputActions = new InputActions();
        inputActions.Enable();
    }
    public Vector2 GetMovementInput()
    {
        Vector2 movePosition = inputActions.Player.Move.ReadValue<Vector2>();
        movePosition = movePosition.normalized;
        return movePosition;
    }
}
