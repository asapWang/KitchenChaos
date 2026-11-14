using System;
using UnityEngine;

[RequireComponent(typeof(UIManager))]
public class GamePauseUIController : MonoBehaviour
{
    private UIManager uiManager;
    private GameManager gameManager;
    private InputSystem inputSystem;

    private void Start()
    {
        uiManager = GetComponent<UIManager>();
        gameManager = GameManager.Instance;
        inputSystem = InputSystem.Instance;
        gameManager.OnLocalPauseGame += OnLocalPauseGame;
        gameManager.OnLocalUnpauseGame += OnLocalUnpauseGame;
        inputSystem.OnPauseAction += OnPauseAction;
    }

    private void OnDestroy()
    {
        if (inputSystem != null)
            inputSystem.OnPauseAction -= OnPauseAction;

        if (gameManager == null)
            return;

        gameManager.OnLocalPauseGame -= OnLocalPauseGame;
        gameManager.OnLocalUnpauseGame -= OnLocalUnpauseGame;
    }

    private void OnPauseAction(object sender, EventArgs e)
    {
        if (uiManager.PeekPanel() is OptionsUI)
        {
            uiManager.PopPanel();
            return;
        }

        gameManager.TogglePauseGame();
    }

    private void OnLocalPauseGame(object sender, EventArgs e)
    {
        uiManager.PushPanel(nameof(GamePauseUI));
    }

    private void OnLocalUnpauseGame(object sender, EventArgs e)
    {
        uiManager.HidePanel(nameof(OptionsUI));
        uiManager.HidePanel(nameof(GamePauseUI));
    }
}
