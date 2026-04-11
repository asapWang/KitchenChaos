using TMPro;
using UnityEngine;
using System;

public class TutorialUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI moveUpText;
    [SerializeField] private TextMeshProUGUI moveDownText;
    [SerializeField] private TextMeshProUGUI moveLeftText;
    [SerializeField] private TextMeshProUGUI moveRightText;
    [SerializeField] private TextMeshProUGUI interactText;
    [SerializeField] private TextMeshProUGUI interactAltText;
    [SerializeField] private TextMeshProUGUI pauseText;
    [SerializeField] private TextMeshProUGUI movePadText;
    [SerializeField] private TextMeshProUGUI interactPadText;
    [SerializeField] private TextMeshProUGUI interactAltPadText;
    [SerializeField] private TextMeshProUGUI pausePadText;
    private void Start()
    {
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        UpdateVisual();
        Show();
    }
    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (GameManager.Instance.IsCountingDown())
        {
            Hide();
        }
    }
    //更新UI显示的按键绑定文本
    private void UpdateVisual()
    {
        moveUpText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Move_Up);
        moveDownText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Move_Down);
        moveLeftText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Move_Left);
        moveRightText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Move_Right);
        interactText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Interact);
        interactAltText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Interact_Alternate);
        pauseText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Pause);
        movePadText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Gamepad_Move);
        interactPadText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Gamepad_Interact);
        interactAltPadText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Gamepad_Interact_Alternate);
        pausePadText.text = InputSystem.Instance.GetBindingText(InputSystem.Binding.Gamepad_Pause);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
    public void Show()
    {
        gameObject.SetActive(true);
    }
}
