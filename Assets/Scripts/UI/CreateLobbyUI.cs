using UnityEngine;
using UnityEngine.UI;
using TMPro;
public class CreateLobbyUI : MonoBehaviour
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
        backButton.onClick.AddListener(() =>
        {
            Hide();
        });
    }
    private void Start()
    {
        Hide();
    }
    private void Hide()
    {
        gameObject.SetActive(false);
    }
    public void Show()
    {
        gameObject.SetActive(true);
        createPublicLobbyButton.Select();
    }
}
