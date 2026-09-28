using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;
    private void Awake()
    {
        mainMenuButton.onClick.AddListener(() =>
        {
            NetworkManager.Singleton.Shutdown();
            Loader.LoadScene(Loader.Scene.MainMenuScene);
            GameLobby.Instance.LeaveLobby();
        });
        readyButton.onClick.AddListener(() =>
        {
            CharacterSelectReady.Instance.SetPlayerReady();
        });
    }
    private void Start()
    {
        //显示lobby名字和密码
        lobbyNameText.text = "Lobby Name: " + GameLobby.Instance.GetLobby().Name;
        lobbyCodeText.text = "Lobby Code: " + GameLobby.Instance.GetLobby().LobbyCode;
    }
}
