using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CreateLobbyUI : BasePanel
{
    [SerializeField] private TMP_InputField lobbyNameInputField;
    [SerializeField] private Button createPublicLobbyButton;
    [SerializeField] private Button createPrivateLobbyButton;
    [SerializeField] private Button backButton;
    private void Awake()
    {
        createPublicLobbyButton.onClick.AddListener(() =>
        {
            string lobbyName = lobbyNameInputField.text;
            GameLobby.Instance.CreateLobby(lobbyName, false);
        });
        createPrivateLobbyButton.onClick.AddListener(() =>
        {
            string lobbyName = lobbyNameInputField.text;
            GameLobby.Instance.CreateLobby(lobbyName, true);
        });
        UIManager uiManager = GetComponentInParent<UIManager>();
        backButton.onClick.AddListener(() => uiManager.HidePanel(nameof(CreateLobbyUI)));
    }

    public override void Enter()
    {
        createPublicLobbyButton.Select();
    }
}
