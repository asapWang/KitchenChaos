using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class LobbyMessageUI : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private TextMeshProUGUI messageText;
    private void Awake()
    {
        backButton.onClick.AddListener(() =>
        {
            Hide();
        });
    }
    private void Start()
    {
        GameMultiplayer.Instance.OnFailedToJoinGame += GameMultiplayer_OnFailedToJoinGame;
        GameLobby.Instance.onCreateLobbyStarted += GameLobby_OnCreateLobbyStarted;
        GameLobby.Instance.onCreateLobbyFailed += GameLobby_OnCreateLobbyFailed;
        GameLobby.Instance.onJoinLobbyStarted += GameLobby_OnJoinLobbyStarted;
        GameLobby.Instance.onJoinLobbyFailed += GameLobby_OnJoinLobbyFailed;
        GameLobby.Instance.onQuickJoinLobbyFailed += GameLobby_OnQuickJoinLobbyFailed;
        Hide();
    }
    private void GameMultiplayer_OnFailedToJoinGame(object sender, System.EventArgs e)
    {
        ShowMessage(NetworkManager.Singleton.DisconnectReason.ToString());
        if(NetworkManager.Singleton.DisconnectReason == null)
        {
            messageText.text = "Failed to connect.";
        }
    }
    private void GameLobby_OnCreateLobbyStarted(object sender, System.EventArgs e)
    {
        ShowMessage("Creating lobby...");
    }
    private void GameLobby_OnCreateLobbyFailed(object sender, System.EventArgs e)
    {
        ShowMessage("Failed to create lobby.");
    }
    private void GameLobby_OnJoinLobbyStarted(object sender, System.EventArgs e)
    {
        ShowMessage("Joining lobby...");
    }
    private void GameLobby_OnJoinLobbyFailed(object sender, System.EventArgs e)
    {
        ShowMessage("Failed to join lobby.");
    }
    private void GameLobby_OnQuickJoinLobbyFailed(object sender, System.EventArgs e)
    {
        ShowMessage("Failed to quick join lobby.");
    }
    private void ShowMessage(string message)
    {
        Show();
        messageText.text = message;
    }
    private void Show()
    {
        this.gameObject.SetActive(true);
        backButton.Select();
    }
    private void Hide()
    {
        this.gameObject.SetActive(false);
    }
    
    private void OnDestroy()
    {
        GameMultiplayer.Instance.OnFailedToJoinGame -= GameMultiplayer_OnFailedToJoinGame;
        GameLobby.Instance.onCreateLobbyStarted -= GameLobby_OnCreateLobbyStarted;
        GameLobby.Instance.onCreateLobbyFailed -= GameLobby_OnCreateLobbyFailed;
        GameLobby.Instance.onJoinLobbyStarted -= GameLobby_OnJoinLobbyStarted;
        GameLobby.Instance.onJoinLobbyFailed -= GameLobby_OnJoinLobbyFailed;
        GameLobby.Instance.onQuickJoinLobbyFailed -= GameLobby_OnQuickJoinLobbyFailed;
    }
}
