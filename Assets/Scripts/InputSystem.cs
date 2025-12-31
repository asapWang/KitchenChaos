using UnityEngine;
using System;

public class InputSystem : MonoBehaviour
{
    private InputActions inputActions;
    //与ClearCounter交流事件
    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAlternativeAction;
    void Awake()
    {
        inputActions = new InputActions();
        inputActions.Enable();
        //事件的好处就是解耦合，并且不需要每一帧都检测，但是新输入系统的自带事件，底层其实还是每帧检测，只不过在我们代码层面简化了不需要考虑这些
        inputActions.Player.Interact.performed += AllowInteraction;
        inputActions.Player.InteractAlternative.performed += AllowInteractionAlternative;
    }
    public Vector2 GetMovementInput()
    {
        Vector2 movePosition = inputActions.Player.Move.ReadValue<Vector2>();
        movePosition = movePosition.normalized;
        return movePosition;
    }

    private void AllowInteraction(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }
    private void AllowInteractionAlternative(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        OnInteractAlternativeAction?.Invoke(this, EventArgs.Empty);
    }

}