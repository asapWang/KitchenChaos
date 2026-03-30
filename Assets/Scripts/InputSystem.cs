using UnityEngine;
using System;

public class InputSystem : MonoBehaviour
{
    public static InputSystem Instance;
    
    private InputActions inputActions;
    //与ClearCounter交流事件
    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAlternativeAction;
    public event EventHandler OnPauseAction;
    void Awake()
    {
        Instance = this;
        inputActions = new InputActions();
        inputActions.Enable();
        //事件的好处就是解耦合，并且不需要每一帧都检测，但是新输入系统的自带事件，底层其实还是每帧检测，只不过在我们代码层面简化了不需要考虑这些
        inputActions.Player.Interact.performed += AllowInteraction;
        inputActions.Player.InteractAlternative.performed += AllowInteractionAlternative;
        inputActions.Player.Pause.performed += AllowPause;
    }
    //onDestroy是unity自带的回调函数，在对象被销毁时调用
    //取消订阅并销毁输入系统
    private void OnDestroy()
    {
        inputActions.Player.Interact.performed -= AllowInteraction;
        inputActions.Player.InteractAlternative.performed -= AllowInteractionAlternative;
        inputActions.Player.Pause.performed -= AllowPause;   
        inputActions.Dispose();
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
    private void AllowPause(UnityEngine.InputSystem.InputAction.CallbackContext context)
    {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

}