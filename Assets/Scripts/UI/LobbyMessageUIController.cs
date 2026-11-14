using System;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(UIManager))]
public class LobbyMessageUIController : MonoBehaviour
{
    private UIManager uiManager;
    private GameLobby gameLobby;
    private GameMultiplayer gameMultiplayer;

    private void Start()
    {
        uiManager = GetComponent<UIManager>();
        gameLobby = GameLobby.Instance;
        gameMultiplayer = GameMultiplayer.Instance;

        gameMultiplayer.OnFailedToJoinGame += GameMultiplayer_OnFailedToJoinGame;
        gameLobby.onCreateLobbyStarted += GameLobby_OnCreateLobbyStarted;
        gameLobby.onCreateLobbyFailed += GameLobby_OnCreateLobbyFailed;
        gameLobby.onJoinLobbyStarted += GameLobby_OnJoinLobbyStarted;
        gameLobby.onJoinLobbyFailed += GameLobby_OnJoinLobbyFailed;
        gameLobby.onQuickJoinLobbyFailed += GameLobby_OnQuickJoinLobbyFailed;
    }

    private void OnDestroy()
    {
        if (gameMultiplayer != null)
            gameMultiplayer.OnFailedToJoinGame -= GameMultiplayer_OnFailedToJoinGame;

        if (gameLobby == null)
            return;

        gameLobby.onCreateLobbyStarted -= GameLobby_OnCreateLobbyStarted;
        gameLobby.onCreateLobbyFailed -= GameLobby_OnCreateLobbyFailed;
        gameLobby.onJoinLobbyStarted -= GameLobby_OnJoinLobbyStarted;
        gameLobby.onJoinLobbyFailed -= GameLobby_OnJoinLobbyFailed;
        gameLobby.onQuickJoinLobbyFailed -= GameLobby_OnQuickJoinLobbyFailed;
    }

    private void GameMultiplayer_OnFailedToJoinGame(object sender, EventArgs e)
    {
        string reason = NetworkManager.Singleton.DisconnectReason;
        ShowMessage(string.IsNullOrWhiteSpace(reason) ? "Failed to connect." : reason);
    }

    private void GameLobby_OnCreateLobbyStarted(object sender, EventArgs e)
    {
        ShowMessage("Creating lobby...");
    }

    private void GameLobby_OnCreateLobbyFailed(object sender, EventArgs e)
    {
        ShowMessage("Failed to create lobby.");
    }

    private void GameLobby_OnJoinLobbyStarted(object sender, EventArgs e)
    {
        ShowMessage("Joining lobby...");
    }

    private void GameLobby_OnJoinLobbyFailed(object sender, EventArgs e)
    {
        ShowMessage("Failed to join lobby.");
    }

    private void GameLobby_OnQuickJoinLobbyFailed(object sender, EventArgs e)
    {
        ShowMessage("Failed to quick join lobby.");
    }

    private void ShowMessage(string message)
    {
        uiManager.ShowPanel(nameof(LobbyMessageUI));
        if (uiManager.GetPanel(nameof(LobbyMessageUI)) is LobbyMessageUI panel)
            panel.SetMessage(message);
    }
}
