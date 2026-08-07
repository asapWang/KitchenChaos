using UnityEngine;
using System;

public class WaitingForOtherPlayers : MonoBehaviour
{
    private void Start()
    {
        GameManager.Instance.OnLocalPlayerReadyChanged += GameManager_OnLocalPlayerReadyChanged;
        GameManager.Instance.OnStateChanged += GameManager_OnStateChanged;
        Hide();
    }
    private void GameManager_OnLocalPlayerReadyChanged(object sender, EventArgs e)
    {
        if (GameManager.Instance.IsPlayerReady())
        {
            Show();
        }
    }
    private void GameManager_OnStateChanged(object sender, EventArgs e)
    {
        if (GameManager.Instance.IsCountingDown() )
        {
            Hide();
        }
    }
    private void Show()
    {
        gameObject.SetActive(true);
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
