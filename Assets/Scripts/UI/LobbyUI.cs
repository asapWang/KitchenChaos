using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Lobbies.Models;
public class LobbyUI : MonoBehaviour
{
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button quickJoinButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button joinByCodeButton;
    [SerializeField] private CreateLobbyUI createLobbyUI;
    [SerializeField] private TMP_InputField lobbyCodeInputField;
    [SerializeField] private TMP_InputField playerNameInputField;
    [SerializeField] private Transform lobbyListContainer;
    [SerializeField] private Transform lobbyListItemTemplate;
    private void Awake()
    {
        createLobbyButton.onClick.AddListener(() =>
        {
            createLobbyUI.Show();
        });
        quickJoinButton.onClick.AddListener(() =>
        {
            GameLobby.Instance.QuickJoinLobby();
        });
        mainMenuButton.onClick.AddListener(() =>
        {
            Loader.LoadScene(Loader.Scene.MainMenuScene);
        });
        joinByCodeButton.onClick.AddListener(() =>
        {
            string lobbyCode = lobbyCodeInputField.text;
            GameLobby.Instance.JoinLobbyByCode(lobbyCode);
        });
    }
    private void Start()
    {
        playerNameInputField.text = GameMultiplayer.Instance.GetPlayerName();
        playerNameInputField.onValueChanged.AddListener((string value) =>
        {
            GameMultiplayer.Instance.SetPlayerName(value);
        });
        GameLobby.Instance.onLobbyListChanged += GameLobby_OnLobbyListChanged;
        lobbyListItemTemplate.gameObject.SetActive(false);
    }
    private void GameLobby_OnLobbyListChanged(object sender, GameLobby.LobbyListChangedEventArgs e)
    {
        UpdateLobbyListVisual(e.lobbyList);
    }
    private void UpdateLobbyListVisual(List<Lobby> lobbyList)
    {
        foreach (Transform child in lobbyListContainer)
        {
            if (child == lobbyListItemTemplate) continue;
            Destroy(child.gameObject);
        }
        foreach (Lobby lobby in lobbyList)
        {
            Transform lobbyListItemTransform = Instantiate(lobbyListItemTemplate, lobbyListContainer);
            lobbyListItemTransform.gameObject.SetActive(true);
            lobbyListItemTransform.GetComponent<ListLobbySingleUI>().SetLobby(lobby);
        }
    }

}
