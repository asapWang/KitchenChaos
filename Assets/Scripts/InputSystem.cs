using UnityEngine;
using System;
using UnityEngine.InputSystem;

public class InputSystem : MonoBehaviour
{
    public static InputSystem Instance;
    private InputActions inputActions;
    //定义常量，表示按键绑定信息的键名
    private const string Player_Input_Rebinds = "PlayerInputRebinds";
    //与ClearCounter交流事件
    public event EventHandler OnInteractAction;
    public event EventHandler OnInteractAlternativeAction;
    public event EventHandler OnPauseAction;
    //键位名称枚举
    public enum Binding
    {
        Move_Up,
        Move_Down,
        Move_Left,
        Move_Right,
        Interact,
        Interact_Alternate,
        Pause
    }
    void Awake()
    {
        Instance = this;
        inputActions = new InputActions();
        //加载保存的按键绑定信息，如果有的话
        if (PlayerPrefs.HasKey(Player_Input_Rebinds))
        {
            //InputActions有一个特别方便的方法，LoadBindingOverridesFromJson()方法会把保存的JSON字符串加载到输入动作中，恢复之前的绑定设置
            inputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(Player_Input_Rebinds));
        }
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

    private void AllowInteraction(InputAction.CallbackContext context)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }
    private void AllowInteractionAlternative(InputAction.CallbackContext context)
    {
        OnInteractAlternativeAction?.Invoke(this, EventArgs.Empty);
    }
    private void AllowPause(InputAction.CallbackContext context)
    {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }
    //获取按键绑定文本
    public string GetBindingText(Binding binding)
    {
        switch (binding)
        {
            default:
            case Binding.Move_Up:
            //ToDisplayString()方法会根据绑定的输入设备类型，只返回按键的名称，而不是完整的路径，这样UI显示更友好
            //绑定的索引是单列索引，根据InputActions里面的顺序来的，下列四个的父对象WASD的索引是0
                return inputActions.Player.Move.bindings[1].ToDisplayString();
            case Binding.Move_Down:
                return inputActions.Player.Move.bindings[2].ToDisplayString();
            case Binding.Move_Left:
                return inputActions.Player.Move.bindings[3].ToDisplayString();
            case Binding.Move_Right:
                return inputActions.Player.Move.bindings[4].ToDisplayString();
            case Binding.Interact:
                return inputActions.Player.Interact.bindings[0].ToDisplayString();
            case Binding.Interact_Alternate:
                return inputActions.Player.InteractAlternative.bindings[0].ToDisplayString();
            case Binding.Pause:
                return inputActions.Player.Pause.bindings[0].ToDisplayString();
        }
    }
    //重新绑定按键
    public void RebindBinding(Binding binding, Action onActionRebound)
    {
        //先禁用输入系统，防止在重新绑定过程中触发其他输入事件
        inputActions.Player.Disable();
        //根据绑定的枚举类型，找到对应的输入动作和绑定索引
        InputAction inputAction;
        int bindingIndex;
        switch (binding)
        {
            default:
            case Binding.Move_Up:
                inputAction = inputActions.Player.Move;
                bindingIndex = 1;
                break;
            case Binding.Move_Down:
                inputAction = inputActions.Player.Move;
                bindingIndex = 2;
                break;
            case Binding.Move_Left:
                inputAction = inputActions.Player.Move;
                bindingIndex = 3;
                break;
            case Binding.Move_Right:
                inputAction = inputActions.Player.Move;
                bindingIndex = 4;
                break;
            case Binding.Interact:
                inputAction = inputActions.Player.Interact;
                bindingIndex = 0;
                break;
            case Binding.Interact_Alternate:
                inputAction = inputActions.Player.InteractAlternative;
                bindingIndex = 0;
                break;
            case Binding.Pause:
                inputAction = inputActions.Player.Pause;
                bindingIndex = 0;
                break;
        }

        // 开始重新绑定
        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback =>
            {
                callback.Dispose();
                //保存新的绑定信息到PlayerPrefs，方便下次打开游戏时加载
                //InputActions有一个特别方便的方法，SaveBindingOverridesAsJson()方法会把当前输入动作的所有绑定覆盖信息保存成一个JSON字符串
                PlayerPrefs.SetString(Player_Input_Rebinds, inputActions.SaveBindingOverridesAsJson());
                //重新启用输入系统
                inputActions.Player.Enable();
                // 重新绑定完成后，调用回调函数关闭提示UI，并且刷新显示的按键绑定文本
                onActionRebound();
            })
            .Start();

    }
}