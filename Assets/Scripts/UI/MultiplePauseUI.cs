using System;
using UnityEngine;

public class MultiplePauseUI : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.OnMultiplayerPauseGame += GameManager_OnMultiplayerPauseGame;
        GameManager.Instance.OnMultiplayerUnpauseGame += GameManager_OnMultiplayerUnpauseGame;
        Hide();
    }
    private void GameManager_OnMultiplayerPauseGame(object sender, EventArgs e)
    {
        Show();
    }
    private void GameManager_OnMultiplayerUnpauseGame(object sender, EventArgs e)
    {
        Hide();
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }

}
